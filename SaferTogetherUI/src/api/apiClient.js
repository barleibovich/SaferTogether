const SESSION_TOKEN_KEY = "saferTogetherToken.v1";

export function setSessionToken(token) {
  if (token) {
    sessionStorage.setItem(SESSION_TOKEN_KEY, token);
  } else {
    sessionStorage.removeItem(SESSION_TOKEN_KEY);
  }
}

// send a json request, get back parsed json
async function requestJson(path, options = {}) {
  const token = sessionStorage.getItem(SESSION_TOKEN_KEY);
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
