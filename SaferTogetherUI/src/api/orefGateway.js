import { requestJson } from "./apiClient.js";

// This function saves the current user's GPS-resolved alert area.
export async function saveCurrentUserAlertLocation(coords) {
  const payload = await requestJson("/api/auth/location", {
    body: coords,
    method: "PATCH"
  });

  return payload.alertLocation;
}

// This function gets the live HFC alert status for one group.
export async function getGroupOrefStatus(groupId) {
  return requestJson(`/api/oref/groups/${encodeURIComponent(groupId)}/status`);
}
