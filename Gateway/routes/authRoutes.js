const {
  clearAuthCookies,
  getAccessTokenFromRequest,
  readJsonBody,
  sendJson,
  sendRouteError,
  setAuthCookies
} = require("../http");
const {
  getCurrentUserProfile,
  loginWithUsername,
  logout,
  signUpWithUsername
} = require("../../Services/authService");

// This function handles all the auth routes.
async function handleAuthRoute(request, response, pathname) {
  try {
    if (pathname === "/api/auth/signup" && request.method === "POST") {
      const body = await readJsonBody(request);
      const result = await signUpWithUsername(body);
      setAuthCookies(response, result.session);
      sendJson(response, 201, { profile: result.profile });
      return true;
    }

    if (pathname === "/api/auth/login" && request.method === "POST") {
      const body = await readJsonBody(request);
      const result = await loginWithUsername(body);
      setAuthCookies(response, result.session);
      sendJson(response, 200, { profile: result.profile });
      return true;
    }

    if (pathname === "/api/auth/logout" && request.method === "POST") {
      const accessToken = getAccessTokenFromRequest(request);
      try {
        await logout(accessToken);
      } catch {
        // Clearing cookies is enough for the current prototype flow.
      }
      clearAuthCookies(response);
      sendJson(response, 200, { success: true });
      return true;
    }

    if (pathname === "/api/auth/profile" && request.method === "GET") {
      const accessToken = getAccessTokenFromRequest(request);
      const profile = await getCurrentUserProfile(accessToken);
      sendJson(response, 200, { profile });
      return true;
    }
  } catch (error) {
    sendRouteError(response, error);
    return true;
  }

  return false;
}

module.exports = {
  handleAuthRoute
};
