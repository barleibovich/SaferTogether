// all the avatar parsing + normalize rules live here

const AVATAR_OPTIONS = ["aqua", "mint", "sun", "rose", "violet", "steel"];
const AVATAR_BUILDER_COLORS = [
  "aqua",
  "mint",
  "sun",
  "rose",
  "violet",
  "steel",
  "coral",
  "lime",
  "sky",
  "peach"
];
const AVATAR_BUILDER_EYES = ["dot", "line", "happy", "wink"];
const AVATAR_BUILDER_SHAPES = ["circle", "square", "diamond", "hex"];

const LEGACY_CHARACTER_ACCESSORIES = ["none", "glasses", "cap", "badge", "mask"];
const LEGACY_CHARACTER_EYES = ["dot", "line", "happy", "focused"];
const LEGACY_CHARACTER_HAIR_COLORS = ["black", "brown", "blonde", "red", "blue", "silver"];
const LEGACY_CHARACTER_HAIR_STYLES = ["short", "bob", "curls", "spiky", "hijab", "none"];
const LEGACY_CHARACTER_MOUTHS = ["smile", "calm", "open", "flat"];
const LEGACY_CHARACTER_SHIRTS = ["tee", "hoodie", "jacket", "vest"];
const LEGACY_CHARACTER_SKINS = ["light", "tan", "brown", "deep"];

const CHARACTER_ACCESSORIES = [
  "none",
  "bandana",
  "crown",
  "glasses",
  "mask"
];
const CHARACTER_BACKGROUNDS = [
  ...AVATAR_BUILDER_COLORS,
  "navy",
  "white",
  "black",
  "red",
  "green",
  "denim"
];
const CHARACTER_BOTTOMS = ["jeans", "cargo", "sports"];
const CHARACTER_CLOTHING_COLORS = ["black", "blue", "green", "red", "white", "yellow", "denim"];
const CHARACTER_EYE_COLORS = ["brown", "blue", "green", "hazel", "violet", "amber", "gray"];
const CHARACTER_EYES = ["dot", "almond", "happy", "focused", "sleepy"];
const CHARACTER_FACE_SHAPES = ["round", "soft", "sharp", "snout", "long"];
const CHARACTER_HAIR_COLORS = ["black", "brown", "blonde", "red", "blue", "pink", "silver", "white"];
const CHARACTER_HAIR_STYLES = [
  "short",
  "bob",
  "curls",
  "spiky",
  "long",
  "ponytail",
  "bun",
  "mohawk",
  "hijab",
  "none"
];
const CHARACTER_SEXES = ["female", "male"];
const CHARACTER_SHOES = ["sneakers", "boots", "space-shoes"];
const CHARACTER_SKINS = ["porcelain", "light", "tan", "brown", "deep", "green", "red", "gray", "gold"];
const CHARACTER_SPECIES = ["male", "female", "dragon", "devil"];
const CHARACTER_TOPS = ["tee", "sweatshirt", "undershirt"];

const DEFAULT_CHARACTER_V2 = {
  accessory: "none",
  background: "sky",
  bottom: "jeans",
  bottomColor: "denim",
  eyeColor: "brown",
  eyes: "almond",
  face: "soft",
  hair: "short",
  hairColor: "brown",
  sex: "male",
  shoes: "sneakers",
  shoeColor: "white",
  skin: "tan",
  species: "male",
  top: "tee",
  topColor: "aqua"
};

// turn a username into a stable number
function seedFromUsername(username) {
  return String(username || "")
    .trim()
    .toLowerCase()
    .split("")
    .reduce((sum, char) => sum + char.charCodeAt(0), 0);
}

// build a character:v2 id from a spec
function buildCharacterAvatarV2(spec) {
  return [
    "character",
    "v2",
    spec.species,
    spec.sex,
    spec.skin,
    spec.face,
    spec.eyes,
    spec.eyeColor,
    spec.hair,
    spec.hairColor,
    spec.top,
    spec.topColor,
    spec.bottom,
    spec.bottomColor,
    spec.shoes,
    spec.shoeColor,
    spec.accessory,
    spec.background
  ].join(":");
}

// clean up the species, treat "human" as male
function normalizeCharacterSpecies(value) {
  const cleanValue = String(value || "").trim().toLowerCase();
  if (cleanValue === "human") return "male";
  return CHARACTER_SPECIES.includes(cleanValue) ? cleanValue : DEFAULT_CHARACTER_V2.species;
}

// default avatar built off the username
function avatarFromUsername(username) {
  const seed = seedFromUsername(username);
  const colors = CHARACTER_CLOTHING_COLORS;

  return buildCharacterAvatarV2({
    ...DEFAULT_CHARACTER_V2,
    background: colors[(seed + 2) % colors.length],
    eyeColor: CHARACTER_EYE_COLORS[seed % CHARACTER_EYE_COLORS.length],
    hairColor: CHARACTER_HAIR_COLORS[(seed + 3) % CHARACTER_HAIR_COLORS.length],
    topColor: colors[(seed + 5) % colors.length]
  });
}

// check + normalize a builder avatar id
function parseBuilderAvatar(avatar) {
  const value = String(avatar || "").trim().toLowerCase();

  if (!value.startsWith("builder:")) {
    return "";
  }

  const parts = value.split(":");

  if (parts.length !== 5) {
    return "";
  }

  const shape = parts[1];
  const base = parts[2];
  const accent = parts[3];
  const eyes = parts[4];

  if (!AVATAR_BUILDER_SHAPES.includes(shape)) {
    return "";
  }

  if (!AVATAR_BUILDER_COLORS.includes(base) || !AVATAR_BUILDER_COLORS.includes(accent)) {
    return "";
  }

  if (!AVATAR_BUILDER_EYES.includes(eyes)) {
    return "";
  }

  return `builder:${shape}:${base}:${accent}:${eyes}`;
}

// check + normalize an old (v1) character id
function parseLegacyCharacterAvatar(avatar) {
  const value = String(avatar || "").trim().toLowerCase();

  if (!value.startsWith("character:v1:")) {
    return "";
  }

  const parts = value.split(":");

  if (parts.length !== 11) {
    return "";
  }

  const skin = parts[2];
  const hair = parts[3];
  const hairColor = parts[4];
  const shirt = parts[5];
  const shirtColor = parts[6];
  const eyes = parts[7];
  const mouth = parts[8];
  const accessory = parts[9];
  const background = parts[10];

  if (!LEGACY_CHARACTER_SKINS.includes(skin) || !LEGACY_CHARACTER_HAIR_STYLES.includes(hair)) {
    return "";
  }

  if (!LEGACY_CHARACTER_HAIR_COLORS.includes(hairColor) || !LEGACY_CHARACTER_SHIRTS.includes(shirt)) {
    return "";
  }

  if (!AVATAR_BUILDER_COLORS.includes(shirtColor) || !LEGACY_CHARACTER_EYES.includes(eyes)) {
    return "";
  }

  if (!LEGACY_CHARACTER_MOUTHS.includes(mouth) || !LEGACY_CHARACTER_ACCESSORIES.includes(accessory)) {
    return "";
  }

  if (!AVATAR_BUILDER_COLORS.includes(background)) {
    return "";
  }

  return [
    "character",
    "v1",
    skin,
    hair,
    hairColor,
    shirt,
    shirtColor,
    eyes,
    mouth,
    accessory,
    background
  ].join(":");
}

// check + normalize a full character id
function parseCharacterAvatar(avatar) {
  const value = String(avatar || "").trim().toLowerCase();

  if (!value.startsWith("character:v2:")) {
    return "";
  }

  const parts = value.split(":");

  if (parts.length !== 18) {
    return "";
  }

  const spec = {
    accessory: parts[16],
    background: parts[17],
    bottom: parts[12],
    bottomColor: parts[13],
    eyeColor: parts[7],
    eyes: parts[6],
    face: parts[5],
    hair: parts[8],
    hairColor: parts[9],
    sex: parts[3],
    shoes: parts[14],
    shoeColor: parts[15],
    skin: parts[4],
    species: normalizeCharacterSpecies(parts[2]),
    top: parts[10],
    topColor: parts[11]
  };
  spec.sex = spec.species === "female" ? "female" : spec.species === "male" ? "male" : spec.sex;

  if (!CHARACTER_SPECIES.includes(spec.species) || !CHARACTER_SEXES.includes(spec.sex)) {
    return "";
  }

  if (!CHARACTER_SKINS.includes(spec.skin) || !CHARACTER_FACE_SHAPES.includes(spec.face)) {
    return "";
  }

  if (!CHARACTER_EYES.includes(spec.eyes) || !CHARACTER_EYE_COLORS.includes(spec.eyeColor)) {
    return "";
  }

  if (!CHARACTER_HAIR_STYLES.includes(spec.hair) || !CHARACTER_HAIR_COLORS.includes(spec.hairColor)) {
    return "";
  }

  if (!CHARACTER_TOPS.includes(spec.top) || !CHARACTER_CLOTHING_COLORS.includes(spec.topColor)) {
    return "";
  }

  if (!CHARACTER_BOTTOMS.includes(spec.bottom) || !CHARACTER_CLOTHING_COLORS.includes(spec.bottomColor)) {
    return "";
  }

  if (!CHARACTER_SHOES.includes(spec.shoes) || !CHARACTER_CLOTHING_COLORS.includes(spec.shoeColor)) {
    return "";
  }

  if (!CHARACTER_ACCESSORIES.includes(spec.accessory) || !CHARACTER_BACKGROUNDS.includes(spec.background)) {
    return "";
  }

  return buildCharacterAvatarV2(spec);
}

// keep known avatar ids, fall back to default otherwise
function normalizeAvatar(avatar, username) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();
  const characterAvatar = parseCharacterAvatar(cleanAvatar);
  const legacyCharacterAvatar = parseLegacyCharacterAvatar(cleanAvatar);
  const builderAvatar = parseBuilderAvatar(cleanAvatar);

  if (AVATAR_OPTIONS.includes(cleanAvatar)) {
    return cleanAvatar;
  }

  if (characterAvatar) {
    return characterAvatar;
  }

  if (legacyCharacterAvatar) {
    return legacyCharacterAvatar;
  }

  if (builderAvatar) {
    return builderAvatar;
  }

  return avatarFromUsername(username);
}

module.exports = {
  normalizeAvatar
};
