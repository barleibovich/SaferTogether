import { requestJson } from "./apiClient.js";

// admin raises a group alarm ("real" or "training"). Returns the alarm record.
export async function startAlarm(groupId, mode) {
  const payload = await requestJson(`/api/groups/${groupId}/alarm`, {
    body: { mode },
    method: "POST"
  });

  return payload.alarm;
}

// any member fires a real alarm on a live HFC alert (server checks it's real first)
export async function raiseOrefAlarm(groupId) {
  const payload = await requestJson(`/api/groups/${groupId}/alarm/oref`, {
    method: "POST"
  });

  return payload.alarm;
}

// current alarm state for a group: { alarm, safeUserIds, unlocked }
export async function getActiveAlarm(groupId) {
  return requestJson(`/api/groups/${groupId}/alarm`);
}

// the current user marks themselves safe. Returns { safeUserIds, unlocked }.
export async function markAlarmSafe(groupId) {
  return requestJson(`/api/groups/${groupId}/alarm/safe`, { method: "POST" });
}

// admin opens the activities for everyone (override). Returns { safeUserIds, unlocked }.
export async function unlockAlarm(groupId) {
  return requestJson(`/api/groups/${groupId}/alarm/unlock`, { method: "POST" });
}

// admin ends the active alarm.
export async function endAlarm(groupId) {
  return requestJson(`/api/groups/${groupId}/alarm`, { method: "DELETE" });
}

// a member reports their play progress (e.g. trivia 2/3) for the admin to watch.
export async function reportAlarmProgress(groupId, progress) {
  return requestJson(`/api/groups/${groupId}/alarm/progress`, {
    body: progress,
    method: "POST"
  });
}
