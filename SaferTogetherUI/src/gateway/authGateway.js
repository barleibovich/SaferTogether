import { requestJson } from "./apiClient.js";

export async function signUpWithUsername({ username, password, role }) {
  return requestJson("/api/auth/signup", {
    body: { password, role, username },
    method: "POST"
  });
}

export async function loginWithUsername({ username, password }) {
  return requestJson("/api/auth/login", {
    body: { password, username },
    method: "POST"
  });
}

export async function logout() {
  return requestJson("/api/auth/logout", {
    method: "POST"
  });
}

export async function getCurrentUserProfile() {
  const payload = await requestJson("/api/auth/profile");
  return payload.profile;
}
