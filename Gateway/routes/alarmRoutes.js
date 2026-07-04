const {
  getAccessTokenFromRequest,
  readJsonBody,
  sendJson,
  sendRouteError
} = require("../http");
const {
  endAlarm,
  getActiveAlarm,
  markAlarmSafe,
  reportAlarmProgress,
  startAlarm,
  startOrefAlarm,
  unlockAlarm
} = require("../../Services/alarmService");
const { sendAlarmPushToGroup } = require("../../Services/pushService");
const { getGroupOrefStatus } = require("./orefRoutes");

// handle the group alarm routes (broadcast + "I'm safe" + unlock + progress)
async function handleAlarmRoute(request, response, pathname) {
  const accessToken = getAccessTokenFromRequest(request);
  const alarmMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm$/);
  const alarmOrefMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm\/oref$/);
  const alarmSafeMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm\/safe$/);
  const alarmUnlockMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm\/unlock$/);
  const alarmProgressMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm\/progress$/);

  try {
    if (alarmMatch && request.method === "POST") {
      const body = await readJsonBody(request);
      const alarm = await startAlarm(accessToken, alarmMatch[1], body.mode);
      sendJson(response, 200, { alarm });
      // push the alarm to the group's phones; never let a push error break the raise
      sendAlarmPushToGroup(accessToken, alarmMatch[1], alarm).catch(error => {
        console.error("alarm push failed:", error?.message || error);
      });
      return true;
    }

    // member auto-raise on a live HFC alert: check the alert is real, then raise
    // once and push the group only if we actually created the alarm
    if (alarmOrefMatch && request.method === "POST") {
      const groupId = alarmOrefMatch[1];
      const status = await getGroupOrefStatus(accessToken, groupId);

      if (!status?.hasGroupAlert) {
        sendJson(response, 409, {
          error: "אין כרגע התרעת פיקוד העורף פעילה עבור אזור הקבוצה"
        });
        return true;
      }

      const { alarm, created } = await startOrefAlarm(accessToken, groupId);
      sendJson(response, 200, { alarm });

      if (created) {
        sendAlarmPushToGroup(accessToken, groupId, alarm).catch(error => {
          console.error("oref alarm push failed:", error?.message || error);
        });
      }
      return true;
    }

    if (alarmMatch && request.method === "GET") {
      const result = await getActiveAlarm(accessToken, alarmMatch[1]);
      sendJson(response, 200, result);
      return true;
    }

    if (alarmMatch && request.method === "DELETE") {
      const result = await endAlarm(accessToken, alarmMatch[1]);
      sendJson(response, 200, result);
      return true;
    }

    if (alarmSafeMatch && request.method === "POST") {
      const result = await markAlarmSafe(accessToken, alarmSafeMatch[1]);
      sendJson(response, 200, result);
      return true;
    }

    if (alarmUnlockMatch && request.method === "POST") {
      const result = await unlockAlarm(accessToken, alarmUnlockMatch[1]);
      sendJson(response, 200, result);
      return true;
    }

    if (alarmProgressMatch && request.method === "POST") {
      const body = await readJsonBody(request);
      const result = await reportAlarmProgress(accessToken, alarmProgressMatch[1], body);
      sendJson(response, 200, result);
      return true;
    }
  } catch (error) {
    sendRouteError(response, error);
    return true;
  }

  return false;
}

module.exports = {
  handleAlarmRoute
};
