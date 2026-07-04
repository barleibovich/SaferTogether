import { requestJson } from "./apiClient.js";

// the current user's groups
export async function getCurrentUserGroups() {
  const payload = await requestJson("/api/groups");
  return payload.groups;
}

// make a new group
export async function createGroupForCurrentUser({ name }) {
  const payload = await requestJson("/api/groups", {
    body: { name },
    method: "POST"
  });

  return payload.group;
}

// ask to join using a code
export async function requestJoinByCode({ code }) {
  const payload = await requestJson("/api/groups/join-requests", {
    body: { code },
    method: "POST"
  });

  return payload.joinRequest;
}

// admin approves/declines a request
export async function reviewJoinRequest({ groupId, requestId, status }) {
  const payload = await requestJson(`/api/groups/${groupId}/join-requests/${requestId}`, {
    body: { status },
    method: "PATCH"
  });

  return payload.joinRequest;
}

// rename a group
export async function renameGroup(groupId, name) {
  const payload = await requestJson(`/api/groups/${groupId}`, {
    body: { name },
    method: "PATCH"
  });
  return payload.group;
}

// delete a group
export async function deleteOwnedGroup(groupId) {
  return requestJson(`/api/groups/${groupId}`, {
    method: "DELETE"
  });
}

// leave a group
export async function leaveGroup(groupId) {
  return requestJson(`/api/groups/${groupId}/members/me`, {
    method: "DELETE"
  });
}

// start a drill for the group (admin only)
export async function startDrill(groupId) {
  return requestJson(`/api/groups/${groupId}/drill`, { method: "POST" });
}

// end the active drill (admin only)
export async function endDrill(groupId) {
  return requestJson(`/api/groups/${groupId}/drill`, { method: "DELETE" });
}

// mark me safe in the active drill
export async function markSafe(groupId) {
  const payload = await requestJson(`/api/groups/${groupId}/drill/safe`, { method: "POST" });
  return payload.safeUsers;
}

// who has marked safe in the active drill
export async function fetchDrillStatus(groupId) {
  const payload = await requestJson(`/api/groups/${groupId}/drill/status`);
  return payload.safeUsers;
}
