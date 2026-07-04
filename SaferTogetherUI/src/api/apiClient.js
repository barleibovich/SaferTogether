// tokens live in localStorage so login survives closing the app; the access token is
// short-lived and gets swapped for a new one via the refresh token when it expires
const SESSION_TOKEN_KEY = "saferTogetherToken.v1";
const REFRESH_TOKEN_KEY = "saferTogetherRefresh.v1";

export function setSessionToken(token) {
  if (token) {
    localStorage.setItem(SESSION_TOKEN_KEY, token);
  } else {
    localStorage.removeItem(SESSION_TOKEN_KEY);
  }
}

export function setRefreshToken(token) {
  if (token) {
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
  } else {
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }
}

function getSessionToken() {
  return localStorage.getItem(SESSION_TOKEN_KEY);
}

function getRefreshToken() {
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

// de-dupe concurrent refreshes so a burst of 401s only triggers one refresh call
let refreshInFlight = null;

async function refreshAccessToken() {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    return null;
  }

  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        const response = await fetch("/api/auth/refresh", {
          body: JSON.stringify({ refreshToken }),
          headers: { "Content-Type": "application/json" },
          method: "POST"
        });

        if (!response.ok) {
          // refresh token dead -> clear and send them back to login
          setSessionToken(null);
          setRefreshToken(null);
          return null;
        }

        const text = await response.text();
        const payload = text ? JSON.parse(text) : null;

        if (payload?.accessToken) {
          setSessionToken(payload.accessToken);
          if (payload.refreshToken) {
            setRefreshToken(payload.refreshToken);
          }
          return payload.accessToken;
        }

        return null;
      } catch {
        return null;
      }
    })().finally(() => {
      refreshInFlight = null;
    });
  }

  return refreshInFlight;
}

// send a json request, get back parsed json
async function requestJson(path, options = {}, retried = false) {
  const token = getSessionToken();
  const headers = {
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(options.headers || {})
  };

  const requestInit = {
    credentials: "same-origin",
    headers,
    method: options.method || "GET"
  };

  if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
    requestInit.body = JSON.stringify(options.body);
  }

  const response = await fetch(path, requestInit);

  // access token likely expired -> refresh once and retry the same request
  if (response.status === 401 && !retried && path !== "/api/auth/refresh" && getRefreshToken()) {
    const newToken = await refreshAccessToken();
    if (newToken) {
      return requestJson(path, options, true);
    }
  }

  const responseText = await response.text();
  const payload = responseText ? JSON.parse(responseText) : null;

  if (!response.ok) {
    throw new Error(payload?.error || "Request failed");
  }

  return payload;
}

export {
  requestJson
};
