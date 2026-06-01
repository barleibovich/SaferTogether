const { httpError } = require("./errors");
const { findOrefLocationByCoordinates } = require("./orefAlertService");
const { getSessionContext } = require("./supabaseService");

const LOCATION_TABLE = "profile_locations";

// This function recognizes Supabase errors caused by the required location table not being installed.
function isMissingLocationTableError(error) {
  const code = String(error?.code || "");
  const message = String(error?.message || "").toLowerCase();

  return (
    ["42P01", "42703", "PGRST204", "PGRST205"].includes(code) ||
    (message.includes(LOCATION_TABLE) && (
      message.includes("does not exist") ||
      message.includes("schema cache") ||
      message.includes("column")
    ))
  );
}

// This function turns a missing Supabase table into an actionable setup error.
function missingLocationTableError() {
  return httpError(
    500,
    "The profile_locations table is missing. Run supabase/profile_locations.sql in Supabase first."
  );
}

// This function maps a stored profile location into the public API shape.
function mapProfileLocation(row) {
  if (!row) {
    return null;
  }

  return {
    areaId: String(row.area_id || ""),
    areaName: String(row.area_name || ""),
    areaNameHebrew: String(row.area_name_he || ""),
    districtName: String(row.district_name || ""),
    shelterTimeSeconds: row.shelter_time_seconds ?? null,
    updatedAt: row.updated_at || null
  };
}

// This function gets saved alert areas for a list of users.
async function getLocationsForUsers(client, userIds) {
  const ids = [...new Set((userIds || []).filter(Boolean))];

  if (!ids.length) {
    return new Map();
  }

  const { data, error } = await client
    .from(LOCATION_TABLE)
    .select("user_id, area_id, area_name, area_name_he, district_name, shelter_time_seconds, updated_at")
    .in("user_id", ids);

  if (error) {
    if (isMissingLocationTableError(error)) {
      throw missingLocationTableError();
    }

    throw error;
  }

  return new Map((data || []).map(row => [row.user_id, mapProfileLocation(row)]));
}

// This function gets the current user's saved alert area.
async function getLocationForUser(client, userId) {
  if (!userId) {
    return null;
  }

  const { data, error } = await client
    .from(LOCATION_TABLE)
    .select("user_id, area_id, area_name, area_name_he, district_name, shelter_time_seconds, updated_at")
    .eq("user_id", userId)
    .maybeSingle();

  if (error) {
    if (isMissingLocationTableError(error)) {
      throw missingLocationTableError();
    }

    throw error;
  }

  return mapProfileLocation(data);
}

// This function saves the current user's official HFC alert area from GPS coordinates.
async function saveCurrentUserAlertLocation(accessToken, { latitude, longitude }) {
  const context = await getSessionContext(accessToken);
  const location = await findOrefLocationByCoordinates({ latitude, longitude });

  if (!location) {
    throw httpError(400, "Could not match this location to a Home Front Command alert area");
  }

  const { data, error } = await context.client
    .from(LOCATION_TABLE)
    .upsert({
      area_id: location.areaId,
      area_name: location.areaName,
      area_name_he: location.areaNameHebrew,
      district_name: location.districtName,
      shelter_time_seconds: location.shelterTimeSeconds,
      updated_at: new Date().toISOString(),
      user_id: context.user.id
    }, {
      onConflict: "user_id"
    })
    .select("user_id, area_id, area_name, area_name_he, district_name, shelter_time_seconds, updated_at")
    .single();

  if (error) {
    if (isMissingLocationTableError(error)) {
      throw missingLocationTableError();
    }

    throw error;
  }

  return mapProfileLocation(data);
}

module.exports = {
  getLocationForUser,
  getLocationsForUsers,
  saveCurrentUserAlertLocation
};
