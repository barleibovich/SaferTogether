import { requestJson } from "./apiClient.js";

// This function creates a user account.
export async function signUpWithUsername({ username, password, role, avatar, avatarImage }) {
  return requestJson("/api/auth/signup", {
    body: { avatar, avatarImage, password, role, username },
    method: "POST"
  });
}

// This function logs in with a username and password.
export async function loginWithUsername({ username, password }) {
  return requestJson("/api/auth/login", {
    body: { password, username },
    method: "POST"
  });
}

// This function logs out the current user.
export async function logout() {
  return requestJson("/api/auth/logout", {
    method: "POST"
  });
}

// This function gets the current user's profile.
export async function getCurrentUserProfile() {
  const payload = await requestJson("/api/auth/profile");
  return payload.profile;
}

// This function saves the current user's avatar.
export async function updateCurrentUserAvatar({ avatar, avatarImage }) {
  const payload = await requestJson("/api/auth/profile", {
    body: { avatar, avatarImage },
    method: "PATCH"
  });

  return payload.profile;
}
