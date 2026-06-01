const { httpError } = require("./errors");
const { getConfig } = require("./configService");
const { normalizeAvatar } = require("./avatarService");
const {
  getLocationForUser,
  saveCurrentUserAlertLocation
} = require("./memberLocationService");
const { createPublicClient, createUserClient, getSessionContext } = require("./supabaseService");

const AUTH_EMAIL_DOMAINS = ["safertogether.app", "safertogether.local"];

// This function normalizes a username before validation or auth lookup.
function normalizeUsername(username) {
  return String(username || "").trim().toLowerCase();
}

// This function checks that a username can be safely used for login.
function validateUsername(username) {
  return /^[a-zA-Z0-9_]{3,30}$/.test(username);
}

// This function checks that the requested profile role is supported.
function validateRole(role) {
  return ["admin", "user"].includes(role);
}

// This function builds the synthetic email Supabase auth uses for username login.
function usernameToAuthEmail(username, domain = AUTH_EMAIL_DOMAINS[0]) {
  return `${normalizeUsername(username)}@${domain}`;
}

// This function combines the profile row with the avatar stored in auth metadata.
function buildProfile(profile, user, alertLocation = null) {
  const avatar = user?.user_metadata?.avatar || profile?.avatar;

  return {
    ...profile,
    alertLocation,
    avatar: normalizeAvatar(avatar, profile?.username)
  };
}

// This function recognizes Supabase projects that have not added profile avatar setup yet.
function isProfileAvatarSetupError(error) {
  const code = String(error?.code || "");
  const message = String(error?.message || "").toLowerCase();

  return (
    ["42501", "42703", "PGRST204", "PGRST205"].includes(code) ||
    (message.includes("avatar") && (
      message.includes("column") ||
      message.includes("schema cache")
    )) ||
    (message.includes("permission denied") && message.includes("profiles"))
  );
}

// This function mirrors the selected avatar onto public profiles for group member cards.
async function saveProfileAvatar(client, userId, avatar) {
  const { data, error } = await client
    .from("profiles")
    .update({ avatar })
    .eq("id", userId)
    .select("*")
    .maybeSingle();

  if (error) {
    if (isProfileAvatarSetupError(error)) {
      return null;
    }

    throw error;
  }

  return data;
}

// This function updates Supabase auth metadata with the current access token.
async function updateAuthUserMetadata(accessToken, metadata) {
  const { supabaseAnonKey, supabaseUrl } = getConfig();
  const response = await fetch(`${supabaseUrl}/auth/v1/user`, {
    body: JSON.stringify({ data: metadata }),
    headers: {
      apikey: supabaseAnonKey,
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json"
    },
    method: "PUT"
  });

  const payload = await response.json().catch(() => null);

  if (!response.ok) {
    throw httpError(response.status, payload?.msg || payload?.message || "Avatar update failed");
  }

  return payload;
}

// This function signs in by trying each supported synthetic email domain.
async function signInWithKnownDomains(client, username, password) {
  let lastError = null;

  for (const domain of AUTH_EMAIL_DOMAINS) {
    const authEmail = usernameToAuthEmail(username, domain);
    const { data, error } = await client.auth.signInWithPassword({
      email: authEmail,
      password
    });

    if (!error) {
      return data;
    }

    lastError = error;
  }

  throw lastError || httpError(401, "Invalid login credentials");
}

// This function creates a Supabase auth user and a matching app profile.
async function signUpWithUsername({ username, password, role = "user", avatar }) {
  const cleanUsername = normalizeUsername(username);
  const cleanRole = validateRole(role) ? role : "user";
  const cleanAvatar = normalizeAvatar(avatar, cleanUsername);

  if (!validateUsername(cleanUsername)) {
    throw httpError(
      400,
      "Username must be 3-30 characters and contain only letters, numbers, or underscore"
    );
  }

  if (!password || password.length < 6) {
    throw httpError(400, "Password must be at least 6 characters");
  }

  const client = createPublicClient();
  const authEmail = usernameToAuthEmail(cleanUsername);
  const { data: authData, error: authError } = await client.auth.signUp({
    email: authEmail,
    password,
    options: {
      data: {
        avatar: cleanAvatar,
        role: cleanRole,
        username: cleanUsername
      }
    }
  });

  if (authError) {
    throw authError;
  }

  if (!authData.user) {
    throw httpError(500, "Signup failed: user was not created");
  }

  const sessionData = authData.session
    ? authData
    : await signInWithKnownDomains(client, cleanUsername, password);
  const session = sessionData.session;

  if (!session?.access_token) {
    throw httpError(
      500,
      "Signup succeeded, but no session was created. Turn off email confirmation in Supabase."
    );
  }

  const userClient = createUserClient(session.access_token);
  const { error: profileError } = await userClient.from("profiles").insert({
    id: authData.user.id,
    role: cleanRole,
    username: cleanUsername
  });

  if (profileError) {
    throw profileError;
  }

  await saveProfileAvatar(userClient, authData.user.id, cleanAvatar);
  const profile = await getCurrentUserProfile(session.access_token);

  return {
    profile,
    session
  };
}

// This function logs in with a username and returns the current app profile.
async function loginWithUsername({ username, password }) {
  const cleanUsername = normalizeUsername(username);

  if (!validateUsername(cleanUsername)) {
    throw httpError(400, "Invalid username");
  }

  const client = createPublicClient();
  const data = await signInWithKnownDomains(client, cleanUsername, password);

  if (!data.session?.access_token) {
    throw httpError(401, "Login failed");
  }

  const profile = await getCurrentUserProfile(data.session.access_token);

  return {
    profile,
    session: data.session
  };
}

// This function signs the current user out of Supabase auth.
async function logout(accessToken) {
  if (!accessToken) {
    return;
  }

  const client = createUserClient(accessToken);
  await client.auth.signOut();
}

// This function returns the current profile with its selected avatar.
async function getCurrentUserProfile(accessToken) {
  const context = await getSessionContext(accessToken);
  const alertLocation = await getLocationForUser(context.client, context.user.id);
  return buildProfile(context.profile, context.user, alertLocation);
}

// This function updates the logged-in user's avatar choice.
async function updateCurrentUserAvatar(accessToken, { avatar }) {
  const context = await getSessionContext(accessToken);
  const cleanAvatar = normalizeAvatar(avatar, context.profile.username);
  const metadata = {
    ...(context.user.user_metadata || {}),
    avatar: cleanAvatar
  };

  const user = await updateAuthUserMetadata(accessToken, metadata);
  const profile = await saveProfileAvatar(context.client, context.user.id, cleanAvatar) || context.profile;
  const alertLocation = await getLocationForUser(context.client, context.user.id);
  return buildProfile(profile, user, alertLocation);
}

// This function updates the logged-in user's Home Front Command alert area.
async function updateCurrentUserAlertLocation(accessToken, location) {
  return saveCurrentUserAlertLocation(accessToken, location);
}

module.exports = {
  getCurrentUserProfile,
  loginWithUsername,
  logout,
  signUpWithUsername,
  updateCurrentUserAlertLocation,
  updateCurrentUserAvatar
};
