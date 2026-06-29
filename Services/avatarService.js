// avatar id parsing + normalize rules (Quaternius character pack)

// the only selectable avatars are the imported pack characters. ids are stored as
// "pack:<character>" and map 1:1 to the Unity prefabs under Resources/SaferTogetherAvatars.
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

// keep valid pack ids, fall back to a per-user default otherwise.
// legacy character:v1/v2 + builder ids are no longer selectable, so they normalize to a
// pack character the new Unity editor + mission room can actually render.
function normalizeAvatar(avatar, username) {
  return parsePackAvatar(avatar) || packAvatarFromUsername(username);
}

module.exports = {
  normalizeAvatar,
  packAvatarFromUsername,
  PACK_CHARACTERS,
  DEFAULT_PACK_CHARACTER
};
