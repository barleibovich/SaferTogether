const path = require("path");
const { createRequire } = require("module");
const { getConfig } = require("./configService");
const { httpError } = require("./errors");
const { getSessionContext } = require("./supabaseService");
const { getGroupMemberIds } = require("./alarmService");

const requireFromGateway = createRequire(path.join(__dirname, "..", "Gateway", "package.json"));

const SUBSCRIPTION_TABLE = "push_subscriptions";

// undefined = not loaded yet, null = unavailable (missing dep or unconfigured)
let webpushModule = undefined;

// load web-push once and wire the VAPID keys. Returns null when push can't run
// (web-push not installed, or VAPID keys not configured) so callers degrade.
function loadWebPush() {
  if (webpushModule !== undefined) {
    return webpushModule;
  }

  try {
    let mod;
    try {
      mod = require("web-push");
    } catch {
      mod = requireFromGateway("web-push");
    }

    const { vapidPublicKey, vapidPrivateKey, vapidSubject } = getConfig();

    if (vapidPublicKey && vapidPrivateKey) {
      mod.setVapidDetails(vapidSubject || "mailto:alerts@safertogether.app", vapidPublicKey, vapidPrivateKey);
      webpushModule = mod;
    } else {
      webpushModule = null;
    }
  } catch {
    webpushModule = null;
  }

  return webpushModule;
}

function isPushEnabled() {
  return Boolean(loadWebPush());
}

// missing-table detection, mirroring alarmService's graceful degradation
function isMissingTableError(error) {
  const code = String(error?.code || "");
  const message = String(error?.message || "").toLowerCase();

  return (
    ["42P01", "42703", "PGRST204", "PGRST205"].includes(code) ||
    (message.includes("push_subscriptions") && (
      message.includes("does not exist") ||
      message.includes("schema cache") ||
      message.includes("column")
    ))
  );
}

function missingTableError() {
  return httpError(
    500,
    "The push_subscriptions table is missing. Run supabase/push_subscriptions.sql in Supabase first."
  );
}

// what the browser needs to subscribe: the VAPID public key (safe to expose).
function getPublicConfig() {
  const { vapidPublicKey } = getConfig();
  const enabled = isPushEnabled();
  return {
    enabled,
    publicKey: enabled ? vapidPublicKey : ""
  };
}

// store (or refresh) the current user's push subscription
async function saveSubscription(accessToken, subscription, userAgent = "") {
  if (!subscription?.endpoint || !subscription?.keys?.p256dh || !subscription?.keys?.auth) {
    throw httpError(400, "Invalid push subscription");
  }

  const { client, user } = await getSessionContext(accessToken);

  try {
    const { error } = await client
      .from(SUBSCRIPTION_TABLE)
      .upsert({
        auth: subscription.keys.auth,
        endpoint: subscription.endpoint,
        p256dh: subscription.keys.p256dh,
        updated_at: new Date().toISOString(),
        user_agent: userAgent ? String(userAgent).slice(0, 400) : null,
        user_id: user.id
      }, {
        onConflict: "endpoint"
      });

    if (error) {
      throw error;
    }

    return { saved: true };
  } catch (error) {
    if (isMissingTableError(error)) {
      throw missingTableError();
    }

    throw error;
  }
}

// drop a subscription (e.g. the browser revoked it or the user logged out)
async function deleteSubscription(accessToken, endpoint) {
  if (!endpoint) {
    return { deleted: false };
  }

  const { client } = await getSessionContext(accessToken);

  try {
    const { error } = await client
      .from(SUBSCRIPTION_TABLE)
      .delete()
      .eq("endpoint", endpoint);

    if (error) {
      throw error;
    }

    return { deleted: true };
  } catch (error) {
    if (isMissingTableError(error)) {
      return { deleted: false };
    }

    throw error;
  }
}

// push an alarm to every group member's phone. Best-effort and side-channel:
// callers fire-and-forget this so it never blocks/breaks raising the alarm.
// Uses the alarm-raising admin's session — RLS (shares_group_with) lets that
// session read co-members' subscriptions server-side; the keys never leave here.
async function sendAlarmPushToGroup(accessToken, groupId, alarm, options = {}) {
  const mod = loadWebPush();
  if (!mod) {
    return { sent: 0, skipped: true };
  }

  const { client, user } = await getSessionContext(accessToken);
  const excludeUserId = options.excludeUserId || user.id;

  const memberIds = await getGroupMemberIds(client, groupId);
  const targetIds = [...memberIds].filter(id => id && id !== excludeUserId);
  if (!targetIds.length) {
    return { sent: 0 };
  }

  let subscriptions = [];
  try {
    const { data, error } = await client
      .from(SUBSCRIPTION_TABLE)
      .select("endpoint, p256dh, auth, user_id")
      .in("user_id", targetIds);

    if (error) {
      throw error;
    }

    subscriptions = data || [];
  } catch (error) {
    if (isMissingTableError(error)) {
      return { sent: 0, skipped: true };
    }
    throw error;
  }

  if (!subscriptions.length) {
    return { sent: 0 };
  }

  const mode = alarm?.mode === "training" ? "training" : "real";
  const payload = JSON.stringify({
    body: mode === "training"
      ? "המנהל הפעיל תרגול. היכנסו למרחב מוגן וסמנו שאתם מוגנים."
      : "התקבלה אזעקה. היכנסו עכשיו למרחב מוגן וסמנו שאתם מוגנים.",
    mode,
    tag: `alarm-${alarm?.id || mode}`,
    title: mode === "training" ? "תרגול התחיל 🟠" : "אזעקת אמת 🔴",
    url: "/emergency.html"
  });

  const deadEndpoints = [];
  let sent = 0;

  await Promise.all(subscriptions.map(async sub => {
    try {
      await mod.sendNotification(
        { endpoint: sub.endpoint, keys: { auth: sub.auth, p256dh: sub.p256dh } },
        payload,
        { TTL: 300, urgency: "high" }
      );
      sent += 1;
    } catch (error) {
      const status = error?.statusCode;
      if (status === 404 || status === 410) {
        deadEndpoints.push(sub.endpoint);
      } else {
        console.error("push send failed:", status || "", error?.body || error?.message || error);
      }
    }
  }));

  if (deadEndpoints.length) {
    try {
      await client.from(SUBSCRIPTION_TABLE).delete().in("endpoint", deadEndpoints);
    } catch {
      // best effort cleanup of expired endpoints
    }
  }

  return { sent };
}

module.exports = {
  deleteSubscription,
  getPublicConfig,
  isPushEnabled,
  saveSubscription,
  sendAlarmPushToGroup
};
