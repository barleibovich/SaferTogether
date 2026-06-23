import { requestJson } from "./apiClient.js";

// tell the server this client is online right now (presence heartbeat)
export async function sendPresenceHeartbeat() {
  return requestJson("/api/presence", { method: "POST" });
}
