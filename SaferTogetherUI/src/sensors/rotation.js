// Reads phone tilt (rotation) and shake (movement) while a game task runs, for the stats.
// No phone sensors (desktop) -> the take() calls return null and the chart shows "no data".

let listening = false;   // are the sensor listeners attached?
let armed = false;       // did we already set up the iPhone permission tap?
let gotTilt = false;     // got at least one real tilt reading?
let gotShake = false;    // got at least one real shake reading?
let tiltSum = 0;         // tilt change (degrees) since the last take
let shakeSum = 0;        // total shake since the last take
let shakeCount = 0;      // shake readings since the last take
let lastBeta = null;
let lastGamma = null;
let lastAccel = null;

// smallest turn between two angles (so 359 -> 1 counts as 2, not 358)
function angleDelta(now, prev) {
  const d = Math.abs(now - prev);
  return d > 180 ? 360 - d : d;
}

// add up how much the phone tilted
function onTilt(event) {
  const beta = typeof event.beta === "number" ? event.beta : null;
  const gamma = typeof event.gamma === "number" ? event.gamma : null;
  if (beta === null && gamma === null) return; // desktops sometimes send one empty event
  gotTilt = true;
  if (lastBeta !== null && beta !== null) tiltSum += angleDelta(beta, lastBeta);
  if (lastGamma !== null && gamma !== null) tiltSum += angleDelta(gamma, lastGamma);
  lastBeta = beta;
  lastGamma = gamma;
}

// add up how much the phone moved/shook between readings
function onShake(event) {
  const a = event.accelerationIncludingGravity || event.acceleration;
  if (!a || (a.x == null && a.y == null && a.z == null)) return;
  gotShake = true;
  if (lastAccel) {
    const dx = (a.x || 0) - lastAccel.x;
    const dy = (a.y || 0) - lastAccel.y;
    const dz = (a.z || 0) - lastAccel.z;
    shakeSum += Math.sqrt(dx * dx + dy * dy + dz * dz);
    shakeCount += 1;
  }
  lastAccel = { x: a.x || 0, y: a.y || 0, z: a.z || 0 };
}

// attach both sensor listeners (once)
function startListening() {
  if (listening) return;
  if ("DeviceOrientationEvent" in window) window.addEventListener("deviceorientation", onTilt, { passive: true });
  if ("DeviceMotionEvent" in window) window.addEventListener("devicemotion", onShake, { passive: true });
  listening = true;
}

// true only on browsers that gate the sensors behind a tap permission (iPhone)
function needsPermissionTap() {
  return typeof window.DeviceOrientationEvent?.requestPermission === "function"
    || typeof window.DeviceMotionEvent?.requestPermission === "function";
}

// ask for the sensors right now (must run inside a real tap on iPhone).
// fire BOTH requests before the first await: iOS drops the tap gesture after an await,
// so requesting the second permission later would be denied.
async function requestNow() {
  const orient = typeof window.DeviceOrientationEvent?.requestPermission === "function"
    ? window.DeviceOrientationEvent.requestPermission()
    : Promise.resolve("granted");
  const motion = typeof window.DeviceMotionEvent?.requestPermission === "function"
    ? window.DeviceMotionEvent.requestPermission()
    : Promise.resolve("granted");

  let okTilt = false;
  let okShake = false;
  try { okTilt = (await orient) === "granted"; } catch { okTilt = false; }
  try { okShake = (await motion) === "granted"; } catch { okShake = false; }

  if (okTilt || okShake) startListening();
}

// call when a game/mission starts: sense now on Android, or ask on first tap on iPhone
export function primeMotionSensors() {
  if (typeof window === "undefined") return;
  if (!needsPermissionTap()) {
    startListening(); // Android/desktop: no prompt needed
    return;
  }
  if (armed) return;  // iPhone: only wire the permission tap once
  armed = true;
  window.addEventListener("pointerdown", () => { void requestNow(); }, { capture: true, once: true });
}

// rotation done this task (degrees), or null when there's no phone sensor
export function takeRotationForItem() {
  if (!gotTilt) return null;
  const value = Math.round(tiltSum * 10) / 10;
  tiltSum = 0;
  return value;
}

// average shake this task (m/s^2), or null when there's no phone sensor
export function takeMovementForItem() {
  if (!gotShake || shakeCount === 0) return null;
  const value = Math.round((shakeSum / shakeCount) * 100) / 100;
  shakeSum = 0;
  shakeCount = 0;
  return value;
}

// stop and reset everything (between alarms / teardown)
export function stopMotionTracking() {
  window.removeEventListener("deviceorientation", onTilt);
  window.removeEventListener("devicemotion", onShake);
  listening = false;
  gotTilt = false;
  gotShake = false;
  tiltSum = 0;
  shakeSum = 0;
  shakeCount = 0;
  lastBeta = null;
  lastGamma = null;
  lastAccel = null;
}
