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

// This function normalizes HFC area names for matching across English and Hebrew endpoint data.
function normalizeAreaName(value) {
  return String(value || "")
    .normalize("NFKC")
    .replace(/[\u0591-\u05C7]/g, "")
    .replace(/['"\u05F3\u05F4`]/g, "")
    .replace(/\s+/g, " ")
    .trim()
    .toLowerCase();
}

// This function returns the headers expected by the Home Front Command website endpoint.
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

// This function parses HFC responses, including the empty BOM payload returned when there are no alerts.
function parseJsonPayload(text, emptyValue) {
  const cleanText = String(text || "").replace(/^\uFEFF/, "").trim();
  return cleanText ? JSON.parse(cleanText) : emptyValue;
}

// This function fetches JSON from an HFC endpoint with a short timeout.
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

// This function fetches a binary resource with a short timeout.
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

// This function reads the local-file header for the single JSON file inside the HFC polygon zip.
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

// This function turns the alert data payload into a clean list of affected areas.
function normalizeAlertAreas(data) {
  if (Array.isArray(data)) {
    return data.map(area => String(area || "").trim()).filter(Boolean);
  }

  const area = String(data || "").trim();
  return area ? [area] : [];
}

// This function maps a raw HFC alert into the public shape used by the app.
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

// This function gets active real alerts from the Home Front Command website endpoint.
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
    .finally(() => {
      pendingAlertsRequest = null;
    });

  return pendingAlertsRequest;
}

// This function maps the HFC location list into the public shape used by the app.
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

// This function loads and caches official HFC alert areas.
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

// This function loads and caches alert-area polygons for GPS-to-area matching.
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

// This function finds the official HFC location record for a polygon area name.
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

// This function finds the official alert area containing a GPS coordinate.
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
  // The polygon map is keyed by HFC area name; after the geometric match we load the full area record.
  const areaName = Object.keys(polygons).find(name => pointInPolygon([lat, lon], polygons[name]));

  if (!areaName) {
    return null;
  }

  return findOrefLocationByAreaName(areaName);
}

// This function uses ray casting to decide whether one GPS coordinate is inside an HFC polygon.
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

// This function treats GPS points on an area border as inside the area.
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

// This function checks whether an active alert affects one saved member location.
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

// This function annotates each group member with its live HFC alert state.
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
