import { requestJson } from "./apiClient.js";

// get all activities for a group
export async function getGroupActivities(groupId) {
  const payload = await requestJson(`/api/groups/${groupId}/activities`);
  return payload.activities;
}

// make a new activity in a group
export async function createGroupActivity(groupId, activity) {
  const payload = await requestJson(`/api/groups/${groupId}/activities`, {
    body: activity,
    method: "POST"
  });

  return payload.activity;
}

// turn an activity on for a mode
export async function activateGroupActivity(groupId, activityId, mode) {
  const payload = await requestJson(`/api/groups/${groupId}/activities/${activityId}/activate`, {
    body: { mode },
    method: "POST"
  });

  return payload.activation;
}

// turn an activity off for a mode
export async function deactivateGroupActivity(groupId, activityId, mode) {
  return requestJson(`/api/groups/${groupId}/activity-activations/${mode}/${activityId}`, {
    method: "DELETE"
  });
}

// delete a group activity by id
export async function deleteGroupActivity(groupId, activityId) {
  return requestJson(`/api/groups/${groupId}/activities/${activityId}`, {
    method: "DELETE"
  });
}

// get the currently active activities for a mode
export async function getActiveGroupActivities(groupId, mode) {
  const payload = await requestJson(`/api/groups/${groupId}/active-activities?mode=${encodeURIComponent(mode)}`);
  return payload.activities || [];
}

// grab all the results for a group
export async function getGroupActivityResults(groupId) {
  const payload = await requestJson(`/api/groups/${groupId}/activity-results`);
  return payload.results;
}

// send in a result for an activity
export async function submitGroupActivityResult(groupId, result) {
  const payload = await requestJson(`/api/groups/${groupId}/activity-results`, {
    body: result,
    method: "POST"
  });

  return payload.result;
}

// admin reviews a result (approve/reject + note)
export async function reviewGroupActivityResult(groupId, resultId, status, adminNote = "") {
  const payload = await requestJson(`/api/groups/${groupId}/activity-results/${resultId}`, {
    body: { adminNote, status },
    method: "PATCH"
  });

  return payload.result;
}
