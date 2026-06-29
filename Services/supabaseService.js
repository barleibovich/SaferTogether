const path = require("path");
const { createRequire } = require("module");
const { getConfig } = require("./configService");
const { httpError } = require("./errors");

const requireFromGateway = createRequire(path.join(__dirname, "..", "Gateway", "package.json"));
let createClient;

try {
  ({ createClient } = require("@supabase/supabase-js"));
} catch {
  ({ createClient } = requireFromGateway("@supabase/supabase-js"));
}

// shared options for the supabase client
function createBaseOptions(global = {}) {
  return {
    auth: {
      autoRefreshToken: false,
      detectSessionInUrl: false,
      persistSession: false
    },
    global
  };
}

// anon supabase client
function createPublicClient() {
  const { supabaseAnonKey, supabaseUrl } = getConfig();
  return createClient(supabaseUrl, supabaseAnonKey, createBaseOptions());
}

// supabase client logged in as the current user
function createUserClient(accessToken) {
  const { supabaseAnonKey, supabaseUrl } = getConfig();
  return createClient(
    supabaseUrl,
    supabaseAnonKey,
    createBaseOptions({
      headers: {
        Authorization: `Bearer ${accessToken}`
      }
    })
  );
}

// get the auth user from an access token
async function getUserFromAccessToken(accessToken) {
  if (!accessToken) {
    throw httpError(401, "Unauthorized");
  }

  const client = createPublicClient();
  const { data, error } = await client.auth.getUser(accessToken);

  if (error) {
    throw error;
  }

  if (!data.user) {
    throw httpError(401, "Unauthorized");
  }

  return data.user;
}

// grab the user, their profile, and a user-scoped client
async function getSessionContext(accessToken) {
  const user = await getUserFromAccessToken(accessToken);
  const client = createUserClient(accessToken);
  const { data: profile, error: profileError } = await client
    .from("profiles")
    .select("*")
    .eq("id", user.id)
    .single();

  if (profileError) {
    throw profileError;
  }

  return {
    client,
    profile,
    user
  };
}

module.exports = {
  createPublicClient,
  createUserClient,
  getSessionContext,
  getUserFromAccessToken
};
