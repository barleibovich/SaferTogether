const { httpError } = require("./errors");
const { getManageableGroupRecord, requireAdminContext } = require("./groupService");
const { getSessionContext } = require("./supabaseService");

const ALARM_TABLE = "group_alarms";
const SAFE_TABLE = "group_alarm_safe_users";
const PROGRESS_TABLE = "group_alarm_progress";

// is this error because the alarm tables aren't set up yet?
function isMissingAlarmTableError(error) {
  const code = String(error?.code || "");
  const message = String(error?.message || "").toLowerCase();

  return (
    ["42P01", "42703", "PGRST204", "PGRST205"].includes(code) ||
    (message.includes("group_alarm") && (
      message.includes("does not exist") ||
      message.includes("schema cache") ||
      message.includes("column")
    ))
  );
}

// missing tables -> tell them how to fix it
function missingAlarmTableError() {
  return httpError(
    500,
    "The group alarm tables are missing. Run supabase/group_alarms.sql in Supabase first."
  );
}

// the member-raise DEFINER function hasn't been installed yet?
function isMissingOrefRaiseFunctionError(error) {
  const code = String(error?.code || "");
  const message = String(error?.message || "").toLowerCase();

  return (
    code === "42883" ||
    code === "PGRST202" ||
    message.includes("could not find the function") ||
    (message.includes("raise_group_oref_alarm") && message.includes("does not exist"))
  );
}

// db row -> the shape we send out
function mapAlarm(row) {
  if (!row) {
    return null;
  }

  return {
    groupId: row.group_id,
    id: row.id,
    mode: row.mode,
    startedAt: row.started_at || null,
    status: row.status,
    unlocked: Boolean(row.unlocked),
    unlockedAt: row.unlocked_at || null
  };
}

// the active alarm row for a group, if any
async function findActiveAlarm(client, groupId) {
  const { data, error } = await client
    .from(ALARM_TABLE)
    .select("*")
    .eq("group_id", groupId)
    .eq("status", "active")
    .order("started_at", { ascending: false })
    .limit(1)
    .maybeSingle();

  if (error) {
    throw error;
  }

  return data || null;
}

// every member id of a group (membership rows + the owner, mirroring the board)
async function getGroupMemberIds(client, groupId) {
  const { data, error } = await client
    .from("user_groups")
    .select("user_id")
    .eq("group_id", groupId);

  if (error) {
    throw error;
  }

  const ids = new Set((data || []).map(row => row.user_id).filter(Boolean));

  const { data: group } = await client
    .from("groups")
    .select("created_by")
    .eq("id", groupId)
    .maybeSingle();

  if (group?.created_by) {
    ids.add(group.created_by);
  }

  return ids;
}

// who has marked safe for this alarm
async function getSafeUserIds(client, alarmId) {
  const { data, error } = await client
    .from(SAFE_TABLE)
    .select("user_id")
    .eq("alarm_id", alarmId);

  if (error) {
    throw error;
  }

  return (data || []).map(row => row.user_id);
}

// live play progress for everyone in this alarm
async function getAlarmProgress(client, alarmId) {
  const { data, error } = await client
    .from(PROGRESS_TABLE)
    .select("user_id, activity_id, activity_type, completed, total")
    .eq("alarm_id", alarmId);

  if (error) {
    throw error;
  }

  return (data || []).map(row => ({
    activityId: row.activity_id,
    activityType: row.activity_type,
    completed: row.completed,
    total: row.total,
    userId: row.user_id
  }));
}

// admin raises an alarm for a group (ends any previous active one first).
async function startAlarm(accessToken, groupId, mode) {
  const cleanMode = mode === "training" ? "training" : (mode === "real" ? "real" : null);

  if (!cleanMode) {
    throw httpError(400, "המצב חייב להיות 'real' או 'training'");
  }

  const context = await requireAdminContext(accessToken);
  await getManageableGroupRecord(context.client, context.user.id, groupId);

  try {
    await context.client
      .from(ALARM_TABLE)
      .update({ ended_at: new Date().toISOString(), status: "ended" })
      .eq("group_id", groupId)
      .eq("status", "active");

    const { data, error } = await context.client
      .from(ALARM_TABLE)
      .insert({
        group_id: groupId,
        mode: cleanMode,
        started_by: context.user.id,
        status: "active",
        unlocked: false
      })
      .select("*")
      .single();

    if (error) {
      throw error;
    }

    return mapAlarm(data);
  } catch (error) {
    if (isMissingAlarmTableError(error)) {
      throw missingAlarmTableError();
    }

    throw error;
  }
}

// any member raises a real alarm via the DEFINER function (bypasses admin RLS); returns { alarm, created }
async function startOrefAlarm(accessToken, groupId) {
  const context = await getSessionContext(accessToken);

  try {
    const { data, error } = await context.client.rpc("raise_group_oref_alarm", {
      p_group_id: groupId
    });

    if (error) {
      throw error;
    }

    return {
      alarm: mapAlarm(data?.alarm),
      created: Boolean(data?.created)
    };
  } catch (error) {
    if (isMissingOrefRaiseFunctionError(error)) {
      throw httpError(
        500,
        "The member-raise function is missing. Run supabase/group_alarm_oref_raise.sql in Supabase first."
      );
    }
    if (isMissingAlarmTableError(error)) {
      throw missingAlarmTableError();
    }

    throw error;
  }
}

// current alarm state for a group (any member can read); "no alarm" if tables are missing
async function getActiveAlarm(accessToken, groupId) {
  const context = await getSessionContext(accessToken);

  try {
    const alarm = await findActiveAlarm(context.client, groupId);

    if (!alarm) {
      return { alarm: null, progress: [], safeUserIds: [], unlocked: false };
    }

    const safeUserIds = await getSafeUserIds(context.client, alarm.id);
    const progress = await getAlarmProgress(context.client, alarm.id);

    return {
      alarm: mapAlarm(alarm),
      progress,
      safeUserIds,
      unlocked: Boolean(alarm.unlocked)
    };
  } catch (error) {
    if (isMissingAlarmTableError(error)) {
      return { alarm: null, progress: [], safeUserIds: [], unlocked: false };
    }

    throw error;
  }
}

// a member reports their progress (e.g. trivia 2/3); no-ops if no alarm or tables missing
async function reportAlarmProgress(accessToken, groupId, body = {}) {
  const context = await getSessionContext(accessToken);
  const activityId = body.activityId;

  if (!activityId) {
    return { reported: false };
  }

  try {
    const alarm = await findActiveAlarm(context.client, groupId);

    if (!alarm) {
      return { reported: false };
    }

    const { error } = await context.client
      .from(PROGRESS_TABLE)
      .upsert({
        activity_id: activityId,
        activity_type: body.type === "mission" ? "mission" : "trivia",
        alarm_id: alarm.id,
        completed: Math.max(0, Number(body.completed) || 0),
        group_id: groupId,
        total: Math.max(0, Number(body.total) || 0),
        updated_at: new Date().toISOString(),
        user_id: context.user.id
      }, {
        onConflict: "alarm_id,user_id,activity_id"
      });

    if (error) {
      throw error;
    }

    return { reported: true };
  } catch (error) {
    if (isMissingAlarmTableError(error)) {
      return { reported: false };
    }

    throw error;
  }
}

// mark me safe in the active alarm; unlocks the activities once everyone is safe
async function markAlarmSafe(accessToken, groupId) {
  const context = await getSessionContext(accessToken);

  try {
    const alarm = await findActiveAlarm(context.client, groupId);

    if (!alarm) {
      throw httpError(400, "אין אזעקה פעילה לקבוצה זו");
    }

    const { error } = await context.client
      .from(SAFE_TABLE)
      .upsert({
        alarm_id: alarm.id,
        group_id: groupId,
        safe_at: new Date().toISOString(),
        user_id: context.user.id
      }, {
        ignoreDuplicates: true,
        onConflict: "alarm_id,user_id"
      });

    if (error) {
      throw error;
    }

    const safeUserIds = await getSafeUserIds(context.client, alarm.id);
    let unlocked = Boolean(alarm.unlocked);

    if (!unlocked) {
      const memberIds = await getGroupMemberIds(context.client, groupId);
      const safeSet = new Set(safeUserIds);
      const allSafe = memberIds.size > 0 && [...memberIds].every(id => safeSet.has(id));

      if (allSafe) {
        const { error: unlockError } = await context.client
          .from(ALARM_TABLE)
          .update({ unlocked: true, unlocked_at: new Date().toISOString() })
          .eq("id", alarm.id)
          .eq("status", "active");

        if (!unlockError) {
          unlocked = true;
        }
      }
    }

    return { safeUserIds, unlocked };
  } catch (error) {
    if (isMissingAlarmTableError(error)) {
      throw missingAlarmTableError();
    }

    throw error;
  }
}

// admin manually opens the activities for everyone (override the all-safe gate).
async function unlockAlarm(accessToken, groupId) {
  const context = await requireAdminContext(accessToken);
  await getManageableGroupRecord(context.client, context.user.id, groupId);

  try {
    const alarm = await findActiveAlarm(context.client, groupId);

    if (!alarm) {
      throw httpError(400, "אין אזעקה פעילה לקבוצה זו");
    }

    const { error } = await context.client
      .from(ALARM_TABLE)
      .update({ unlocked: true, unlocked_at: new Date().toISOString() })
      .eq("id", alarm.id);

    if (error) {
      throw error;
    }

    const safeUserIds = await getSafeUserIds(context.client, alarm.id);
    return { safeUserIds, unlocked: true };
  } catch (error) {
    if (isMissingAlarmTableError(error)) {
      throw missingAlarmTableError();
    }

    throw error;
  }
}

// admin ends the active alarm for a group.
async function endAlarm(accessToken, groupId) {
  const context = await requireAdminContext(accessToken);
  await getManageableGroupRecord(context.client, context.user.id, groupId);

  try {
    await context.client
      .from(ALARM_TABLE)
      .update({ ended_at: new Date().toISOString(), status: "ended" })
      .eq("group_id", groupId)
      .eq("status", "active");

    return { success: true };
  } catch (error) {
    if (isMissingAlarmTableError(error)) {
      throw missingAlarmTableError();
    }

    throw error;
  }
}

module.exports = {
  endAlarm,
  getActiveAlarm,
  getGroupMemberIds,
  markAlarmSafe,
  reportAlarmProgress,
  startAlarm,
  startOrefAlarm,
  unlockAlarm
};
