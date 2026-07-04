// avatar id parsing + normalize rules (Quaternius character pack)

// avatar ids look like "pack:<character>" and match the Unity prefabs 1:1
const PACK_CHARACTERS = [
  "adventurer",
  "beach",
  "casual",
  "casual2",
  "farmer",
  "king",
  "punk",
  "spacesuit",
  "suit",
  "swat",
  "worker"
];

const DEFAULT_PACK_CHARACTER = "adventurer";

// turn a username into a stable number
function seedFromUsername(username) {
  return String(username || "")
    .trim()
    .toLowerCase()
    .split("")
    .reduce((sum, char) => sum + char.charCodeAt(0), 0);
}

// check + normalize a pack avatar id ("pack:<character>")
function parsePackAvatar(avatar) {
  const value = String(avatar || "").trim().toLowerCase();

  if (!value.startsWith("pack:")) {
    return "";
  }

  const name = value.slice("pack:".length);
  return PACK_CHARACTERS.includes(name) ? `pack:${name}` : "";
}

// deterministic default character so users without a saved pick still differ from each other
function packAvatarFromUsername(username) {
  const seed = seedFromUsername(username);
  return `pack:${PACK_CHARACTERS[seed % PACK_CHARACTERS.length]}`;
}

// keep a valid pack id, else pick the user's default (old ids map here too)
function normalizeAvatar(avatar, username) {
  return parsePackAvatar(avatar) || packAvatarFromUsername(username);
}

module.exports = {
  normalizeAvatar,
  packAvatarFromUsername,
  PACK_CHARACTERS,
  DEFAULT_PACK_CHARACTER
};
