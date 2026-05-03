const { httpError } = require("./errors");
const { createPublicClient, createUserClient, getSessionContext } = require("./supabaseService");

const AUTH_EMAIL_DOMAINS = ["safertogether.app", "safertogether.local"];

function normalizeUsername(username) {
  return String(username || "").trim().toLowerCase();
}

function validateUsername(username) {
  return /^[a-zA-Z0-9_]{3,30}$/.test(username);
}

function validateRole(role) {
  return ["admin", "user"].includes(role);
}

function usernameToAuthEmail(username, domain = AUTH_EMAIL_DOMAINS[0]) {
  return `${normalizeUsername(username)}@${domain}`;
}

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

async function signUpWithUsername({ username, password, role = "user" }) {
  const cleanUsername = normalizeUsername(username);
  const cleanRole = validateRole(role) ? role : "user";

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

  const profile = await getCurrentUserProfile(session.access_token);

  return {
    profile,
    session
  };
}

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

async function logout(accessToken) {
  if (!accessToken) {
    return;
  }

  const client = createUserClient(accessToken);
  await client.auth.signOut();
}

async function getCurrentUserProfile(accessToken) {
  const { profile } = await getSessionContext(accessToken);
  return profile;
}

module.exports = {
  getCurrentUserProfile,
  loginWithUsername,
  logout,
  signUpWithUsername
};
