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
  unlockAlarm
} = require("../../Services/alarmService");

// handle the group alarm routes (broadcast + "I'm safe" + unlock + progress)
async function handleAlarmRoute(request, response, pathname) {
  const accessToken = getAccessTokenFromRequest(request);
  const alarmMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm$/);
  const alarmSafeMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm\/safe$/);
  const alarmUnlockMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm\/unlock$/);
  const alarmProgressMatch = pathname.match(/^\/api\/groups\/([^/]+)\/alarm\/progress$/);

  try {
    if (alarmMatch && request.method === "POST") {
      const body = await readJsonBody(request);
      const alarm = await startAlarm(accessToken, alarmMatch[1], body.mode);
      sendJson(response, 200, { alarm });
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
