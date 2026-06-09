const { httpError } = require("../Services/errors");

// read the body and parse it as json
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

// send back json
function sendJson(response, statusCode, payload, extraHeaders = {}) {
  response.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    ...extraHeaders
  });
  response.end(JSON.stringify(payload));
}

// send an error response
function sendRouteError(response, error) {
  const statusCode = error.statusCode || error.status || 500;
  const message = error.message || "Unexpected server error";
  sendJson(response, statusCode, { error: message });
}

// pull the cookies out of the request headers
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

// grab the access token from the request (header or cookie)
function getAccessTokenFromRequest(request) {
  const authorization = request.headers.authorization || "";

  if (authorization.startsWith("Bearer ")) {
    return authorization.slice("Bearer ".length).trim();
  }

  const cookies = parseCookies(request);
  return cookies.safer_access_token || "";
}

// build a cookie string with the given options
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

// set the auth cookies on the response
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

// wipe the auth cookies
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
