const { httpError } = require("./errors");
const { getSessionContext } = require("./supabaseService");

const ACTIVITY_TABLE = "group_activities";
const ACTIVATION_TABLE = "group_activity_activations";
const RESULT_TABLE = "group_activity_results";
const ACTIVITY_TYPES = ["trivia", "mission"];
const ACTIVITY_MODES = ["real", "training"];
const RESULT_STATUSES = ["pending", "approved", "rejected"];
const MISSION_TARGETS = ["puzzle", "code", "missile", "custom"];
const MISSION_TASK_LABELS = {
  code: "קוד הדלת",
  missile: "טילים",
  puzzle: "ערכת חירום"
};
const MISSION_TASK_STAGE_COUNTS = {
  code: 4,
  missile: 1,
  puzzle: 4
};

// is this error just because the activity tables aren't set up?
function isMissingActivitySetupError(error) {
  const code = String(error?.code || "");
  const message = String(error?.message || "").toLowerCase();

  return (
    ["42P01", "42703", "PGRST204", "PGRST205"].includes(code) ||
    [ACTIVITY_TABLE, ACTIVATION_TABLE, RESULT_TABLE].some(table => (
      message.includes(table) && (
        message.includes("does not exist") ||
        message.includes("schema cache") ||
        message.includes("column")
      )
    ))
  );
}

// missing tables -> tell them which sql to run
function missingActivitySetupError() {
  return httpError(
    500,
    "Activity tables are missing. Run supabase/group_activities.sql in Supabase first."
  );
}

// run a supabase query and translate the missing-table error
async function runActivityQuery(query) {
  const { data, error } = await query;

  if (error) {
    if (isMissingActivitySetupError(error)) {
      throw missingActivitySetupError();
    }

    throw error;
  }

  return data;
}

// trim a string, use fallback if empty
function cleanText(value, fallback = "") {
  return String(value || "").trim() || fallback;
}

// make sure the type is trivia or mission
function cleanActivityType(type) {
  const value = String(type || "").trim().toLowerCase();

  if (!ACTIVITY_TYPES.includes(value)) {
    throw httpError(400, "סוג המשחק חייב להיות טריוויה או משימה");
  }

  return value;
}

// make sure the mode is real or training
function cleanActivityMode(mode) {
  const value = String(mode || "").trim().toLowerCase();

  if (!ACTIVITY_MODES.includes(value)) {
    throw httpError(400, "מצב המשחק חייב להיות אזעקת אמת או תרגול");
  }

  return value;
}

// check the status is one we allow
function cleanResultStatus(status) {
  const value = String(status || "").trim().toLowerCase();

  if (!RESULT_STATUSES.includes(value)) {
    throw httpError(400, "סטטוס התוצאה חייב להיות ממתין, אושר או נדחה");
  }

  return value;
}

// validate + tidy up one trivia question
function cleanQuestion(rawQuestion, index) {
  const question = cleanText(rawQuestion?.question);
  const answers = Array.isArray(rawQuestion?.answers)
    ? rawQuestion.answers.map(answer => cleanText(answer))
    : [];
  const correctAnswerIndex = Number(rawQuestion?.correctAnswerIndex);

  if (!question) {
    throw httpError(400, `Question ${index + 1} is missing text`);
  }

  if (answers.length !== 4 || answers.some(answer => !answer)) {
    throw httpError(400, `Question ${index + 1} must have exactly 4 answers`);
  }

  if (!Number.isInteger(correctAnswerIndex) || correctAnswerIndex < 0 || correctAnswerIndex > 3) {
    throw httpError(400, `Question ${index + 1} has an invalid correct answer`);
  }

  return {
    answers,
    correctAnswerIndex,
    id: cleanText(rawQuestion?.id, `q${index + 1}`),
    question
  };
}

// keep known mission targets, else fall back to custom
function normalizeMissionTarget(value) {
  const target = String(value || "").trim().toLowerCase();
  return MISSION_TARGETS.includes(target) ? target : "custom";
}

// validate + tidy up one mission
function cleanMission(rawMission, index) {
  const title = cleanText(rawMission?.title);
  const description = cleanText(rawMission?.description);

  if (!title) {
    throw httpError(400, `Mission ${index + 1} is missing a title`);
  }

  if (!description) {
    throw httpError(400, `Mission ${index + 1} is missing a description`);
  }

  return {
    description,
    expectedAnswer: cleanText(rawMission?.expectedAnswer),
    expectedChannel: cleanText(rawMission?.expectedChannel),
    id: cleanText(rawMission?.id, `m${index + 1}`),
    requiredAction: cleanText(rawMission?.requiredAction, title),
    target: normalizeMissionTarget(rawMission?.target),
    title
  };
}

const MISSION_TASKS = ["puzzle", "code", "missile"];

// pull the selected mission mini-games out of the body
function cleanMissionPayload(body) {
  const rawTasks = Array.isArray(body.tasks)
    ? body.tasks
    : (Array.isArray(body.payload?.tasks) ? body.payload.tasks : []);

  const tasks = [...new Set(
    rawTasks
      .map(task => String(task || "").trim().toLowerCase())
      .filter(task => MISSION_TASKS.includes(task))
  )];

  if (!tasks.length) {
    throw httpError(400, "בחרו לפחות משימה אחת לחדר המשימות");
  }

  return { exercises: [], tasks };
}

// build the right payload depending on type
function cleanPayload(type, body) {
  if (type === "trivia") {
    const questions = Array.isArray(body.questions)
      ? body.questions
      : (Array.isArray(body.payload?.questions) ? body.payload.questions : []);

    if (!questions.length) {
      throw httpError(400, "הוסיפו לפחות שאלת טריוויה אחת");
    }

    return {
      questions: questions.map(cleanQuestion)
    };
  }

  return cleanMissionPayload(body);
}

// db row -> activity shape we send out
function mapActivity(row, activeModes = []) {
  if (!row) {
    return null;
  }

  return {
    activeModes,
    createdAt: row.created_at || null,
    groupId: row.group_id,
    id: row.id,
    payload: row.payload || {},
    title: row.title || "",
    type: row.type,
    updatedAt: row.updated_at || null
  };
}

// db row -> result shape, with activity + username mixed in
function mapResult(row, activityMap = new Map(), profileMap = new Map()) {
  const activity = activityMap.get(row.activity_id) || null;
  const profile = profileMap.get(row.user_id) || null;

  return {
    activity: activity ? {
      id: activity.id,
      title: activity.title,
      type: activity.type
    } : null,
    activityId: row.activity_id,
    adminNote: row.admin_note || "",
    groupId: row.group_id,
    id: row.id,
    mode: row.mode,
    payload: row.payload || {},
    reviewedAt: row.reviewed_at || null,
    reviewedBy: row.reviewed_by || null,
    status: row.status,
    submittedAt: row.submitted_at || null,
    userId: row.user_id,
    username: profile?.username || "User"
  };
}

// grab usernames for a bunch of user ids
async function getProfilesById(client, userIds) {
  const ids = [...new Set((userIds || []).filter(Boolean))];

  if (!ids.length) {
    return new Map();
  }

  const profiles = await runActivityQuery(
    client
      .from("profiles")
      .select("id, username")
      .in("id", ids)
  );

  return new Map((profiles || []).map(profile => [profile.id, profile]));
}

// fetch one group or 404
async function getGroupRecord(client, groupId) {
  const group = await runActivityQuery(
    client
      .from("groups")
      .select("id, name, created_by")
      .eq("id", groupId)
      .maybeSingle()
  );

  if (!group) {
    throw httpError(404, "הקבוצה לא נמצאה");
  }

  return group;
}

// check if a user belongs to a group
async function getMembershipRecord(client, userId, groupId) {
  return runActivityQuery(
    client
      .from("user_groups")
      .select("group_id")
      .eq("group_id", groupId)
      .eq("user_id", userId)
      .maybeSingle()
  );
}

// make sure the caller is in the group, return their context
async function requireGroupMemberContext(accessToken, groupId) {
  const context = await getSessionContext(accessToken);
  const group = await getGroupRecord(context.client, groupId);

  if (group.created_by === context.user.id) {
    return {
      ...context,
      canManage: context.profile.role === "admin",
      group
    };
  }

  const membership = await getMembershipRecord(context.client, context.user.id, groupId);

  if (!membership) {
    throw httpError(404, "הקבוצה לא נמצאה");
  }

  return {
    ...context,
    canManage: context.profile.role === "admin",
    group
  };
}

// same but caller has to be an admin
async function requireGroupAdminContext(accessToken, groupId) {
  const context = await requireGroupMemberContext(accessToken, groupId);

  if (context.profile.role !== "admin") {
    throw httpError(403, "רק מנהלים יכולים לנהל את משחקי הקבוצה");
  }

  return context;
}

// fetch one activity in a group or 404
async function getActivityRecord(client, groupId, activityId) {
  const activity = await runActivityQuery(
    client
      .from(ACTIVITY_TABLE)
      .select("*")
      .eq("group_id", groupId)
      .eq("id", activityId)
      .maybeSingle()
  );

  if (!activity) {
    throw httpError(404, "המשחק לא נמצא");
  }

  return activity;
}

// map each activity id to the modes it's active in
async function getActivationsForActivities(client, groupId, activityIds) {
  if (!activityIds.length) {
    return new Map();
  }

  const activations = await runActivityQuery(
    client
      .from(ACTIVATION_TABLE)
      .select("activity_id, mode")
      .eq("group_id", groupId)
      .in("activity_id", activityIds)
  );

  const activeModesByActivityId = new Map();

  (activations || []).forEach(activation => {
    const modes = activeModesByActivityId.get(activation.activity_id) || [];
    modes.push(activation.mode);
    activeModesByActivityId.set(activation.activity_id, modes);
  });

  return activeModesByActivityId;
}

// list a group's activities (members only see active ones)
async function getGroupActivities(accessToken, groupId) {
  const context = await requireGroupMemberContext(accessToken, groupId);

  let activities = await runActivityQuery(
    context.client
      .from(ACTIVITY_TABLE)
      .select("*")
      .eq("group_id", groupId)
      .order("created_at", { ascending: false })
  );

  const activeModesByActivityId = await getActivationsForActivities(
    context.client,
    groupId,
    (activities || []).map(activity => activity.id)
  );

  if (!context.canManage) {
    activities = (activities || []).filter(activity => activeModesByActivityId.has(activity.id));
  }

  return (activities || []).map(activity => (
    mapActivity(activity, activeModesByActivityId.get(activity.id) || [])
  ));
}

// admin creates a new trivia/mission activity
async function createGroupActivity(accessToken, groupId, body = {}) {
  const context = await requireGroupAdminContext(accessToken, groupId);
  const type = cleanActivityType(body.type);
  const payload = cleanPayload(type, body);
  const title = cleanText(
    body.title,
    type === "trivia" ? "Trivia game" : "Mission room"
  );

  const activity = await runActivityQuery(
    context.client
      .from(ACTIVITY_TABLE)
      .insert({
        created_by: context.user.id,
        group_id: groupId,
        payload,
        title,
        type,
        updated_at: new Date().toISOString()
      })
      .select("*")
      .single()
  );

  return mapActivity(activity);
}

// admin turns an activity on for a given mode
async function activateGroupActivity(accessToken, groupId, activityId, body = {}) {
  const context = await requireGroupAdminContext(accessToken, groupId);
  const mode = cleanActivityMode(body.mode);
  const activity = await getActivityRecord(context.client, groupId, activityId);

  const activation = await runActivityQuery(
    context.client
      .from(ACTIVATION_TABLE)
      .upsert({
        activated_at: new Date().toISOString(),
        activated_by: context.user.id,
        activity_id: activity.id,
        group_id: groupId,
        mode
      }, {
        onConflict: "group_id,mode,activity_id"
      })
      .select("*")
      .single()
  );

  return {
    activity: mapActivity(activity, [mode]),
    activation: {
      activatedAt: activation.activated_at,
      activityId: activation.activity_id,
      groupId: activation.group_id,
      mode: activation.mode
    }
  };
}

// admin turns an activity off for a given mode
async function deactivateGroupActivity(accessToken, groupId, activityId, mode) {
  const context = await requireGroupAdminContext(accessToken, groupId);
  const cleanMode = cleanActivityMode(mode);
  const cleanId = cleanText(activityId);

  await runActivityQuery(
    context.client
      .from(ACTIVATION_TABLE)
      .delete()
      .eq("group_id", groupId)
      .eq("mode", cleanMode)
      .eq("activity_id", cleanId)
  );

  return {
    activityId: cleanId,
    groupId,
    mode: cleanMode,
    success: true
  };
}

// admin deletes an activity
async function deleteGroupActivity(accessToken, groupId, activityId) {
  const context = await requireGroupAdminContext(accessToken, groupId);
  await getActivityRecord(context.client, groupId, activityId);

  // activations + results get cleaned up by the cascade fks
  await runActivityQuery(
    context.client
      .from(ACTIVITY_TABLE)
      .delete()
      .eq("group_id", groupId)
      .eq("id", activityId)
  );

  return {
    id: activityId,
    success: true
  };
}

// get the active activities for a mode, in activation order
async function getActiveGroupActivities(accessToken, groupId, mode) {
  const context = await requireGroupMemberContext(accessToken, groupId);
  const cleanMode = cleanActivityMode(mode);

  const activations = await runActivityQuery(
    context.client
      .from(ACTIVATION_TABLE)
      .select("activity_id, activated_at")
      .eq("group_id", groupId)
      .eq("mode", cleanMode)
      .order("activated_at", { ascending: true })
  );

  const activityIds = [...new Set((activations || []).map(activation => activation.activity_id).filter(Boolean))];

  if (!activityIds.length) {
    return [];
  }

  const activities = await runActivityQuery(
    context.client
      .from(ACTIVITY_TABLE)
      .select("*")
      .eq("group_id", groupId)
      .in("id", activityIds)
  );

  const activityById = new Map((activities || []).map(activity => [activity.id, activity]));

  // keep the order they were turned on
  return (activations || [])
    .map(activation => activityById.get(activation.activity_id))
    .filter(Boolean)
    .map(activity => ({ ...mapActivity(activity, [cleanMode]), mode: cleanMode }));
}

// only keep a plain object payload, else empty
function normalizeResultPayload(payload) {
  if (!payload || typeof payload !== "object" || Array.isArray(payload)) {
    return {};
  }

  return payload;
}

// pull valid per-item metrics for the stats (skip null/missing so averages stay clean)
function cleanMetricItems(payload) {
  const rawItems = Array.isArray(payload?.items) ? payload.items : [];

  return rawItems.reduce((items, raw) => {
    const index = Number(raw?.index);

    if (!Number.isInteger(index) || index < 0) {
      return items;
    }

    const item = { index, label: cleanText(raw?.label, `#${index + 1}`) };
    const time = Number(raw?.timeSeconds);
    const rotation = Number(raw?.rotation);
    const mistakes = Number(raw?.mistakes ?? raw?.wrongAttempts ?? raw?.hits);

    if (Number.isFinite(time) && time >= 0) {
      item.timeSeconds = time;
    }

    if (Number.isFinite(rotation) && rotation >= 0) {
      item.rotation = rotation;
    }

    if (Number.isFinite(mistakes) && mistakes >= 0) {
      item.mistakes = mistakes;
    }

    if (typeof raw?.correct === "boolean") {
      item.correct = raw.correct;
    }

    items.push(item);
    return items;
  }, []);
}

// add a result's metrics to the running totals; best-effort so a missing table can't fail submit
async function applyActivityMetrics(client, groupId, activityId, mode, payload) {
  const items = cleanMetricItems(payload);

  if (!items.length) {
    return;
  }

  const { error } = await client.rpc("apply_activity_metrics", {
    p_activity_id: activityId,
    p_group_id: groupId,
    p_items: items,
    p_mode: mode
  });

  if (error) {
    console.warn("apply_activity_metrics failed (run supabase/activity_metrics.sql?):", error.message || error);
  }
}

// canonical ordered item list for an activity (questions for trivia, tasks for mission)
function activityItems(row) {
  if (row.type === "trivia") {
    const questions = Array.isArray(row.payload?.questions) ? row.payload.questions : [];
    return questions.map((question, index) => ({ index, label: `Q${index + 1}` }));
  }

  const tasks = Array.isArray(row.payload?.tasks) ? row.payload.tasks : [];
  return tasks.flatMap((task, taskIndex) => {
    const count = MISSION_TASK_STAGE_COUNTS[task] || 1;
    const base = MISSION_TASK_LABELS[task] || task;

    return Array.from({ length: count }, (_, stageIndex) => ({
      index: taskIndex * 10 + stageIndex,
      label: count > 1 ? `${base} - ${stageIndex + 1}` : base
    }));
  });
}

// member submits their answers, missions wait for review
async function submitGroupActivityResult(accessToken, groupId, body = {}) {
  const context = await requireGroupMemberContext(accessToken, groupId);
  const activityId = cleanText(body.activityId);
  const mode = cleanActivityMode(body.mode);
  const activity = await getActivityRecord(context.client, groupId, activityId);
  const activation = await runActivityQuery(
    context.client
      .from(ACTIVATION_TABLE)
      .select("activity_id")
      .eq("group_id", groupId)
      .eq("mode", mode)
      .eq("activity_id", activity.id)
      .maybeSingle()
  );

  if (!activation) {
    throw httpError(409, "המשחק כבר אינו פעיל עבור סוג האזעקה שנבחר");
  }

  const status = activity.type === "mission" ? "pending" : "approved";
  const result = await runActivityQuery(
    context.client
      .from(RESULT_TABLE)
      .insert({
        activity_id: activity.id,
        group_id: groupId,
        mode,
        payload: normalizeResultPayload(body.payload),
        reviewed_at: status === "approved" ? new Date().toISOString() : null,
        status,
        user_id: context.user.id
      })
      .select("*")
      .single()
  );

  // trivia auto-approves so add its metrics now; missions wait for admin approval
  if (status === "approved") {
    await applyActivityMetrics(context.client, groupId, activity.id, mode, body.payload);
  }

  const profileMap = new Map([[context.user.id, context.profile]]);
  const activityMap = new Map([[activity.id, activity]]);

  return mapResult(result, activityMap, profileMap);
}

// admin pulls all submitted results for the group
async function getGroupActivityResults(accessToken, groupId) {
  const context = await requireGroupAdminContext(accessToken, groupId);
  const results = await runActivityQuery(
    context.client
      .from(RESULT_TABLE)
      .select("*")
      .eq("group_id", groupId)
      .order("submitted_at", { ascending: false })
  );

  const activityIds = [...new Set((results || []).map(result => result.activity_id).filter(Boolean))];
  const userIds = [...new Set((results || []).map(result => result.user_id).filter(Boolean))];
  const activities = activityIds.length
    ? await runActivityQuery(
      context.client
        .from(ACTIVITY_TABLE)
        .select("*")
        .eq("group_id", groupId)
        .in("id", activityIds)
    )
    : [];
  const profileMap = await getProfilesById(context.client, userIds);
  const activityMap = new Map((activities || []).map(activity => [activity.id, activity]));

  return (results || []).map(result => mapResult(result, activityMap, profileMap));
}

// admin approves or rejects a submitted result
async function reviewGroupActivityResult(accessToken, groupId, resultId, body = {}) {
  const context = await requireGroupAdminContext(accessToken, groupId);
  const status = cleanResultStatus(body.status);

  if (status === "pending") {
    throw httpError(400, "סטטוס הסקירה חייב להיות אושר או נדחה");
  }

  // read the row first so we only aggregate metrics on the FIRST approval
  const existing = await runActivityQuery(
    context.client
      .from(RESULT_TABLE)
      .select("reviewed_at")
      .eq("group_id", groupId)
      .eq("id", resultId)
      .maybeSingle()
  );

  const updatedResult = await runActivityQuery(
    context.client
      .from(RESULT_TABLE)
      .update({
        admin_note: cleanText(body.adminNote),
        reviewed_at: new Date().toISOString(),
        reviewed_by: context.user.id,
        status
      })
      .eq("group_id", groupId)
      .eq("id", resultId)
      .select("*")
      .single()
  );

  const profileMap = await getProfilesById(context.client, [updatedResult.user_id]);
  const activity = await getActivityRecord(context.client, groupId, updatedResult.activity_id);
  const activityMap = new Map([[activity.id, activity]]);

  // add mission metrics only on the first approval, so re-reviews don't double-count
  if (status === "approved" && activity.type === "mission" && existing && !existing.reviewed_at) {
    await applyActivityMetrics(
      context.client,
      groupId,
      updatedResult.activity_id,
      updatedResult.mode,
      updatedResult.payload
    );
  }

  return mapResult(updatedResult, activityMap, profileMap);
}

// admin-only: all the data the statistics page needs (members, activities, totals, latest results)
async function getGroupStatistics(accessToken, groupId) {
  const context = await requireGroupAdminContext(accessToken, groupId);

  const memberships = await runActivityQuery(
    context.client
      .from("user_groups")
      .select("user_id, member_username")
      .eq("group_id", groupId)
  );

  const memberIds = [...new Set([
    ...(memberships || []).map(membership => membership.user_id),
    context.group.created_by
  ].filter(Boolean))];
  const profileMap = await getProfilesById(context.client, memberIds);
  const usernameFallback = new Map((memberships || []).map(membership => [membership.user_id, membership.member_username]));
  const members = memberIds.map(id => ({
    userId: id,
    username: profileMap.get(id)?.username || usernameFallback.get(id) || "User"
  }));

  const activityRows = await runActivityQuery(
    context.client
      .from(ACTIVITY_TABLE)
      .select("*")
      .eq("group_id", groupId)
      .order("created_at", { ascending: false })
  );
  const activities = (activityRows || []).map(row => ({
    id: row.id,
    items: activityItems(row),
    title: row.title || "",
    type: row.type
  }));

  // running aggregates — best-effort so stats degrade gracefully if the table is missing
  let aggregates = [];
  const aggResponse = await context.client
    .from("activity_metric_aggregates")
    .select("activity_id, item_index, item_label, mode, metric, avg_value, sample_count")
    .eq("group_id", groupId);

  if (aggResponse.error) {
    console.warn("activity_metric_aggregates read failed (run supabase/activity_metrics.sql?):", aggResponse.error.message || aggResponse.error);
  } else {
    aggregates = (aggResponse.data || []).map(row => ({
      activityId: row.activity_id,
      avgValue: Number(row.avg_value),
      itemIndex: row.item_index,
      itemLabel: row.item_label || "",
      metric: row.metric,
      mode: row.mode,
      sampleCount: row.sample_count
    }));
  }

  const resultRows = await runActivityQuery(
    context.client
      .from(RESULT_TABLE)
      .select("user_id, activity_id, mode, payload, submitted_at")
      .eq("group_id", groupId)
      .order("submitted_at", { ascending: false })
  );

  // rows are newest-first, so the first one seen per (user, activity) is the latest
  const latestByKey = new Map();
  (resultRows || []).forEach(row => {
    const key = `${row.user_id}:${row.activity_id}`;
    if (latestByKey.has(key)) {
      return;
    }

    latestByKey.set(key, {
      activityId: row.activity_id,
      games: Array.isArray(row.payload?.games) ? row.payload.games : [],
      items: Array.isArray(row.payload?.items) ? row.payload.items : [],
      mode: row.mode,
      submittedAt: row.submitted_at,
      userId: row.user_id
    });
  });

  return {
    activities,
    aggregates,
    latestResults: [...latestByKey.values()],
    members
  };
}

module.exports = {
  activateGroupActivity,
  createGroupActivity,
  deactivateGroupActivity,
  deleteGroupActivity,
  getActiveGroupActivities,
  getGroupActivities,
  getGroupActivityResults,
  getGroupStatistics,
  reviewGroupActivityResult,
  submitGroupActivityResult
};
