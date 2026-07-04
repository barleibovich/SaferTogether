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
  refreshSession,
  signUpWithUsername,
  updateCurrentUserAlertLocation,
  updateCurrentUserAvatar,
  updateCurrentUserCredentials
} = require("../../Services/authService");

// handle all the auth routes
async function handleAuthRoute(request, response, pathname) {
  try {
    if (pathname === "/api/auth/signup" && request.method === "POST") {
      const body = await readJsonBody(request);
      const result = await signUpWithUsername(body);
      setAuthCookies(response, result.session);
      sendJson(response, 201, {
        accessToken: result.session.access_token,
        profile: result.profile,
        refreshToken: result.session.refresh_token
      });
      return true;
    }

    if (pathname === "/api/auth/login" && request.method === "POST") {
      const body = await readJsonBody(request);
      const result = await loginWithUsername(body);
      setAuthCookies(response, result.session);
      sendJson(response, 200, {
        accessToken: result.session.access_token,
        profile: result.profile,
        refreshToken: result.session.refresh_token
      });
      return true;
    }

    if (pathname === "/api/auth/refresh" && request.method === "POST") {
      const body = await readJsonBody(request);
      const result = await refreshSession(body?.refreshToken);
      sendJson(response, 200, {
        accessToken: result.accessToken,
        profile: result.profile,
        refreshToken: result.refreshToken
      });
      return true;
    }

    if (pathname === "/api/auth/logout" && request.method === "POST") {
      const accessToken = getAccessTokenFromRequest(request);
      try {
        await logout(accessToken);
      } catch {
        // just clearing the cookies is fine for now
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

    if (pathname === "/api/auth/profile" && request.method === "PATCH") {
      const accessToken = getAccessTokenFromRequest(request);
      const body = await readJsonBody(request);
      const profile = await updateCurrentUserAvatar(accessToken, body);
      sendJson(response, 200, { profile });
      return true;
    }

    if (pathname === "/api/auth/credentials" && request.method === "PATCH") {
      const accessToken = getAccessTokenFromRequest(request);
      const body = await readJsonBody(request);
      const profile = await updateCurrentUserCredentials(accessToken, body);
      sendJson(response, 200, { profile });
      return true;
    }

    if (pathname === "/api/auth/location" && request.method === "PATCH") {
      const accessToken = getAccessTokenFromRequest(request);
      const body = await readJsonBody(request);
      const alertLocation = await updateCurrentUserAlertLocation(accessToken, body);
      sendJson(response, 200, { alertLocation });
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
