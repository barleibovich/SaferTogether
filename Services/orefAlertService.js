const zlib = require("zlib");

const DEFAULT_ALERTS_URL = "https://www.oref.org.il/warningMessages/alert/Alerts.json";
const DEFAULT_LOCATIONS_URL = "https://alerts-history.oref.org.il/Shared/Ajax/GetDistricts.aspx?lang=en";
const DEFAULT_POLYGONS_URL = "https://raw.githubusercontent.com/amitfin/oref_alert/main/custom_components/oref_alert/metadata/area_to_polygon.json.zip";

const ALERT_CACHE_MS = 2500;
const LOCATION_CACHE_MS = 6 * 60 * 60 * 1000;
const POLYGON_CACHE_MS = 24 * 60 * 60 * 1000;

let alertCache = {
  expiresAt: 0,
  value: []
};

let locationCache = {
  expiresAt: 0,
  value: []
};

let polygonCache = {
  expiresAt: 0,
  value: null
};

let pendingAlertsRequest = null;
let pendingLocationsRequest = null;
let pendingPolygonsRequest = null;

// clean up HFC area names so english/hebrew ones match
function normalizeAreaName(value) {
  return String(value || "")
    .normalize("NFKC")
    .replace(/[\u0591-\u05C7]/g, "")
    .replace(/['"\u05F3\u05F4`]/g, "")
    .replace(/\s+/g, " ")
    .trim()
    .toLowerCase();
}

// headers the oref website endpoint expects
function orefHeaders() {
  return {
    Accept: "application/json, text/plain, */*",
    "Cache-Control": "no-cache",
    Pragma: "no-cache",
    Referer: "https://www.oref.org.il/",
    "User-Agent": "Mozilla/5.0 SaferTogether/1.0",
    "X-Requested-With": "XMLHttpRequest"
  };
}

// parse HFC responses, handle the empty BOM payload when theres no alerts
function parseJsonPayload(text, emptyValue) {
  const cleanText = String(text || "").replace(/^\uFEFF/, "").trim();
  return cleanText ? JSON.parse(cleanText) : emptyValue;
}

// fetch json from an HFC endpoint, short timeout
async function fetchOrefJson(url, emptyValue) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 7000);

  try {
    const response = await fetch(url, {
      headers: orefHeaders(),
      signal: controller.signal
    });
    const text = await response.text();

    if (!response.ok) {
      throw new Error(`Home Front Command request failed with ${response.status}`);
    }

    return parseJsonPayload(text, emptyValue);
  } finally {
    clearTimeout(timeoutId);
  }
}

// fetch a binary resource, short timeout
async function fetchBuffer(url) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 10000);

  try {
    const response = await fetch(url, {
      headers: orefHeaders(),
      signal: controller.signal
    });

    if (!response.ok) {
      throw new Error(`Home Front Command location map request failed with ${response.status}`);
    }

    return Buffer.from(await response.arrayBuffer());
  } finally {
    clearTimeout(timeoutId);
  }
}

// pull the one json file out of the polygon zip
function extractFirstJsonFromZip(zipBuffer) {
  const signature = zipBuffer.readUInt32LE(0);

  if (signature !== 0x04034b50) {
    throw new Error("Invalid Home Front Command area map archive");
  }

  const compressionMethod = zipBuffer.readUInt16LE(8);
  const compressedSize = zipBuffer.readUInt32LE(18);
  const fileNameLength = zipBuffer.readUInt16LE(26);
  const extraLength = zipBuffer.readUInt16LE(28);
  const dataStart = 30 + fileNameLength + extraLength;
  const compressedData = zipBuffer.subarray(dataStart, dataStart + compressedSize);

  if (compressionMethod === 0) {
    return JSON.parse(compressedData.toString("utf8"));
  }

  if (compressionMethod === 8) {
    return JSON.parse(zlib.inflateRawSync(compressedData).toString("utf8"));
  }

  throw new Error("Unsupported Home Front Command area map compression");
}

// turn the alert payload into a clean list of areas
function normalizeAlertAreas(data) {
  if (Array.isArray(data)) {
    return data.map(area => String(area || "").trim()).filter(Boolean);
  }

  const area = String(data || "").trim();
  return area ? [area] : [];
}

// map a raw HFC alert into the shape the app uses
function normalizeAlert(alert) {
  if (!alert || typeof alert !== "object") {
    return null;
  }

  const areas = normalizeAlertAreas(alert.data);

  if (!areas.length) {
    return null;
  }

  return {
    areas,
    category: String(alert.cat || ""),
    description: String(alert.desc || ""),
    id: String(alert.id || `${alert.cat || "alert"}:${areas.join("|")}`),
    title: String(alert.title || "Home Front Command alert")
  };
}

// get the live alerts from the oref website
async function getCurrentOrefAlerts({ force = false } = {}) {
  const now = Date.now();

  if (!force && alertCache.expiresAt > now) {
    return alertCache.value;
  }

  if (!force && pendingAlertsRequest) {
    return pendingAlertsRequest;
  }

  pendingAlertsRequest = fetchOrefJson(
    process.env.OREF_ALERTS_URL || DEFAULT_ALERTS_URL,
    null
  )
    .then(payload => {
      const alerts = Array.isArray(payload) ? payload : (payload ? [payload] : []);
      const value = alerts.map(normalizeAlert).filter(Boolean);
      alertCache = {
        expiresAt: Date.now() + ALERT_CACHE_MS,
        value
      };
      return value;
    })
    .catch(error => {
      // oref is often blocked from cloud IPs; serve the last known alerts instead of crashing
      console.error("HFC alerts fetch failed, serving last known alerts:", error?.message || error);
      return alertCache.value || [];
    })
    .finally(() => {
      pendingAlertsRequest = null;
    });

  return pendingAlertsRequest;
}

// map the HFC location list into the shape the app uses
function normalizeLocation(location) {
  const areaId = String(location?.id || "").trim();
  const areaName = String(location?.label || "").trim();
  const areaNameHebrew = String(location?.label_he || "").trim();
  const districtName = String(location?.areaname || "").trim();
  const shelterTimeSeconds = Number(location?.migun_time);

  if (!areaId || !areaName) {
    return null;
  }

  return {
    areaId,
    areaName,
    areaNameHebrew,
    districtName,
    shelterTimeSeconds: Number.isFinite(shelterTimeSeconds) ? shelterTimeSeconds : null
  };
}

// load + cache the official HFC alert areas
async function getAllOrefLocations({ force = false } = {}) {
  const now = Date.now();

  if (!force && locationCache.expiresAt > now) {
    return locationCache.value;
  }

  if (!force && pendingLocationsRequest) {
    return pendingLocationsRequest;
  }

  pendingLocationsRequest = fetchOrefJson(
    process.env.OREF_LOCATIONS_URL || DEFAULT_LOCATIONS_URL,
    { value: [] }
  )
    .then(payload => {
      const locations = Array.isArray(payload) ? payload : payload?.value || [];
      const value = locations.map(normalizeLocation).filter(Boolean);
      locationCache = {
        expiresAt: Date.now() + LOCATION_CACHE_MS,
        value
      };
      return value;
    })
    .finally(() => {
      pendingLocationsRequest = null;
    });

  return pendingLocationsRequest;
}

// load + cache the area polygons, used for gps -> area matching
async function getOrefAreaPolygons({ force = false } = {}) {
  const now = Date.now();

  if (!force && polygonCache.expiresAt > now && polygonCache.value) {
    return polygonCache.value;
  }

  if (!force && pendingPolygonsRequest) {
    return pendingPolygonsRequest;
  }

  pendingPolygonsRequest = fetchBuffer(process.env.OREF_POLYGONS_URL || DEFAULT_POLYGONS_URL)
    .then(buffer => {
      const value = extractFirstJsonFromZip(buffer);
      polygonCache = {
        expiresAt: Date.now() + POLYGON_CACHE_MS,
        value
      };
      return value;
    })
    .finally(() => {
      pendingPolygonsRequest = null;
    });

  return pendingPolygonsRequest;
}

// find the HFC location record for a polygon area name
async function findOrefLocationByAreaName(areaName) {
  const cleanAreaName = normalizeAreaName(areaName);

  if (!cleanAreaName) {
    return null;
  }

  const locations = await getAllOrefLocations();
  return locations.find(location => [
    location.areaName,
    location.areaNameHebrew
  ].map(normalizeAreaName).includes(cleanAreaName)) || null;
}

// find which alert area a gps coordinate falls in
async function findOrefLocationByCoordinates({ latitude, longitude }) {
  const lat = Number(latitude);
  const lon = Number(longitude);

  if (!Number.isFinite(lat) || !Number.isFinite(lon)) {
    return null;
  }

  if (lat < 29 || lat > 34 || lon < 33.5 || lon > 36.5) {
    return null;
  }

  const polygons = await getOrefAreaPolygons();
  // polygon map is keyed by the (hebrew) area name, then we grab the full area record
  const areaName = Object.keys(polygons).find(name => pointInPolygon([lat, lon], polygons[name]));

  if (!areaName) {
    return null;
  }

  // add the official district info; if that lookup fails, just use the polygon area name
  try {
    const located = await findOrefLocationByAreaName(areaName);
    if (located) {
      return located;
    }
  } catch (error) {
    console.error("HFC districts lookup failed, falling back to polygon area name:", error?.message || error);
  }

  return {
    areaId: "",
    areaName,
    areaNameHebrew: areaName,
    districtName: "",
    shelterTimeSeconds: null
  };
}

// ray casting to check if a point is inside a polygon
function pointInPolygon(point, polygon) {
  const [x, y] = point;
  let inside = false;

  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i, i += 1) {
    const [xi, yi] = polygon[i];
    const [xj, yj] = polygon[j];

    if (pointOnSegment(x, y, xi, yi, xj, yj)) {
      return true;
    }

    const intersects = ((yi > y) !== (yj > y)) &&
      (x < ((xj - xi) * (y - yi)) / (yj - yi) + xi);

    if (intersects) {
      inside = !inside;
    }
  }

  return inside;
}

// points on the border count as inside
function pointOnSegment(px, py, ax, ay, bx, by) {
  const squaredLength = ((bx - ax) ** 2) + ((by - ay) ** 2);

  if (squaredLength === 0) {
    return Math.abs(px - ax) <= 0.000001 && Math.abs(py - ay) <= 0.000001;
  }

  const cross = (px - ax) * (by - ay) - (py - ay) * (bx - ax);

  if (Math.abs(cross) > 0.000001) {
    return false;
  }

  const dot = (px - ax) * (bx - ax) + (py - ay) * (by - ay);

  if (dot < 0) {
    return false;
  }

  return dot <= squaredLength;
}

// check if any active alert hits a saved member location
function getMatchingAlertsForLocation(alerts, location) {
  if (!location) {
    return [];
  }

  const locationKeys = [
    location.areaId,
    location.areaName,
    location.areaNameHebrew
  ].map(normalizeAreaName).filter(Boolean);

  if (!locationKeys.length) {
    return [];
  }

  return alerts.filter(alert => {
    const alertAreas = alert.areas.map(normalizeAreaName);
    return locationKeys.some(key => alertAreas.includes(key));
  });
}

// tag each group member with their current alert state
function annotateMembersWithOrefAlerts(members, alerts) {
  return (members || []).map(member => {
    const matchingAlerts = getMatchingAlertsForLocation(alerts, member.alertLocation);

    return {
      alertLocation: member.alertLocation || null,
      alerts: matchingAlerts,
      memberId: member.id,
      status: matchingAlerts.length ? "alert" : (member.alertLocation ? "clear" : "unknown"),
      username: member.username
    };
  });
}

module.exports = {
  annotateMembersWithOrefAlerts,
  findOrefLocationByCoordinates,
  getAllOrefLocations,
  getCurrentOrefAlerts,
  getMatchingAlertsForLocation,
  normalizeAreaName
};
