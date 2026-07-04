const { getSessionContext } = require("./supabaseService");

const PRESENCE_TABLE = "profile_presence";

// is this error because the presence table isn't set up yet?
function isMissingPresenceTableError(error) {
  const code = String(error?.code || "");
  const message = String(error?.message || "").toLowerCase();

  return (
    ["42P01", "42703", "PGRST204", "PGRST205"].includes(code) ||
    (message.includes(PRESENCE_TABLE) && (
      message.includes("does not exist") ||
      message.includes("schema cache") ||
      message.includes("column")
    ))
  );
}

// mark the current user online now; no-ops if the table isn't set up yet
async function recordHeartbeat(accessToken) {
  const context = await getSessionContext(accessToken);

  const { error } = await context.client
    .from(PRESENCE_TABLE)
    .upsert({
      last_seen_at: new Date().toISOString(),
      user_id: context.user.id
    }, {
      onConflict: "user_id"
    });

  if (error) {
    if (isMissingPresenceTableError(error)) {
      return { presenceDisabled: true, success: false };
    }

    throw error;
  }

  return { success: true };
}

// get the last-seen timestamp for a bunch of users (degrades to empty if missing)
async function getPresenceForUsers(client, userIds) {
  const ids = [...new Set((userIds || []).filter(Boolean))];

  if (!ids.length) {
    return new Map();
  }

  const { data, error } = await client
    .from(PRESENCE_TABLE)
    .select("user_id, last_seen_at")
    .in("user_id", ids);

  if (error) {
    // reads should never break the board — just report nobody as online
    if (isMissingPresenceTableError(error)) {
      return new Map();
    }

    throw error;
  }

  return new Map((data || []).map(row => [row.user_id, row.last_seen_at || null]));
}

module.exports = {
  getPresenceForUsers,
  recordHeartbeat
};
