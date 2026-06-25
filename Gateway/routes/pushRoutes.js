const {
  getAccessTokenFromRequest,
  readJsonBody,
  sendJson,
  sendRouteError
} = require("../http");
const {
  deleteSubscription,
  getPublicConfig,
  saveSubscription
} = require("../../Services/pushService");

// handle the web-push routes (public VAPID key + subscription register/remove)
async function handlePushRoute(request, response, pathname) {
  try {
    // public VAPID key + whether push is configured — no auth needed
    if (pathname === "/api/push/config" && request.method === "GET") {
      sendJson(response, 200, getPublicConfig());
      return true;
    }

    if (pathname === "/api/push/subscriptions" && request.method === "POST") {
      const accessToken = getAccessTokenFromRequest(request);
      const body = await readJsonBody(request);
      const result = await saveSubscription(accessToken, body.subscription, body.userAgent);
      sendJson(response, 200, result);
      return true;
    }

    if (pathname === "/api/push/subscriptions" && request.method === "DELETE") {
      const accessToken = getAccessTokenFromRequest(request);
      const body = await readJsonBody(request);
      const result = await deleteSubscription(accessToken, body.endpoint);
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
  handlePushRoute
};
