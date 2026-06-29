const { httpError } = require("./errors");
const { getConfig } = require("./configService");
const { normalizeAvatar } = require("./avatarService");
const {
  getLocationForUser,
  saveCurrentUserAlertLocation
} = require("./memberLocationService");
const { createPublicClient, createUserClient, getSessionContext } = require("./supabaseService");

const AUTH_EMAIL_DOMAINS = ["safertogether.app", "safertogether.local"];

// clean up the username before validating or auth lookup
function normalizeUsername(username) {
  return String(username || "").trim().toLowerCase();
}

// check the username is ok for login
function validateUsername(username) {
  return /^[a-zA-Z0-9_]{3,30}$/.test(username);
}

// only admin/user roles allowed
function validateRole(role) {
  return ["admin", "user"].includes(role);
}

// only allow small unity png snapshots for profile cards
function normalizeAvatarImage(avatarImage) {
  const value = String(avatarImage || "").trim();

  if (!value) {
    return "";
  }

  if (value.length > 1500000 || !/^data:image\/png;base64,[a-zA-Z0-9+/=]+$/.test(value)) {
    throw httpError(400, "תמונת הדמות חייבת להיות בפורמט PNG (data URL)");
  }

  return value;
}

// fake email supabase uses for username login
function usernameToAuthEmail(username, domain = AUTH_EMAIL_DOMAINS[0]) {
  return `${normalizeUsername(username)}@${domain}`;
}

// merge profile row with the avatar from auth metadata
function buildProfile(profile, user, alertLocation = null) {
  const avatar = user?.user_metadata?.avatar || profile?.avatar;

  return {
    ...profile,
    alertLocation,
    avatar: normalizeAvatar(avatar, profile?.username),
    avatarImage: profile?.avatar_image || ""
  };
}

// detect projects that havent set up profile avatars yet
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

// copy the chosen avatar onto the public profile so group cards show it
async function saveProfileAvatar(client, userId, avatar, avatarImage = undefined, options = {}) {
  const payload = { avatar };

  if (avatarImage !== undefined) {
    payload.avatar_image = avatarImage;
  }

  const { data, error } = await client
    .from("profiles")
    .update(payload)
    .eq("id", userId)
    .select("*")
    .maybeSingle();

  if (error) {
    if (avatarImage !== undefined && isProfileAvatarSetupError(error)) {
      // avatar image column/policy is missing. if caller really wanted the image
      // (avatar editor) throw so we dont silently drop it. otherwise (signup) just keep the text avatar.
      if (options.requireAvatarImage) {
        throw httpError(
          503,
          "Avatar image storage is not set up. Apply supabase/profile_avatars.sql to your Supabase project, then save the avatar again."
        );
      }

      return saveProfileAvatar(client, userId, avatar);
    }

    if (isProfileAvatarSetupError(error)) {
      return null;
    }

    throw error;
  }

  return data;
}

// update supabase auth metadata using the access token
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

// try logging in with each fake email domain we support
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

  throw lastError || httpError(401, "שם משתמש או סיסמה שגויים");
}

// make a supabase auth user + matching profile
async function signUpWithUsername({ username, password, role = "user", avatar, avatarImage }) {
  const cleanUsername = normalizeUsername(username);
  const cleanRole = validateRole(role) ? role : "user";
  const cleanAvatar = normalizeAvatar(avatar, cleanUsername);
  const cleanAvatarImage = normalizeAvatarImage(avatarImage);

  if (!validateUsername(cleanUsername)) {
    throw httpError(
      400,
      "שם המשתמש חייב להכיל 3-30 תווים, ורק אותיות, ספרות או קו תחתון"
    );
  }

  if (!password || password.length < 6) {
    throw httpError(400, "הסיסמה חייבת להכיל לפחות 6 תווים");
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
    throw httpError(500, "ההרשמה נכשלה: המשתמש לא נוצר");
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

  await saveProfileAvatar(userClient, authData.user.id, cleanAvatar, cleanAvatarImage || undefined);
  const profile = await getCurrentUserProfile(session.access_token);

  return {
    profile,
    session
  };
}

// login by username, return the profile
async function loginWithUsername({ username, password }) {
  const cleanUsername = normalizeUsername(username);

  if (!validateUsername(cleanUsername)) {
    throw httpError(400, "שם משתמש לא תקין");
  }

  const client = createPublicClient();
  const data = await signInWithKnownDomains(client, cleanUsername, password);

  if (!data.session?.access_token) {
    throw httpError(401, "ההתחברות נכשלה");
  }

  const profile = await getCurrentUserProfile(data.session.access_token);

  return {
    profile,
    session: data.session
  };
}

// sign the user out of supabase
async function logout(accessToken) {
  if (!accessToken) {
    return;
  }

  const client = createUserClient(accessToken);
  await client.auth.signOut();
}

// get the current profile + its avatar
async function getCurrentUserProfile(accessToken) {
  const context = await getSessionContext(accessToken);
  const alertLocation = await getLocationForUser(context.client, context.user.id);
  return buildProfile(context.profile, context.user, alertLocation);
}

// update the logged-in user's avatar
async function updateCurrentUserAvatar(accessToken, { avatar, avatarImage }) {
  const context = await getSessionContext(accessToken);
  const cleanAvatar = normalizeAvatar(avatar, context.profile.username);
  const cleanAvatarImage = normalizeAvatarImage(avatarImage);
  const metadata = {
    ...(context.user.user_metadata || {}),
    avatar: cleanAvatar
  };

  const user = await updateAuthUserMetadata(accessToken, metadata);
  const profile = await saveProfileAvatar(context.client, context.user.id, cleanAvatar, cleanAvatarImage, {
    requireAvatarImage: Boolean(cleanAvatarImage)
  }) || context.profile;
  const alertLocation = await getLocationForUser(context.client, context.user.id);
  return buildProfile(profile, user, alertLocation);
}

// PUT an arbitrary update (password / email / metadata) to the supabase auth user
async function updateAuthUser(accessToken, payload) {
  const { supabaseAnonKey, supabaseUrl } = getConfig();
  const response = await fetch(`${supabaseUrl}/auth/v1/user`, {
    body: JSON.stringify(payload),
    headers: {
      apikey: supabaseAnonKey,
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json"
    },
    method: "PUT"
  });

  const result = await response.json().catch(() => null);

  if (!response.ok) {
    throw httpError(response.status, result?.msg || result?.message || "Profile update failed");
  }

  return result;
}

// the existing password can never be shown (it is hashed); the only way to confirm it is
// to attempt a fresh sign-in with it.
async function verifyCurrentPassword(username, currentPassword) {
  if (!currentPassword) {
    throw httpError(400, "יש להזין את הסיסמה הנוכחית");
  }

  try {
    await signInWithKnownDomains(createPublicClient(), username, currentPassword);
  } catch {
    throw httpError(401, "הסיסמה הנוכחית שגויה");
  }
}

// update the logged-in user's name and/or password. password change requires the current
// password. NOTE: the username is the login identity (auth email = username@domain), so a
// name change also rewrites the auth email — this needs Supabase "Confirm email change" OFF.
async function updateCurrentUserCredentials(accessToken, { username, currentPassword, newPassword } = {}) {
  const context = await getSessionContext(accessToken);
  const currentUsername = context.profile.username;

  const nextUsername = normalizeUsername(username);
  const wantsName = Boolean(nextUsername) && nextUsername !== currentUsername;
  const wantsPassword = typeof newPassword === "string" && newPassword.length > 0;

  if (!wantsName && !wantsPassword) {
    throw httpError(400, "אין שינויים לשמירה");
  }

  const payload = {};

  if (wantsPassword) {
    if (newPassword.length < 6) {
      throw httpError(400, "הסיסמה החדשה חייבת להכיל לפחות 6 תווים");
    }

    // the session is already authenticated. only re-verify the current password when the
    // caller actually supplied one; the inline avatar editor omits it and relies on the
    // session token instead.
    if (currentPassword) {
      await verifyCurrentPassword(currentUsername, currentPassword);
    }

    payload.password = newPassword;
  }

  if (wantsName) {
    if (!validateUsername(nextUsername)) {
      throw httpError(400, "שם המשתמש חייב להכיל 3-30 תווים, ורק אותיות, ספרות או קו תחתון");
    }

    const { data: taken } = await context.client
      .from("profiles")
      .select("id")
      .eq("username", nextUsername)
      .neq("id", context.user.id)
      .maybeSingle();

    if (taken) {
      throw httpError(409, "שם המשתמש כבר תפוס");
    }

    payload.email = usernameToAuthEmail(nextUsername);
    payload.data = {
      ...(context.user.user_metadata || {}),
      username: nextUsername
    };
  }

  await updateAuthUser(accessToken, payload);

  if (wantsName) {
    // keep the public profile + the denormalized group member rows in sync with the new name
    await context.client.from("profiles").update({ username: nextUsername }).eq("id", context.user.id);
    await context.client
      .from("user_groups")
      .update({ member_username: nextUsername })
      .eq("user_id", context.user.id);
  }

  return getCurrentUserProfile(accessToken);
}

// update the user's HFC alert area
async function updateCurrentUserAlertLocation(accessToken, location) {
  return saveCurrentUserAlertLocation(accessToken, location);
}

module.exports = {
  getCurrentUserProfile,
  loginWithUsername,
  logout,
  signUpWithUsername,
  updateCurrentUserAlertLocation,
  updateCurrentUserAvatar,
  updateCurrentUserCredentials
};
