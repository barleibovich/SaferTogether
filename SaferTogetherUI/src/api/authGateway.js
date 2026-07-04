import { requestJson, setSessionToken, setRefreshToken } from "./apiClient.js";

// create a user account
export async function signUpWithUsername({ username, password, role, avatar, avatarImage }) {
  const result = await requestJson("/api/auth/signup", {
    body: { avatar, avatarImage, password, role, username },
    method: "POST"
  });
  setSessionToken(result?.accessToken);
  setRefreshToken(result?.refreshToken);
  return result;
}

// log in with username + password
export async function loginWithUsername({ username, password }) {
  const result = await requestJson("/api/auth/login", {
    body: { password, username },
    method: "POST"
  });
  setSessionToken(result?.accessToken);
  setRefreshToken(result?.refreshToken);
  return result;
}

// log out
export async function logout() {
  const result = await requestJson("/api/auth/logout", { method: "POST" });
  setSessionToken(null);
  setRefreshToken(null);
  return result;
}

// grab the current user's profile
export async function getCurrentUserProfile() {
  const payload = await requestJson("/api/auth/profile");
  return payload.profile;
}

// save the user's avatar
export async function updateCurrentUserAvatar({ avatar, avatarImage }) {
  const payload = await requestJson("/api/auth/profile", {
    body: { avatar, avatarImage },
    method: "PATCH"
  });

  return payload.profile;
}
