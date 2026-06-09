import { requestJson } from "./apiClient.js";

// save the user's gps alert area
export async function saveCurrentUserAlertLocation(coords) {
  const payload = await requestJson("/api/auth/location", {
    body: coords,
    method: "PATCH"
  });

  return payload.alertLocation;
}

// live hfc alert status for a group
export async function getGroupOrefStatus(groupId) {
  return requestJson(`/api/oref/groups/${encodeURIComponent(groupId)}/status`);
}
