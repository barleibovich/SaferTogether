import { requestJson, setSessionToken } from "./apiClient.js";

// This function creates a user account.
export async function signUpWithUsername({ username, password, role, avatar }) {
  const result = await requestJson("/api/auth/signup", {
    body: { avatar, password, role, username },
    method: "POST"
  });
  setSessionToken(result?.accessToken);
  return result;
}

// This function logs in with a username and password.
export async function loginWithUsername({ username, password }) {
  const result = await requestJson("/api/auth/login", {
    body: { password, username },
    method: "POST"
  });
  setSessionToken(result?.accessToken);
  return result;
}

// This function logs out the current user.
export async function logout() {
  const result = await requestJson("/api/auth/logout", { method: "POST" });
  setSessionToken(null);
  return result;
}

// This function gets the current user's profile.
export async function getCurrentUserProfile() {
  const payload = await requestJson("/api/auth/profile");
  return payload.profile;
}

// This function saves the current user's avatar.
export async function updateCurrentUserAvatar({ avatar }) {
  const payload = await requestJson("/api/auth/profile", {
    body: { avatar },
    method: "PATCH"
  });

  return payload.profile;
}
