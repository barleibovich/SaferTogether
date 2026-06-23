const fs = require("fs");
const path = require("path");

let cachedConfig = null;

// read key=value lines out of an env file
function readEnvFile(filePath) {
  if (!fs.existsSync(filePath)) {
    return {};
  }

  const fileContents = fs.readFileSync(filePath, "utf8");
  const env = {};

  fileContents.split(/\r?\n/).forEach(line => {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) {
      return;
    }

    const separatorIndex = trimmed.indexOf("=");
    if (separatorIndex < 0) {
      return;
    }

    const key = trimmed.slice(0, separatorIndex).trim();
    let value = trimmed.slice(separatorIndex + 1).trim();

    if (
      (value.startsWith('"') && value.endsWith('"')) ||
      (value.startsWith("'") && value.endsWith("'"))
    ) {
      value = value.slice(1, -1);
    }

    env[key] = value;
  });

  return env;
}

// build the app config once and cache it
function getConfig() {
  if (cachedConfig) {
    return cachedConfig;
  }

  const repoRoot = path.join(__dirname, "..");
  const gatewayEnv = readEnvFile(path.join(repoRoot, "Gateway", ".env"));
  const env = {
    ...gatewayEnv,
    ...process.env
  };

  const supabaseUrl = env.SUPABASE_URL || env.VITE_SUPABASE_URL;
  const supabaseAnonKey = env.SUPABASE_ANON_KEY || env.VITE_SUPABASE_ANON_KEY;

  if (!supabaseUrl || !supabaseAnonKey) {
    throw new Error("Missing Supabase environment variables");
  }

  cachedConfig = {
    frontendRoot: path.join(repoRoot, "SaferTogetherUI"),
    port: Number(env.PORT || 5173),
    supabaseAnonKey,
    supabaseUrl,
    // Optional: powers the AI "user situation" summary on the stats page.
    // Kept server-side only — the key must never be sent to the browser.
    groqApiKey: env.GROQ_API_KEY || "",
    groqModel: env.GROQ_MODEL || "llama-3.3-70b-versatile"
  };

  return cachedConfig;
}

module.exports = {
  getConfig
};
