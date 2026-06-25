import { requestJson } from "./apiClient.js";

// VAPID public key + whether the server has push configured
export async function getPushConfig() {
  return requestJson("/api/push/config");
}

// register (or refresh) this device's push subscription for the current user
export async function savePushSubscription(subscription, userAgent) {
  return requestJson("/api/push/subscriptions", {
    body: { subscription, userAgent },
    method: "POST"
  });
}

// remove this device's subscription (e.g. on logout)
export async function deletePushSubscription(endpoint) {
  return requestJson("/api/push/subscriptions", {
    body: { endpoint },
    method: "DELETE"
  });
}
