import { requestJson } from "./apiClient.js";

// This function gets the groups of the current user.
export async function getCurrentUserGroups() {
  const payload = await requestJson("/api/groups");
  return payload.groups;
}

// This function creates a new group.
export async function createGroupForCurrentUser({ name }) {
  const payload = await requestJson("/api/groups", {
    body: { name },
    method: "POST"
  });

  return payload.group;
}

// This function sends a join request by code.
export async function requestJoinByCode({ code }) {
  const payload = await requestJson("/api/groups/join-requests", {
    body: { code },
    method: "POST"
  });

  return payload.joinRequest;
}

// This function lets the admin approve or decline a request.
export async function reviewJoinRequest({ groupId, requestId, status }) {
  const payload = await requestJson(`/api/groups/${groupId}/join-requests/${requestId}`, {
    body: { status },
    method: "PATCH"
  });

  return payload.joinRequest;
}

// This function renames a group.
export async function renameGroup(groupId, name) {
  const payload = await requestJson(`/api/groups/${groupId}`, {
    body: { name },
    method: "PATCH"
  });
  return payload.group;
}

// This function deletes a group.
export async function deleteOwnedGroup(groupId) {
  return requestJson(`/api/groups/${groupId}`, {
    method: "DELETE"
  });
}
