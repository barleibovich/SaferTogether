const http = require("http");
const fs = require("fs");
const path = require("path");
const { getConfig } = require("../Services/configService");
const { handleAuthRoute } = require("./routes/authRoutes");
const { handleGroupRoute } = require("./routes/groupRoutes");

const mimeTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".jpeg": "image/jpeg",
  ".jpg": "image/jpeg",
  ".js": "application/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".svg": "image/svg+xml"
};

// This function sends API requests to the matching route handler.
async function handleApiRoute(request, response, pathname) {
  if (await handleAuthRoute(request, response, pathname)) {
    return true;
  }

  if (await handleGroupRoute(request, response, pathname)) {
    return true;
  }

  return false;
}

// This function serves static files from the frontend folder.
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

  fs.readFile(filePath, (error, data) => {
    if (error) {
      response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
      response.end("Not found");
      return;
    }

    const extension = path.extname(filePath).toLowerCase();
    response.writeHead(200, {
      "Content-Type": mimeTypes[extension] || "application/octet-stream"
    });
    response.end(data);
  });
}

// This function creates the main HTTP server.
function createServer() {
  return http.createServer(async (request, response) => {
    const requestUrl = new URL(request.url, `http://${request.headers.host}`);

    if (requestUrl.pathname.startsWith("/api/")) {
      const handled = await handleApiRoute(request, response, requestUrl.pathname);
      if (!handled) {
        response.writeHead(404, { "Content-Type": "application/json; charset=utf-8" });
        response.end(JSON.stringify({ error: "Not found" }));
      }
      return;
    }

    serveStaticFile(request, response);
  });
}

// This function starts the server on an open port.
function startServer() {
  const { port } = getConfig();
  const server = createServer();

  // This function retries the server with the next port if needed.
  function listen(nextPort, attemptsLeft = 20) {
    server.once("error", error => {
      if (error.code === "EADDRINUSE" && attemptsLeft > 0) {
        listen(nextPort + 1, attemptsLeft - 1);
        return;
      }

      throw error;
    });

    server.listen(nextPort, () => {
      console.log(`SaferTogether gateway running at http://localhost:${nextPort}`);
    });
  }

  listen(port);
}

module.exports = {
  startServer
};

if (require.main === module) {
  startServer();
}
