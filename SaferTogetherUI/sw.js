// SaferTogether service worker — delivers real/training alarm notifications
// pushed from the gateway, so members are alerted even when the app is closed
// or the phone is locked. (Web Push: the OS plays the notification sound; the
// in-app alarm mp3 plays once the member opens the app via the notification.)

self.addEventListener("install", () => {
  self.skipWaiting();
});

self.addEventListener("activate", event => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener("push", event => {
  let data = {};
  try {
    data = event.data ? event.data.json() : {};
  } catch {
    data = {};
  }

  const title = data.title || "SaferTogether";
  const options = {
    body: data.body || "",
    dir: "rtl",
    lang: "he",
    tag: data.tag || "safer-alarm",
    renotify: true,
    requireInteraction: true,
    vibrate: [300, 120, 300, 120, 300],
    data: {
      mode: data.mode || "real",
      url: data.url || "/emergency.html"
    }
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener("notificationclick", event => {
  event.notification.close();
  const targetUrl = (event.notification.data && event.notification.data.url) || "/emergency.html";

  event.waitUntil((async () => {
    const allClients = await self.clients.matchAll({ includeUncontrolled: true, type: "window" });

    for (const client of allClients) {
      if ("focus" in client) {
        if ("navigate" in client) {
          client.navigate(targetUrl).catch(() => {});
        }
        return client.focus();
      }
    }

    if (self.clients.openWindow) {
      return self.clients.openWindow(targetUrl);
    }
    return undefined;
  })());
});
