const { httpError } = require("./errors");
const { getGroupStatistics } = require("./activityService");
const { chatCompletion } = require("./groqService");

const MISSION_GAME_DESCRIPTIONS = {
  code: "Door-code sequence memory game. More errors, a lower weighted score, slower recall, or unusually high phone movement versus both baselines may indicate hesitation, reduced concentration, or cognitive load under stress; they are not diagnostic by themselves.",
  missile: "One-minute missile-dodge game. The duration is fixed, so stress interpretation should focus on hits and tilt strength—not completion time. More hits than the real-alarm and training averages may indicate startle, reduced concentration, or less controlled movement under pressure; sample size and other metrics must be considered.",
  puzzle: "Emergency-kit selection game. More wrong choices, slower decisions, or unusually high phone rotation versus both baselines may indicate hesitation, distraction, or agitation; fast accurate choices are a possible sign of composed decision-making, not proof of low stress."
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
function buildMemberMeasurements(stats, userId, resultId = "") {
  const member = (stats.members || []).find(entry => entry.userId === userId);
  if (!member) {
    throw httpError(404, "החבר לא נמצא בקבוצה זו");
  }

  const aggByKey = new Map();
  (stats.aggregates || []).forEach(row => {
    aggByKey.set(`${row.activityId}:${row.metric}:${row.mode}:${row.itemIndex}`, row);
  });
  const globalAggByKey = new Map();
  (stats.globalAggregates || []).forEach(row => {
    globalAggByKey.set(`${row.metric}:${row.mode}:${row.itemIndex}`, row);
  });

  const selectedResults = resultId
    ? (stats.results || []).filter(row => row.id === resultId && row.userId === userId)
    : (stats.latestResults || []).filter(row => row.userId === userId);
  const selectedActivityIds = new Set(selectedResults.map(row => row.activityId));
  const latestByActivity = new Map();
  selectedResults.forEach(row => {
    if (row.userId === userId) {
      latestByActivity.set(row.activityId, row);
    }
  });

  const activities = (stats.activities || [])
    .filter(activity => !resultId || selectedActivityIds.has(activity.id))
    .map(activity => {
    const latest = latestByActivity.get(activity.id);
    const myItems = new Map();
    (latest?.items || []).forEach(item => {
      if (Number.isInteger(item.index)) {
        myItems.set(item.index, item);
      }
    });

    const agg = (metric, mode, index) => round1OrNull(
      aggByKey.get(`${activity.id}:${metric}:${mode}:${index}`)?.avgValue
    );
    const globalAgg = (metric, mode, index) => round1OrNull(
      globalAggByKey.get(`${metric}:${mode}:${index}`)?.avgValue
    );
    const aggCount = (metric, mode, index) => Number(
      aggByKey.get(`${activity.id}:${metric}:${mode}:${index}`)?.sampleCount || 0
    );
    const globalAggCount = (metric, mode, index) => Number(
      globalAggByKey.get(`${metric}:${mode}:${index}`)?.sampleCount || 0
    );

    const items = (activity.items || []).map(item => {
      const mine = myItems.get(item.index) || {};
      const gameId = String(mine.game || "").trim().toLowerCase();
      return {
        item: item.label,
        game: gameId || null,
        description: MISSION_GAME_DESCRIPTIONS[gameId] || mine.description || null,
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
        },
        globalAverage: {
          timeRealAlarm: globalAgg("time", "real", item.index),
          timeTraining: globalAgg("time", "training", item.index),
          handRotationRealAlarm: globalAgg("rotation", "real", item.index),
          handRotationTraining: globalAgg("rotation", "training", item.index),
          mistakesRealAlarm: globalAgg("mistakes", "real", item.index),
          mistakesTraining: globalAgg("mistakes", "training", item.index)
        },
        baselineSampleCount: {
          groupRealAlarm: aggCount("mistakes", "real", item.index) || aggCount("time", "real", item.index),
          groupTraining: aggCount("mistakes", "training", item.index) || aggCount("time", "training", item.index),
          globalRealAlarm: globalAggCount("mistakes", "real", item.index) || globalAggCount("time", "real", item.index),
          globalTraining: globalAggCount("mistakes", "training", item.index) || globalAggCount("time", "training", item.index)
        }
      };
    });

    const games = (latest?.games || []).map(game => {
      const gameId = String(game.id || game.game || "").trim().toLowerCase();
      return ({
        game: gameId,
        description: MISSION_GAME_DESCRIPTIONS[gameId] || game.description || null,
        totalSeconds: round1OrNull(game.totalSeconds),
        weightedScore: gameId === "code" ? round1OrNull(game.weightedScore) : null,
        hits: gameId === "missile" ? round1OrNull(game.hits) : null,
        tiltStrength: gameId === "missile" ? round1OrNull(game.tiltStrength) : null
      });
    });

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
    analysisScope: resultId ? "single_selected_run" : "latest_run_per_activity",
    selectedResultId: resultId || null,
    selectedSession: selectedResults[0] ? {
      mode: selectedResults[0].mode,
      modeLabelHebrew: selectedResults[0].mode === "real" ? "אזעקת אמת" : "תרגול",
      submittedAt: selectedResults[0].submittedAt || null
    } : null,
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
  "Treat each game's description as an interpretation guide, not decorative text. Explain what a worse-than-baseline deviation may mean for possible stress while remaining non-clinical.",
  "Example rule for missile dodge: if member hits are above both the REAL-alarm and TRAINING averages, identify that as a possible pressure/stress signal (startle, reduced concentration, or less controlled movement). Quantify both gaps. Do not dismiss it merely because other games were accurate.",
  "Do not interpret the missile game's fixed 60-second duration as slow performance. Use hits and tilt strength for that game.",
  "Consider baselineSampleCount: comparisons based on very small samples are weak evidence and must be described cautiously.",
  "We provide two baseline sources per item: groupAverage for this group and globalAverage across all groups/users. Both contain separate REAL-alarm and TRAINING values when data exists.",
  "When analysisScope is single_selected_run, analyze ONLY that selected run and its activity. Do not describe other games as unplayed, and do not claim to analyze the member's history.",
  "For every useful metric, explicitly compare the selected run with BOTH groupAverage real-alarm and training baselines when they are available. State clearly when either baseline is missing.",
  "The selected run's mode and submittedAt identify whether it occurred during a real alarm or a training drill and when it happened.",
  "HEBREW TERMINOLOGY IS STRICT: never use the words 'רונד', 'ראונד', or 'סשן'. Call mode=training 'תרגול' and mode=real 'אזעקת אמת'. Use 'הריצה שנבחרה' only when a neutral term is necessary.",
  "BASELINE COMPARISON IS MANDATORY regardless of whether the selected session itself is training or real. Compare the member separately with (1) the REAL-alarm average and (2) the TRAINING average. Prefer globalAverage so the comparison matches the red/blue chart baselines; also mention groupAverage when available.",
  "Do not tell the admin to check, inspect, or compare the group averages: YOU must perform those comparisons in the analysis. Never put 'compare with the group average' in the recommendations.",
  "If groupAverage is null, use globalAverage. Only say that a real/training baseline is unavailable when BOTH sources are null for the relevant metrics; never invent a value and never silently skip it.",
  "Recommendations must address the member's POSSIBLE STRESS LEVEL and emotional/safety support—not whether they were good or bad at a particular game. Recommend concrete stress-focused actions such as a short breathing/grounding routine, calm rehearsal under gradually realistic conditions, a supportive post-event conversation, checking how the member felt, or professional support if concerning signals persist. Never recommend game-specific performance training. Avoid generic instructions to analyze data. Include at most one data-collection recommendation, only when missing baselines materially limit the stress assessment.",
  "Key reading: bigger gaps between the member and the group, slower-than-training times under a real alarm, high hand rotation/tilt, more wrong attempts/hits, and a drop in accuracy under a real alarm all point to higher stress.",
  "Stress assessment must integrate adverse deviations across games. Strong accuracy in one task does not cancel a meaningful above-baseline stress signal in another task.",
  "Based ONLY on these measurements and chart summaries, write a fuller practical assessment for the admin of how this member is coping and their apparent stress level.",
  "Be careful and NON-CLINICAL — these are behavioural signals, not a medical diagnosis. Explicitly state that one selected run cannot establish a trend.",
  "Use concrete numbers from the JSON when they matter. Compare the member with real-alarm averages and training averages where those values exist.",
  "Answer in HEBREW with these short sections:",
  "1. תמונת מצב כללית",
  "2. ממצאים מרכזיים והשוואה לממוצעים (include an explicit real-alarm-average comparison and a separate training-average comparison)",
  "3. פירוט לפי משחקים/משימות",
  "4. סימני לחץ אפשריים",
  "5. חוזקות ונקודות לשימור",
  "6. המלצות לאדמין",
  "Keep it useful and readable for an admin, about 250-400 words."
].join("\n");

// admin-only: build a member's readings and ask Groq for a Hebrew stress summary
async function generateUserSituationSummary(accessToken, groupId, body = {}) {
  const userId = String(body.userId || "").trim();
  const resultId = String(body.resultId || "").trim();
  if (!userId) {
    throw httpError(400, "נדרש מזהה משתמש");
  }

  // getGroupStatistics checks admin access and returns only this group's real data
  const stats = await getGroupStatistics(accessToken, groupId);
  const measurements = buildMemberMeasurements(stats, userId, resultId);

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

  const sessionLabel = measurements.selectedSession?.modeLabelHebrew || "הריצה שנבחרה";
  const cleanedSummary = String(summary || "")
    .replace(/רונדים|ראונדים|סשנים/g, "תרגולים")
    .replace(/רונד|ראונד|סשן/g, sessionLabel);

  return { summary: cleanedSummary, generated: true };
}

module.exports = {
  generateUserSituationSummary
};
