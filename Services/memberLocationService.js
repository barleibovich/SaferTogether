const { httpError } = require("./errors");
const { findOrefLocationByCoordinates } = require("./orefAlertService");
const { getSessionContext } = require("./supabaseService");

const LOCATION_TABLE = "profile_locations";

// is this error because the location table isn't set up yet?
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

// missing table -> tell them how to fix it
function missingLocationTableError() {
  return httpError(
    500,
    "The profile_locations table is missing. Run supabase/profile_locations.sql in Supabase first."
  );
}

// db row -> the shape we send out
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

// get saved alert areas for a bunch of users
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

// get one user's saved alert area
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

// save the user's HFC alert area from their gps coords
async function saveCurrentUserAlertLocation(accessToken, { latitude, longitude }) {
  const context = await getSessionContext(accessToken);
  const location = await findOrefLocationByCoordinates({ latitude, longitude });

  if (!location) {
    throw httpError(400, "לא ניתן להתאים מיקום זה לאזור התרעה של פיקוד העורף");
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
