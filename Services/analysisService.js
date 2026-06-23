const { httpError } = require("./errors");
const { getGroupStatistics } = require("./activityService");
const { chatCompletion } = require("./groqService");

// round to 1 decimal, or null for non-numbers
function round1OrNull(value) {
  return Number.isFinite(Number(value)) ? Math.round(Number(value) * 10) / 10 : null;
}

// Assemble one member's measurements out of the group statistics bundle:
// for every game item we line up the member's latest reading (time, hand
// rotation, correctness) next to the group's running averages for real
// alarms vs training, so the model can judge how the member copes under stress.
function buildMemberMeasurements(stats, userId) {
  const member = (stats.members || []).find(entry => entry.userId === userId);
  if (!member) {
    throw httpError(404, "Member not found in this group");
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
        you: {
          timeSeconds: round1OrNull(mine.timeSeconds),
          handRotationDegrees: round1OrNull(mine.rotation),
          correct: typeof mine.correct === "boolean" ? mine.correct : null
        },
        groupAverage: {
          timeRealAlarm: agg("time", "real", item.index),
          timeTraining: agg("time", "training", item.index),
          handRotationRealAlarm: agg("rotation", "real", item.index),
          handRotationTraining: agg("rotation", "training", item.index),
          correctRateRealAlarm: agg("correct", "real", item.index),
          correctRateTraining: agg("correct", "training", item.index)
        }
      };
    });

    return {
      game: activity.title || (activity.type === "mission" ? "Mission room" : "Trivia"),
      type: activity.type,
      memberPlayedThisIn: latest?.mode || null,
      hasMemberData: Boolean(latest),
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
  "trivia questions and a 'mission room' with hands-on tasks (lock the door, close the window, wire the radio, board exercises).",
  "For each item we capture behavioural measurements:",
  "- timeSeconds: how long the member took on that item (slow or very uneven times can signal hesitation, freezing or distraction).",
  "- handRotationDegrees: how much the phone was tilted/rotated during the item (high values can indicate shaking, fidgeting or agitation; only available on phones, null on desktop).",
  "- correct: whether a trivia answer was right (missions self-verify, so correct is null there).",
  "We also provide the group's running averages for REAL alarms vs TRAINING, per item, for comparison.",
  "Key reading: bigger gaps between the member and the group, slower-than-training times under a real alarm, high hand rotation, and a drop in accuracy under a real alarm all point to higher stress.",
  "Based ONLY on these measurements, write a short, practical assessment for the admin of how this member is coping and their apparent stress level.",
  "Be careful and NON-CLINICAL — these are behavioural signals, not a medical diagnosis. Explicitly note when data is sparse or missing.",
  "Answer in HEBREW, under ~150 words, using exactly these three short bullet lines:",
  "• מצב כללי: <one or two sentences>",
  "• סימני לחץ: <what in the numbers suggests stress / calm>",
  "• המלצה לאדמין: <one concrete suggestion>"
].join("\n");

// Admin-only: build the selected member's measurements and ask Groq for a
// Hebrew, admin-facing summary of the member's situation / stress level.
async function generateUserSituationSummary(accessToken, groupId, body = {}) {
  const userId = String(body.userId || "").trim();
  if (!userId) {
    throw httpError(400, "A userId is required");
  }

  // getGroupStatistics enforces the admin-of-this-group check and returns
  // members, activities, the running aggregates and each member's latest result.
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
      content: `Member: ${measurements.username}\nMeasurements (JSON):\n${JSON.stringify(measurements.activities, null, 2)}`
    }
  ]);

  return { summary, generated: true };
}

module.exports = {
  generateUserSituationSummary
};
