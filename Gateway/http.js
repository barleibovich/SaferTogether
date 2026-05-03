const { httpError } = require("../Services/errors");

// This function reads and parses a JSON request body.
function readJsonBody(request) {
  return new Promise((resolve, reject) => {
    const chunks = [];

    request.on("data", chunk => {
      chunks.push(chunk);
    });

    request.on("end", () => {
      if (!chunks.length) {
        resolve({});
        return;
      }

      try {
        const body = Buffer.concat(chunks).toString("utf8");
        resolve(body ? JSON.parse(body) : {});
      } catch {
        reject(httpError(400, "Invalid JSON body"));
      }
    });

    request.on("error", reject);
  });
}

// This function sends a JSON response.
function sendJson(response, statusCode, payload, extraHeaders = {}) {
  response.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    ...extraHeaders
  });
  response.end(JSON.stringify(payload));
}

// This function sends an error response for a route.
function sendRouteError(response, error) {
  const statusCode = error.statusCode || error.status || 500;
  const message = error.message || "Unexpected server error";
  sendJson(response, statusCode, { error: message });
}

// This function parses cookies from the request headers.
function parseCookies(request) {
  const cookieHeader = request.headers.cookie || "";

  return cookieHeader.split(";").reduce((cookies, part) => {
    const [rawName, ...rest] = part.trim().split("=");
    if (!rawName) {
      return cookies;
    }

    cookies[rawName] = decodeURIComponent(rest.join("="));
    return cookies;
  }, {});
}

// This function gets the access token from the request.
function getAccessTokenFromRequest(request) {
  const authorization = request.headers.authorization || "";

  if (authorization.startsWith("Bearer ")) {
    return authorization.slice("Bearer ".length).trim();
  }

  const cookies = parseCookies(request);
  return cookies.safer_access_token || "";
}

// This function builds a cookie string with options.
function serializeCookie(name, value, options = {}) {
  const parts = [`${name}=${encodeURIComponent(value)}`];

  if (options.httpOnly) {
    parts.push("HttpOnly");
  }

  if (options.maxAge !== undefined) {
    parts.push(`Max-Age=${options.maxAge}`);
  }

  parts.push(`Path=${options.path || "/"}`);
  parts.push(`SameSite=${options.sameSite || "Lax"}`);

  return parts.join("; ");
}

// This function sets the auth cookies on the response.
function setAuthCookies(response, session) {
  const cookies = [
    serializeCookie("safer_access_token", session.access_token, {
      httpOnly: true,
      maxAge: session.expires_in || 3600
    }),
    serializeCookie("safer_refresh_token", session.refresh_token || "", {
      httpOnly: true,
      maxAge: 60 * 60 * 24 * 30
    })
  ];

  response.setHeader("Set-Cookie", cookies);
}

// This function clears the auth cookies from the response.
function clearAuthCookies(response) {
  response.setHeader("Set-Cookie", [
    serializeCookie("safer_access_token", "", { httpOnly: true, maxAge: 0 }),
    serializeCookie("safer_refresh_token", "", { httpOnly: true, maxAge: 0 })
  ]);
}

module.exports = {
  clearAuthCookies,
  getAccessTokenFromRequest,
  readJsonBody,
  sendJson,
  sendRouteError,
  setAuthCookies
};
