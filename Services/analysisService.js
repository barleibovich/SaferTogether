const { httpError } = require("./errors");
const { getGroupStatistics } = require("./activityService");
const { chatCompletion } = require("./groqService");

const MISSION_GAME_DESCRIPTIONS = {
  code: "Door-code sequence memory game: the keypad flashes random digits, the child repeats them, and early-stage mistakes are weighted more heavily than later ones.",
  missile: "Missile dodge game: missiles fall for one minute, the child moves or tilts the phone to dodge, and hits plus phone tilt strength are measured.",
  puzzle: "Emergency-kit puzzle: in each of four stages the child chooses the correct item to bring to the protected room from four images."
};

// round to 1 decimal, or null for non-numbers
function round1OrNull(value) {
  return Number.isFinite(Number(value)) ? Math.round(Number(value) * 10) / 10 : null;
}

function numericValues(values) {
  return values.map(Number).filter(Number.isFinite);
}

function averageOrNull(values) {
  const numbers = numericValues(values);
  if (!numbers.length) return null;
  return round1OrNull(numbers.reduce((sum, value) => sum + value, 0) / numbers.length);
}

function sumOrNull(values) {
  const numbers = numericValues(values);
  if (!numbers.length) return null;
  return round1OrNull(numbers.reduce((sum, value) => sum + value, 0));
}

function averageGapOrNull(items, ownGetter, benchmarkGetter) {
  const gaps = [];

  items.forEach(item => {
    const own = Number(ownGetter(item));
    const benchmark = Number(benchmarkGetter(item));
    if (Number.isFinite(own) && Number.isFinite(benchmark)) {
      gaps.push(own - benchmark);
    }
  });

  return averageOrNull(gaps);
}

function hasMemberMeasurement(item) {
  const you = item.you || {};
  return [
    you.timeSeconds,
    you.handRotationDegrees,
    you.mistakesOrHits,
    you.weightedScore,
    you.hits,
    you.tiltStrength,
    you.correct
  ].some(value => value !== null && value !== undefined);
}

function summarizeActivityItems(items) {
  const measured = items.filter(hasMemberMeasurement);
  const scored = measured.filter(item => typeof item.you?.correct === "boolean");
  const correct = scored.filter(item => item.you.correct).length;
  const wrong = scored.length - correct;

  return {
    measuredItems: measured.length,
    totalItems: items.length,
    averageTimeSeconds: averageOrNull(measured.map(item => item.you.timeSeconds)),
    maxTimeSeconds: round1OrNull(Math.max(...numericValues(measured.map(item => item.you.timeSeconds)))),
    averageHandRotationDegrees: averageOrNull(measured.map(item => item.you.handRotationDegrees)),
    maxHandRotationDegrees: round1OrNull(Math.max(...numericValues(measured.map(item => item.you.handRotationDegrees)))),
    totalMistakesOrHits: sumOrNull(measured.map(item => item.you.mistakesOrHits)),
    averageMistakesOrHits: averageOrNull(measured.map(item => item.you.mistakesOrHits)),
    correct,
    wrong,
    accuracyPercent: scored.length ? round1OrNull((correct / scored.length) * 100) : null,
    averageTimeGapVsRealAlarmSeconds: averageGapOrNull(
      measured,
      item => item.you.timeSeconds,
      item => item.groupAverage.timeRealAlarm
    ),
    averageTimeGapVsTrainingSeconds: averageGapOrNull(
      measured,
      item => item.you.timeSeconds,
      item => item.groupAverage.timeTraining
    ),
    averageRotationGapVsRealAlarmDegrees: averageGapOrNull(
      measured,
      item => item.you.handRotationDegrees,
      item => item.groupAverage.handRotationRealAlarm
    ),
    averageRotationGapVsTrainingDegrees: averageGapOrNull(
      measured,
      item => item.you.handRotationDegrees,
      item => item.groupAverage.handRotationTraining
    ),
    averageMistakesGapVsRealAlarm: averageGapOrNull(
      measured,
      item => item.you.mistakesOrHits,
      item => item.groupAverage.mistakesRealAlarm
    ),
    averageMistakesGapVsTraining: averageGapOrNull(
      measured,
      item => item.you.mistakesOrHits,
      item => item.groupAverage.mistakesTraining
    )
  };
}

// build one member's readings vs the group averages, so the model can judge their stress
function buildMemberMeasurements(stats, userId) {
  const member = (stats.members || []).find(entry => entry.userId === userId);
  if (!member) {
    throw httpError(404, "החבר לא נמצא בקבוצה זו");
  }

  const aggByKey = new Map();
  (stats.aggregates || []).forEach(row => {
    aggByKey.set(`${row.activityId}:${row.metric}:${row.mode}:${row.itemIndex}`, row.avgValue);
  });

  const latestByActivity = new Map();
  (stats.latestResults || []).forEach(row => {
    if (row.userId === userId) {
      latestByActivity.set(row.activityId, row);
    }
  });

  const activities = (stats.activities || []).map(activity => {
    const latest = latestByActivity.get(activity.id);
    const myItems = new Map();
    (latest?.items || []).forEach(item => {
      if (Number.isInteger(item.index)) {
        myItems.set(item.index, item);
      }
    });

    const agg = (metric, mode, index) => round1OrNull(
      aggByKey.get(`${activity.id}:${metric}:${mode}:${index}`)
    );

    const items = (activity.items || []).map(item => {
      const mine = myItems.get(item.index) || {};
      return {
        item: item.label,
        description: mine.description || null,
        you: {
          timeSeconds: round1OrNull(mine.timeSeconds),
          handRotationDegrees: round1OrNull(mine.rotation),
          mistakesOrHits: round1OrNull(mine.mistakes ?? mine.wrongAttempts ?? mine.hits),
          weightedScore: round1OrNull(mine.weightedScore),
          hits: round1OrNull(mine.hits),
          tiltStrength: round1OrNull(mine.tiltStrength),
          correct: typeof mine.correct === "boolean" ? mine.correct : null
        },
        groupAverage: {
          timeRealAlarm: agg("time", "real", item.index),
          timeTraining: agg("time", "training", item.index),
          handRotationRealAlarm: agg("rotation", "real", item.index),
          handRotationTraining: agg("rotation", "training", item.index),
          mistakesRealAlarm: agg("mistakes", "real", item.index),
          mistakesTraining: agg("mistakes", "training", item.index),
          correctRateRealAlarm: agg("correct", "real", item.index),
          correctRateTraining: agg("correct", "training", item.index)
        }
      };
    });

    const games = (latest?.games || []).map(game => ({
      game: game.game,
      description: MISSION_GAME_DESCRIPTIONS[game.game] || null,
      totalSeconds: round1OrNull(game.totalSeconds),
      weightedScore: game.game === "code" ? round1OrNull(game.weightedScore) : null,
      hits: game.game === "missile" ? round1OrNull(game.hits) : null,
      tiltStrength: game.game === "missile" ? round1OrNull(game.tiltStrength) : null
    }));

    return {
      games,
      game: activity.title || (activity.type === "mission" ? "Mission room" : "Trivia"),
      type: activity.type,
      memberPlayedThisIn: latest?.mode || null,
      submittedAt: latest?.submittedAt || null,
      hasMemberData: Boolean(latest),
      chartSummary: summarizeActivityItems(items),
      items
    };
  });

  return {
    username: member.username,
    hasAnyData: activities.some(activity => activity.hasMemberData),
    activities
  };
}

const SYSTEM_PROMPT = [
  "You are assisting the ADMIN of a civil-defense preparedness group.",
  "During real air-raid alarms and during training drills, each member plays short games:",
  "trivia questions and a 'mission room' with three mini-games: emergency-kit puzzle, door-code sequence, and missile dodge.",
  "For each item we capture behavioural measurements:",
  "- timeSeconds: how long the member took on that item (slow or very uneven times can signal hesitation, freezing or distraction).",
  "- handRotationDegrees: how much the phone was tilted/rotated during the item (high values can indicate shaking, fidgeting or agitation; only available on phones, null on desktop).",
  "- correct: whether a trivia answer or mission stage was correct on the first try.",
  "- mistakesOrHits: wrong attempts in puzzle/code stages, or missile hits in the missile game.",
  "- weightedScore: door-code score where earlier mistakes count more heavily.",
  "- hits and tiltStrength: missile-game hits and accumulated phone tilt strength.",
  "Mission item descriptions explain what each game measured; use them when interpreting the data.",
  "We also provide the group's running averages for REAL alarms vs TRAINING, per item, for comparison.",
  "Key reading: bigger gaps between the member and the group, slower-than-training times under a real alarm, high hand rotation/tilt, more wrong attempts/hits, and a drop in accuracy under a real alarm all point to higher stress.",
  "Based ONLY on these measurements and chart summaries, write a fuller practical assessment for the admin of how this member is coping and their apparent stress level.",
  "Be careful and NON-CLINICAL — these are behavioural signals, not a medical diagnosis. Explicitly note when data is sparse, missing, phone-only, or based on a single latest run.",
  "Use concrete numbers from the JSON when they matter. Compare the member with real-alarm averages and training averages where those values exist.",
  "Answer in HEBREW with these short sections:",
  "1. תמונת מצב כללית",
  "2. ממצאים מרכזיים מהגרפים",
  "3. פירוט לפי משחקים/משימות",
  "4. סימני לחץ אפשריים",
  "5. חוזקות ונקודות לשימור",
  "6. המלצות לאדמין",
  "Keep it useful and readable for an admin, about 250-400 words."
].join("\n");

// admin-only: build a member's readings and ask Groq for a Hebrew stress summary
async function generateUserSituationSummary(accessToken, groupId, body = {}) {
  const userId = String(body.userId || "").trim();
  if (!userId) {
    throw httpError(400, "נדרש מזהה משתמש");
  }

  // getGroupStatistics checks admin access and returns only this group's real data
  const stats = await getGroupStatistics(accessToken, groupId);
  const measurements = buildMemberMeasurements(stats, userId);

  if (!measurements.hasAnyData) {
    return {
      summary: `אין עדיין מספיק מדידות עבור ${measurements.username} כדי להפיק סיכום. בקש מהמשתתף לשחק לפחות משחק אחד ונסה שוב.`,
      generated: false
    };
  }

  const summary = await chatCompletion([
    { role: "system", content: SYSTEM_PROMPT },
    {
      role: "user",
      content: `Member: ${measurements.username}\nReal measurements and chart summaries (JSON):\n${JSON.stringify(measurements, null, 2)}`
    }
  ]);

  return { summary, generated: true };
}

module.exports = {
  generateUserSituationSummary
};
