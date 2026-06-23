const {
  getAccessTokenFromRequest,
  sendJson,
  sendRouteError
} = require("../http");
const { recordHeartbeat } = require("../../Services/presenceService");

// handle the presence heartbeat route (marks the current user as connected)
async function handlePresenceRoute(request, response, pathname) {
  try {
    if (pathname === "/api/presence" && request.method === "POST") {
      const accessToken = getAccessTokenFromRequest(request);
      const result = await recordHeartbeat(accessToken);
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
  handlePresenceRoute
};
