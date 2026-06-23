// Hand-rotation capture for the statistics line chart.
//
// On a phone the browser fires `deviceorientation` events as the user tilts the
// device. We integrate how much the hand rotated (the absolute change in tilt)
// during each question / mission task, so the stats page can graph it just like
// time. On desktop no events ever fire, so `takeRotationForItem()` returns null
// and the chart shows "no rotation data yet" — the plumbing stays dormant until
// the phone build ships.

let tracking = false;
let hasAnyData = false;       // did we ever receive a real sensor reading?
let accumulated = 0;          // integrated rotation since the last take(), in degrees
let lastBeta = null;
let lastGamma = null;
let handler = null;

// wrap an angle delta so a 360->0 flip doesn't register as a huge jump
function angleDelta(current, previous) {
  let delta = Math.abs(current - previous);
  if (delta > 180) {
    delta = 360 - delta;
  }
  return delta;
}

function onOrientation(event) {
  const beta = typeof event.beta === "number" ? event.beta : null;
  const gamma = typeof event.gamma === "number" ? event.gamma : null;

  // some desktops emit a single all-null event — ignore those
  if (beta === null && gamma === null) {
    return;
  }

  hasAnyData = true;

  if (lastBeta !== null && beta !== null) {
    accumulated += angleDelta(beta, lastBeta);
  }
  if (lastGamma !== null && gamma !== null) {
    accumulated += angleDelta(gamma, lastGamma);
  }

  lastBeta = beta;
  lastGamma = gamma;
}

// Begin listening. On iOS 13+ this must be called from a user gesture so the
// permission prompt can appear; we request it but never block gameplay on it.
export async function startRotationTracking() {
  if (tracking || typeof window === "undefined" || !("DeviceOrientationEvent" in window)) {
    return;
  }

  try {
    const requestPermission = window.DeviceOrientationEvent?.requestPermission;
    if (typeof requestPermission === "function") {
      const result = await requestPermission();
      if (result !== "granted") {
        return;
      }
    }
  } catch (error) {
    // permission denied / not available — leave tracking off, charts show no data
    return;
  }

  handler = onOrientation;
  window.addEventListener("deviceorientation", handler, { passive: true });
  tracking = true;
}

// Return the rotation accumulated for the item that just finished and reset the
// accumulator for the next item. Returns null when there is no sensor data
// (desktop), so callers store null and the running average skips it.
export function takeRotationForItem() {
  if (!tracking || !hasAnyData) {
    return null;
  }

  const value = Math.round(accumulated * 10) / 10;
  accumulated = 0;
  return value;
}

export function stopRotationTracking() {
  if (handler) {
    window.removeEventListener("deviceorientation", handler);
    handler = null;
  }
  tracking = false;
  hasAnyData = false;
  accumulated = 0;
  lastBeta = null;
  lastGamma = null;
}
