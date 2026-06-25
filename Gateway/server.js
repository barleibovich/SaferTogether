const http = require("http");
const fs = require("fs");
const path = require("path");
const { getConfig } = require("../Services/configService");
const { handleActivityRoute } = require("./routes/activityRoutes");
const { handleAlarmRoute } = require("./routes/alarmRoutes");
const { handleAuthRoute } = require("./routes/authRoutes");
const { handleGroupRoute } = require("./routes/groupRoutes");
const { handleOrefRoute } = require("./routes/orefRoutes");
const { handlePresenceRoute } = require("./routes/presenceRoutes");
const { handlePushRoute } = require("./routes/pushRoutes");

const mimeTypes = {
  ".br": "application/octet-stream",
  ".css": "text/css; charset=utf-8",
  ".data": "application/octet-stream",
  ".gz": "application/octet-stream",
  ".html": "text/html; charset=utf-8",
  ".jpeg": "image/jpeg",
  ".jpg": "image/jpeg",
  ".js": "application/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".mp3": "audio/mpeg",
  ".png": "image/png",
  ".svg": "image/svg+xml",
  ".wasm": "application/wasm"
};

// route the api request to whichever handler matches
async function handleApiRoute(request, response, pathname, requestUrl) {
  if (await handleAuthRoute(request, response, pathname)) {
    return true;
  }

  if (await handleActivityRoute(request, response, pathname, requestUrl)) {
    return true;
  }

  if (await handleAlarmRoute(request, response, pathname)) {
    return true;
  }

  if (await handlePresenceRoute(request, response, pathname)) {
    return true;
  }

  if (await handlePushRoute(request, response, pathname)) {
    return true;
  }

  if (await handleGroupRoute(request, response, pathname)) {
    return true;
  }

  if (await handleOrefRoute(request, response, pathname)) {
    return true;
  }

  return false;
}

// content type for static files (handles .gz/.br too)
function getStaticContentType(filePath) {
  const normalizedPath = filePath.replace(/\.(br|gz)$/i, "");
  const extension = path.extname(normalizedPath).toLowerCase();
  return mimeTypes[extension] || "application/octet-stream";
}

// figure out the encoding for the compressed unity files
function getStaticContentEncoding(filePath) {
  if (filePath.endsWith(".br")) {
    return "br";
  }

  if (filePath.endsWith(".gz")) {
    return "gzip";
  }

  return "";
}

// serve static files from the frontend folder
function serveStaticFile(request, response) {
  const { frontendRoot } = getConfig();
  const requestUrl = new URL(request.url, `http://${request.headers.host}`);
  const requestPath = decodeURIComponent(requestUrl.pathname);
  const relativePath = requestPath === "/" ? "index.html" : requestPath.replace(/^[/\\]+/, "");
  const filePath = path.resolve(frontendRoot, relativePath);

  if (!filePath.startsWith(frontendRoot + path.sep)) {
    response.writeHead(403, { "Content-Type": "text/plain; charset=utf-8" });
    response.end("Forbidden");
    return;
  }

  // callback after we try reading the file off disk
  fs.readFile(filePath, (error, data) => {
    if (error) {
      response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
      response.end("Not found");
      return;
    }

    const headers = {
      "Content-Length": data.length,
      "Content-Type": getStaticContentType(filePath)
    };
    const contentEncoding = getStaticContentEncoding(filePath);

    if (contentEncoding) {
      headers["Content-Encoding"] = contentEncoding;
    }

    response.writeHead(200, headers);
    response.end(data);
  });
}

// make the main http server
function createServer() {
  // runs for every incoming request, splits api vs static
  return http.createServer(async (request, response) => {
    const requestUrl = new URL(request.url, `http://${request.headers.host}`);

    if (requestUrl.pathname.startsWith("/api/")) {
      const handled = await handleApiRoute(request, response, requestUrl.pathname, requestUrl);
      if (!handled) {
        response.writeHead(404, { "Content-Type": "application/json; charset=utf-8" });
        response.end(JSON.stringify({ error: "Not found" }));
      }
      return;
    }

    serveStaticFile(request, response);
  });
}

// start it up on the port from config
function startServer() {
  const { port } = getConfig();
  const server = createServer();

  server.once("error", error => {
    if (error.code === "EADDRINUSE") {
      console.error(`Port ${port} is already in use. Stop the existing gateway before starting a new one.`);
      process.exit(1);
    }

    throw error;
  });

  server.listen(port, () => {
    console.log(`SaferTogether gateway running at http://localhost:${port}`);
  });
}

module.exports = {
  startServer
};

if (require.main === module) {
  startServer();
}
