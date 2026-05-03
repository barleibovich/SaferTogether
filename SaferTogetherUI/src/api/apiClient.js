async function requestJson(path, options = {}) {
  const headers = {
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
