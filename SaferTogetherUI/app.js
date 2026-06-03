import {
  getCurrentUserProfile,
  loginWithUsername,
  logout,
  signUpWithUsername
} from "./src/api/authGateway.js";
import {
  createGroupForCurrentUser,
  deleteOwnedGroup,
  endDrill,
  fetchDrillStatus,
  getCurrentUserGroups,
  leaveGroup,
  markSafe,
  renameGroup,
  requestJoinByCode,
  reviewJoinRequest,
  startDrill
} from "./src/api/groupGateway.js";
import {
  getGroupOrefStatus,
  saveCurrentUserAlertLocation
} from "./src/api/orefGateway.js";

const STORAGE_KEY = "saferTogetherState.v5";
const SIGNUP_AVATAR_KEY = "saferTogetherSignupAvatar.v1";
const EVENT_DURATION_SECONDS = 600;
const OREF_POLL_INTERVAL_MS = 5000;
const GPS_LOCATION_SAVE_INTERVAL_MS = 15000;
const GPS_LOCATION_DISTANCE_THRESHOLD_METERS = 50;
const AVATAR_OPTIONS = ["aqua", "mint", "sun", "rose", "violet", "steel"];
const AVATAR_BUILDER_SHAPES = ["circle", "square", "diamond", "hex"];
const AVATAR_BUILDER_COLORS = ["aqua", "mint", "sun", "rose", "violet", "steel", "coral", "lime", "sky", "peach"];
const AVATAR_BUILDER_EYES = ["dot", "line", "happy", "wink"];
const LEGACY_CHARACTER_ACCESSORIES = ["none", "glasses", "cap", "badge", "mask"];
const LEGACY_CHARACTER_EYES = ["dot", "line", "happy", "focused"];
const LEGACY_CHARACTER_HAIR_COLORS = ["black", "brown", "blonde", "red", "blue", "silver"];
const LEGACY_CHARACTER_HAIR_STYLES = ["short", "bob", "curls", "spiky", "hijab", "none"];
const LEGACY_CHARACTER_MOUTHS = ["smile", "calm", "open", "flat"];
const LEGACY_CHARACTER_SHIRTS = ["tee", "hoodie", "jacket", "vest"];
const LEGACY_CHARACTER_SKINS = ["light", "tan", "brown", "deep"];
const CHARACTER_ACCESSORIES = ["none", "glasses", "cap", "crown", "mask", "headphones", "wings", "halo", "horns", "tail"];
const CHARACTER_BACKGROUNDS = [...AVATAR_BUILDER_COLORS, "navy", "white", "black", "red", "green", "denim"];
const CHARACTER_BOTTOMS = ["jeans", "training", "shorts", "skirt", "cargo", "leggings"];
const CHARACTER_CLOTHING_COLORS = CHARACTER_BACKGROUNDS;
const CHARACTER_EYE_COLORS = ["brown", "blue", "green", "hazel", "violet", "amber", "gray"];
const CHARACTER_EYES = ["dot", "almond", "happy", "focused", "sleepy"];
const CHARACTER_FACE_SHAPES = ["round", "soft", "sharp", "snout", "long"];
const CHARACTER_HAIR_COLORS = ["black", "brown", "blonde", "red", "blue", "pink", "silver", "white"];
const CHARACTER_HAIR_STYLES = ["short", "bob", "curls", "spiky", "long", "ponytail", "bun", "mohawk", "hijab", "none"];
const CHARACTER_SEXES = ["female", "male"];
const CHARACTER_SHOES = ["sneakers", "boots", "sandals", "slippers", "none"];
const CHARACTER_SKINS = ["porcelain", "light", "tan", "brown", "deep", "green", "red", "gray", "gold"];
const CHARACTER_SPECIES = ["human", "dragon", "bear", "elephant", "devil", "angel"];
const CHARACTER_TOPS = ["tee", "shirt", "hoodie", "sweatshirt", "jacket", "vest", "armor", "dress"];
const AVATAR_COLOR_VALUES = {
  aqua: "#66d9ef",
  black: "#222831",
  coral: "#ff8473",
  denim: "#4169a8",
  green: "#2f9e44",
  lime: "#aee66f",
  mint: "#73e2a7",
  navy: "#203a63",
  peach: "#ffbb8f",
  red: "#e04f5f",
  rose: "#ff9bb0",
  sky: "#78c6ff",
  steel: "#b8c7d9",
  sun: "#ffd36a",
  violet: "#c4a7ff",
  white: "#f8fafc"
};
const CHARACTER_HAIR_COLOR_VALUES = {
  black: "#231f20",
  blonde: "#eac15d",
  blue: "#366ab8",
  brown: "#5b3724",
  pink: "#d96fb1",
  red: "#ac4831",
  silver: "#c2c5cb",
  white: "#f3f4f6"
};
const CHARACTER_EYE_COLOR_VALUES = {
  amber: "#d48b28",
  blue: "#2f80ed",
  brown: "#5b3724",
  gray: "#7b8794",
  green: "#2f9e44",
  hazel: "#8a6f2a",
  violet: "#7c5cff"
};
const CHARACTER_SKIN_VALUES = {
  brown: "#a76947",
  deep: "#693f2d",
  gold: "#d8aa45",
  gray: "#9aa4ad",
  green: "#59b06f",
  light: "#ffd6b9",
  porcelain: "#ffe6d5",
  red: "#c94f45",
  tan: "#da9a69"
};
const DEFAULT_CHARACTER_SPEC = {
  accessory: "none",
  background: "sky",
  bottom: "jeans",
  bottomColor: "denim",
  eyeColor: "brown",
  eyes: "almond",
  face: "soft",
  hair: "short",
  hairColor: "brown",
  sex: "female",
  shoes: "sneakers",
  shoeColor: "white",
  skin: "tan",
  species: "human",
  top: "tee",
  topColor: "aqua"
};
const DEFAULT_FAMILY = [
  { id: "1", name: "×“×§×œ", role: "×™×œ×“", status: "offline", avatar: "ðŸ¯" },
  { id: "2", name: "×©×™×¨×”", role: "×™×œ×“×”", status: "offline", avatar: "ðŸ¬" },
  { id: "3", name: "××‘×™×‘", role: "×™×œ×“", status: "offline", avatar: "ðŸ¦©" },
  { id: "4", name: "×™×”×œ×™", role: "×™×œ×“", status: "offline", avatar: "ðŸ¦‹" }
];

const DEFAULT_QUESTIONS = [
  {
    id: "q1",
    question: "What should we do after entering the protected room?",
    answers: [
      "Leave after one minute",
      "Stay for 10 minutes",
      "Open the window",
      "Stand near the door"
    ],
    correctAnswerIndex: 1
  }
];

const DEFAULT_MISSIONS = [
  {
    id: "m1",
    title: "Close the window",
    description: "Make sure the window is closed before sitting down."
  },
  {
    id: "m2",
    title: "Bring water",
    description: "Bring a water bottle to the protected room."
  },
  {
    id: "m3",
    title: "Sit away from windows",
    description: "Sit on the floor and stay away from glass."
  }
];

const DEFAULT_BASELINE = {
  userId: null,
  averageAnswerTime: 2.1,
  mistakeRate: 0.1,
  averageTapRate: 1.4,
  averageMovementLevel: 0.2
};

let state = loadState();
let orefGpsWatchId = null;
let lastGpsLocationSave = null;

document.addEventListener("DOMContentLoaded", () => {
  ensureDefaults();
  routePage();
  startEventTimer();
});

// This function creates the default in-browser app state.
function initialState() {
  return {
    user: null,
    activeGroupId: "",
    groups: [],
    familyName: "",
    familyMembers: copy(DEFAULT_FAMILY),
    questions: copy(DEFAULT_QUESTIONS),
    missions: copy(DEFAULT_MISSIONS),
    baseline: copy(DEFAULT_BASELINE),
    practiceSession: null,
    orefStatus: null,
    emergency: null
  };
}

// This function clones simple JSON-safe values.
function copy(value) {
  return JSON.parse(JSON.stringify(value));
}

// This function loads saved app state from session storage.
function loadState() {
  try {
    const saved = sessionStorage.getItem(STORAGE_KEY);
    return saved ? { ...initialState(), ...JSON.parse(saved) } : initialState();
  } catch {
    return initialState();
  }
}

// This function saves the current app state to session storage.
function saveState() {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

// This function stores an avatar coming back from Unity so signup can use it before an account exists.
function syncSignupAvatarDraftFromUrl() {
  const params = new URLSearchParams(window.location.search);
  const avatar = params.get("avatar");

  if (!avatar) {
    return;
  }

  sessionStorage.setItem(SIGNUP_AVATAR_KEY, avatar);
  params.delete("avatar");
  const query = params.toString();
  const cleanUrl = `${window.location.pathname}${query ? `?${query}` : ""}${window.location.hash}`;
  window.history.replaceState(null, "", cleanUrl);
}

// This function keeps Unity navigation inside this prototype instead of allowing arbitrary external redirects.
function safeUnityReturnUrl(profile) {
  const params = new URLSearchParams(window.location.search);
  const requested = params.get("return");
  const fallback = profile ? "groups.html" : "signup.html";

  if (!requested) {
    return fallback;
  }

  let url;

  try {
    url = new URL(requested, window.location.origin);
  } catch {
    return fallback;
  }

  if (url.origin !== window.location.origin || url.pathname.endsWith("/avatar-editor.html")) {
    return fallback;
  }

  return `${url.pathname.replace(/^\//, "")}${url.search}${url.hash}` || fallback;
}

// This function fills any missing state sections with defaults.
function ensureDefaults() {
  state.familyMembers = state.familyMembers?.length ? state.familyMembers : copy(DEFAULT_FAMILY);
  state.groups = Array.isArray(state.groups) ? state.groups : [];
  state.activeGroupId = state.groups.some(group => group.id === state.activeGroupId)
    ? state.activeGroupId
    : state.groups[0]?.id || "";
  state.familyName = state.groups.find(group => group.id === state.activeGroupId)?.name || "";
  state.questions = state.questions?.length ? state.questions : copy(DEFAULT_QUESTIONS);
  state.missions = state.missions?.length ? state.missions : copy(DEFAULT_MISSIONS);
  state.baseline = state.baseline || copy(DEFAULT_BASELINE);
  state.orefStatus = state.orefStatus || null;
  delete state.gpsAlertLocationEnabled;
  saveState();
}

// This function creates a deterministic number from a username.
function seedFromUsername(username) {
  return String(username || "")
    .trim()
    .toLowerCase()
    .split("")
    .reduce((sum, char) => sum + char.charCodeAt(0), 0);
}

// This function returns a supported option value with a fallback.
function optionValue(value, options, fallback) {
  const cleanValue = String(value || "").trim().toLowerCase();
  return options.includes(cleanValue) ? cleanValue : fallback;
}

// This function normalizes a complete character spec.
function normalizeCharacterSpec(spec = {}) {
  return {
    accessory: optionValue(spec.accessory, CHARACTER_ACCESSORIES, DEFAULT_CHARACTER_SPEC.accessory),
    background: optionValue(spec.background, CHARACTER_BACKGROUNDS, DEFAULT_CHARACTER_SPEC.background),
    bottom: optionValue(spec.bottom, CHARACTER_BOTTOMS, DEFAULT_CHARACTER_SPEC.bottom),
    bottomColor: optionValue(spec.bottomColor, CHARACTER_CLOTHING_COLORS, DEFAULT_CHARACTER_SPEC.bottomColor),
    eyeColor: optionValue(spec.eyeColor, CHARACTER_EYE_COLORS, DEFAULT_CHARACTER_SPEC.eyeColor),
    eyes: optionValue(spec.eyes, CHARACTER_EYES, DEFAULT_CHARACTER_SPEC.eyes),
    face: optionValue(spec.face, CHARACTER_FACE_SHAPES, DEFAULT_CHARACTER_SPEC.face),
    hair: optionValue(spec.hair, CHARACTER_HAIR_STYLES, DEFAULT_CHARACTER_SPEC.hair),
    hairColor: optionValue(spec.hairColor, CHARACTER_HAIR_COLORS, DEFAULT_CHARACTER_SPEC.hairColor),
    sex: optionValue(spec.sex, CHARACTER_SEXES, DEFAULT_CHARACTER_SPEC.sex),
    shoes: optionValue(spec.shoes, CHARACTER_SHOES, DEFAULT_CHARACTER_SPEC.shoes),
    shoeColor: optionValue(spec.shoeColor, CHARACTER_CLOTHING_COLORS, DEFAULT_CHARACTER_SPEC.shoeColor),
    skin: optionValue(spec.skin, CHARACTER_SKINS, DEFAULT_CHARACTER_SPEC.skin),
    species: optionValue(spec.species, CHARACTER_SPECIES, DEFAULT_CHARACTER_SPEC.species),
    top: optionValue(spec.top, CHARACTER_TOPS, DEFAULT_CHARACTER_SPEC.top),
    topColor: optionValue(spec.topColor, CHARACTER_CLOTHING_COLORS, DEFAULT_CHARACTER_SPEC.topColor)
  };
}

// This function builds the saved complete avatar id.
function buildCharacterAvatar(spec) {
  const avatar = normalizeCharacterSpec(spec);

  return [
    "character",
    "v2",
    avatar.species,
    avatar.sex,
    avatar.skin,
    avatar.face,
    avatar.eyes,
    avatar.eyeColor,
    avatar.hair,
    avatar.hairColor,
    avatar.top,
    avatar.topColor,
    avatar.bottom,
    avatar.bottomColor,
    avatar.shoes,
    avatar.shoeColor,
    avatar.accessory,
    avatar.background
  ].join(":");
}

// This function creates a stable default avatar from the username.
function avatarSpecFromUsername(username) {
  const seed = seedFromUsername(username);

  return normalizeCharacterSpec({
    ...DEFAULT_CHARACTER_SPEC,
    background: CHARACTER_BACKGROUNDS[(seed + 2) % CHARACTER_BACKGROUNDS.length],
    eyeColor: CHARACTER_EYE_COLORS[seed % CHARACTER_EYE_COLORS.length],
    hairColor: CHARACTER_HAIR_COLORS[(seed + 3) % CHARACTER_HAIR_COLORS.length],
    topColor: CHARACTER_CLOTHING_COLORS[(seed + 5) % CHARACTER_CLOTHING_COLORS.length]
  });
}

// This function creates a stable default avatar id from the username.
function avatarFromUsername(username) {
  return buildCharacterAvatar(avatarSpecFromUsername(username));
}

// This function parses a Unity-built avatar id into its saved parts.
function parseBuilderAvatar(avatar) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();
  const parts = cleanAvatar.split(":");

  if (parts.length !== 5 || parts[0] !== "builder") {
    return null;
  }

  const spec = {
    accentColor: parts[3],
    baseColor: parts[2],
    eyes: parts[4],
    shape: parts[1]
  };

  if (!AVATAR_BUILDER_SHAPES.includes(spec.shape)) {
    return null;
  }

  if (!AVATAR_BUILDER_COLORS.includes(spec.baseColor) || !AVATAR_BUILDER_COLORS.includes(spec.accentColor)) {
    return null;
  }

  if (!AVATAR_BUILDER_EYES.includes(spec.eyes)) {
    return null;
  }

  return {
    ...spec,
    id: `builder:${spec.shape}:${spec.baseColor}:${spec.accentColor}:${spec.eyes}`
  };
}

// This function parses a legacy character avatar id into its saved parts.
function parseLegacyCharacterAvatar(avatar) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();
  const parts = cleanAvatar.split(":");

  if (parts.length !== 11 || parts[0] !== "character" || parts[1] !== "v1") {
    return null;
  }

  const spec = {
    accessory: parts[9],
    background: parts[10],
    eyes: parts[7],
    hair: parts[3],
    hairColor: parts[4],
    mouth: parts[8],
    shirt: parts[5],
    shirtColor: parts[6],
    skin: parts[2]
  };

  if (!LEGACY_CHARACTER_SKINS.includes(spec.skin) || !LEGACY_CHARACTER_HAIR_STYLES.includes(spec.hair)) {
    return null;
  }

  if (!LEGACY_CHARACTER_HAIR_COLORS.includes(spec.hairColor) || !LEGACY_CHARACTER_SHIRTS.includes(spec.shirt)) {
    return null;
  }

  if (!AVATAR_BUILDER_COLORS.includes(spec.shirtColor) || !LEGACY_CHARACTER_EYES.includes(spec.eyes)) {
    return null;
  }

  if (!LEGACY_CHARACTER_MOUTHS.includes(spec.mouth) || !LEGACY_CHARACTER_ACCESSORIES.includes(spec.accessory)) {
    return null;
  }

  if (!AVATAR_BUILDER_COLORS.includes(spec.background)) {
    return null;
  }

  return {
    ...spec,
    id: [
      "character",
      "v1",
      spec.skin,
      spec.hair,
      spec.hairColor,
      spec.shirt,
      spec.shirtColor,
      spec.eyes,
      spec.mouth,
      spec.accessory,
      spec.background
    ].join(":")
  };
}

// This function parses a complete character avatar id into its saved parts.
function parseCharacterAvatar(avatar) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();
  const parts = cleanAvatar.split(":");

  if (parts.length !== 18 || parts[0] !== "character" || parts[1] !== "v2") {
    return null;
  }

  const spec = normalizeCharacterSpec({
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
    species: parts[2],
    top: parts[10],
    topColor: parts[11]
  });

  if (buildCharacterAvatar(spec) !== cleanAvatar) {
    return null;
  }

  return {
    ...spec,
    id: buildCharacterAvatar(spec)
  };
}

// This function maps old character ids to the complete editor model.
function legacyCharacterToSpec(legacyAvatar) {
  if (!legacyAvatar) return null;

  return normalizeCharacterSpec({
    ...DEFAULT_CHARACTER_SPEC,
    accessory: legacyAvatar.accessory === "badge" ? "crown" : legacyAvatar.accessory,
    background: legacyAvatar.background,
    eyes: legacyAvatar.eyes === "line" ? "sleepy" : legacyAvatar.eyes,
    hair: legacyAvatar.hair,
    hairColor: legacyAvatar.hairColor,
    skin: legacyAvatar.skin,
    top: legacyAvatar.shirt === "tee" ? "tee" : legacyAvatar.shirt,
    topColor: legacyAvatar.shirtColor
  });
}

// This function maps builder ids and color presets to the complete editor model.
function compactAvatarToSpec(avatar, username) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();
  const builderAvatar = parseBuilderAvatar(cleanAvatar);
  const baseColor = builderAvatar?.baseColor || (AVATAR_OPTIONS.includes(cleanAvatar) ? cleanAvatar : "");
  const accentColor = builderAvatar?.accentColor || "";

  if (!baseColor) {
    return avatarSpecFromUsername(username);
  }

  return normalizeCharacterSpec({
    ...avatarSpecFromUsername(username),
    background: accentColor || "sky",
    eyes: builderAvatar?.eyes === "wink" ? "focused" : "happy",
    topColor: baseColor
  });
}

// This function returns the complete character spec for any supported avatar id.
function avatarSpecFromAvatar(avatar, username) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();
  const characterAvatar = parseCharacterAvatar(cleanAvatar);
  const legacyCharacterAvatar = parseLegacyCharacterAvatar(cleanAvatar);

  if (characterAvatar) {
    return characterAvatar;
  }

  if (legacyCharacterAvatar) {
    return legacyCharacterToSpec(legacyCharacterAvatar);
  }

  return compactAvatarToSpec(cleanAvatar, username);
}

// This function accepts known preset, old builder, and character avatar ids.
function normalizeAvatar(avatar, username) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();
  const characterAvatar = parseCharacterAvatar(cleanAvatar);
  const legacyCharacterAvatar = parseLegacyCharacterAvatar(cleanAvatar);
  const builderAvatar = parseBuilderAvatar(cleanAvatar);

  if (AVATAR_OPTIONS.includes(cleanAvatar)) {
    return cleanAvatar;
  }

  if (characterAvatar) {
    return characterAvatar.id;
  }

  if (legacyCharacterAvatar) {
    return legacyCharacterAvatar.id;
  }

  if (builderAvatar) {
    return builderAvatar.id;
  }

  return avatarFromUsername(username);
}

// This function returns the CSS class for an avatar id.
function avatarClass(avatar, username) {
  const spec = avatarSpecFromAvatar(avatar, username);
  return [
    "avatar-character-v2",
    "avatar-illustrated",
    `avatar-species-${spec.species}`,
    `avatar-sex-${spec.sex}`,
    `avatar-face-${spec.face}`,
    `avatar-eyes-${spec.eyes}`,
    `avatar-hair-${spec.hair}`,
    `avatar-top-${spec.top}`,
    `avatar-bottom-${spec.bottom}`,
    `avatar-shoes-${spec.shoes}`,
    `avatar-accessory-${spec.accessory}`
  ].join(" ");
}

function avatarArtPath(avatar, username) {
  const spec = avatarSpecFromAvatar(avatar, username);
  return `assets/avatar-art/${spec.species}.png`;
}

// This function returns CSS variables for an avatar.
function avatarStyle(avatar, username) {
  const spec = avatarSpecFromAvatar(avatar, username);
  return ` style="--avatar-bg:${AVATAR_COLOR_VALUES[spec.background]};--avatar-skin:${CHARACTER_SKIN_VALUES[spec.skin]};--avatar-eye:${CHARACTER_EYE_COLOR_VALUES[spec.eyeColor]};--avatar-hair:${CHARACTER_HAIR_COLOR_VALUES[spec.hairColor]};--avatar-top:${AVATAR_COLOR_VALUES[spec.topColor]};--avatar-bottom:${AVATAR_COLOR_VALUES[spec.bottomColor]};--avatar-shoe:${AVATAR_COLOR_VALUES[spec.shoeColor]};"`;
}

// This function chooses a readable initial for the avatar badge.
function avatarInitial(username) {
  return String(username || "?").trim().charAt(0).toUpperCase() || "?";
}

// This function renders the CSS-built character avatar from the saved avatar id.
function renderAvatarBadge(username, avatar, className = "profile-avatar") {
  return `
    <span class="${className} ${avatarClass(avatar, username)}"${avatarStyle(avatar, username)}>
      <img class="avatar-art-image" src="${avatarArtPath(avatar, username)}" alt="" aria-hidden="true">
      <span class="avatar-art-overlay" aria-hidden="true">
        <span class="avatar-art-wing"></span>
        <span class="avatar-art-tail"></span>
        <span class="avatar-art-arm avatar-art-arm-left"></span>
        <span class="avatar-art-arm avatar-art-arm-right"></span>
        <span class="avatar-art-top"></span>
        <span class="avatar-art-bottom"></span>
        <span class="avatar-art-leg avatar-art-leg-left"></span>
        <span class="avatar-art-leg avatar-art-leg-right"></span>
        <span class="avatar-art-shoe avatar-art-shoe-left"></span>
        <span class="avatar-art-shoe avatar-art-shoe-right"></span>
        <span class="avatar-art-initial">${escapeHtml(avatarInitial(username))}</span>
        <span class="avatar-art-accessory"></span>
        <span class="avatar-art-accessory-detail avatar-art-accessory-detail-left"></span>
        <span class="avatar-art-accessory-detail avatar-art-accessory-detail-right"></span>
      </span>
      <span class="avatar-v2-shadow"></span>
      <span class="avatar-v2-wings"></span>
      <span class="avatar-v2-tail"></span>
      <span class="avatar-v2-arm avatar-v2-arm-left"></span>
      <span class="avatar-v2-arm avatar-v2-arm-right"></span>
      <span class="avatar-v2-body">
        <span class="avatar-v2-neck"></span>
        <span class="avatar-v2-bottom"></span>
        <span class="avatar-v2-leg avatar-v2-leg-left"></span>
        <span class="avatar-v2-leg avatar-v2-leg-right"></span>
        <span class="avatar-v2-shoe avatar-v2-shoe-left"></span>
        <span class="avatar-v2-shoe avatar-v2-shoe-right"></span>
        <span class="avatar-v2-ear avatar-v2-ear-left avatar-v2-ear-outer"></span>
        <span class="avatar-v2-ear avatar-v2-ear-right avatar-v2-ear-outer"></span>
        <span class="avatar-v2-hair avatar-v2-hair-outer"></span>
        <span class="avatar-v2-head">
          <span class="avatar-v2-ear avatar-v2-ear-left"></span>
          <span class="avatar-v2-ear avatar-v2-ear-right"></span>
          <span class="avatar-v2-hair"></span>
          <span class="avatar-v2-trunk"></span>
          <span class="avatar-v2-face-mark"></span>
          <span class="avatar-v2-eyes"></span>
          <span class="avatar-v2-nose"></span>
          <span class="avatar-v2-mouth"></span>
          <span class="avatar-v2-accessory"></span>
        </span>
        <span class="avatar-v2-accessory avatar-v2-accessory-outer"></span>
        <span class="avatar-v2-badge">${escapeHtml(avatarInitial(username))}</span>
      </span>
    </span>
  `;
}

// This function routes the current HTML page to its initializer.
function routePage() {
  const page = document.body.dataset.page;

  if (page === "login") initLogin();
  if (page === "signup") initSignup();
  if (page === "groups") initGroups();
  if (page === "create-group") initCreateGroup();
  if (page === "board") initBoard();
  if (page === "create-activity") initAdminPage();
  if (page === "trivia") initAdminPage(initTrivia);
  if (page === "missions") initAdminPage(initMissions);
  if (page === "practice") initPractice();
  if (page === "emergency") initEmergency();
  if (page === "game") initGame();
  if (page === "unity-avatar-editor") initUnityAvatarEditor();
  if (page === "summary") initSummary();
  if (page === "report") initAdminPage(initReport);
}

// This function wires the login form to the auth API.
function initLogin() {
  const form = document.querySelector("[data-login-form]");

  form?.addEventListener("submit", async event => {
    event.preventDefault();
    clearFormMessage(form);

    const formData = new FormData(form);
    const username = formData.get("username")?.toString() || "";
    const password = formData.get("password")?.toString() || "";

    try {
      setFormBusy(form, true);
      await loginWithUsername({ username, password });
      await loadSessionIntoState();
      saveState();
      window.location.href = "groups.html";
    } catch (error) {
      showFormError(form, readableAuthError(error));
    } finally {
      setFormBusy(form, false);
    }
  });
}

// This function wires the signup form and initial avatar choice to the auth API.
function initSignup() {
  const form = document.querySelector("[data-signup-form]");
  syncSignupAvatarDraftFromUrl();

  form?.addEventListener("submit", async event => {
    event.preventDefault();
    clearFormMessage(form);

    const formData = new FormData(form);
    const username = formData.get("username")?.toString() || "";
    const password = formData.get("password")?.toString() || "";
    const role = formData.get("role")?.toString() === "admin" ? "admin" : "user";
    const avatar = normalizeAvatar(sessionStorage.getItem(SIGNUP_AVATAR_KEY), username);

    try {
      setFormBusy(form, true);
      await signUpWithUsername({ avatar, username, password, role });
      await loadSessionIntoState();
      saveState();
      sessionStorage.removeItem(SIGNUP_AVATAR_KEY);
      showFormSuccess(form, "Saved successfully");
      window.setTimeout(() => {
        window.location.href = "groups.html";
      }, 900);
    } catch (error) {
      showFormError(form, readableAuthError(error));
      setFormBusy(form, false);
    }
  });
}

// This function saves the logged-in user profile in local state.
function setSessionUser(username, role, userId, avatar, alertLocation = null) {
  const cleanRole = role === "admin" ? "admin" : "user";

  state.user = {
    avatar: normalizeAvatar(avatar, username),
    userId,
    username,
    name: username,
    role: cleanRole,
    alertLocation,
    familyRoomId: state.activeGroupId || ""
  };

  state.groups = state.groups.map(group => ({ ...group, userRole: cleanRole }));
}

// This function loads the server session profile into local state.
async function loadSessionIntoState() {
  const profile = await getCurrentUserProfile();

  if (!profile) {
    return null;
  }

  setSessionUser(profile.username, profile.role, profile.id, profile.avatar, profile.alertLocation);
  state.groups = [];
  await refreshCurrentUserGroups(profile.role);
  return profile;
}

// This function refreshes the visible groups for the current user.
async function refreshCurrentUserGroups(role = state.user?.role || "user") {
  const groups = await getCurrentUserGroups();

  state.groups = groups
    .filter(Boolean)
    .map(group => ({
      id: group.id,
      joinCode: group.joinCode || "",
      members: Array.isArray(group.members)
        ? group.members.map(member => ({
          ...member,
          alertLocation: member.alertLocation || null,
          avatar: member.id === state.user?.userId
            ? normalizeAvatar(state.user.avatar, member.username)
            : normalizeAvatar(member.avatar, member.username)
        }))
        : [],
      name: group.name || "Untitled group",
      pendingRequests: Array.isArray(group.pendingRequests) ? group.pendingRequests : [],
      userRole: group.userRole || (role === "admin" ? "admin" : "user")
    }));

  if (!state.groups.length) {
    state.activeGroupId = "";
    state.familyName = "";
    if (state.user) state.user.familyRoomId = "";
    return;
  }

  if (!state.groups.some(group => group.id === state.activeGroupId)) {
    state.activeGroupId = state.groups[0].id;
  }

  const activeGroup = state.groups.find(group => group.id === state.activeGroupId) || state.groups[0];
  state.familyName = activeGroup.name;
  if (state.user) state.user.familyRoomId = activeGroup.id;
}

// This function disables or enables a form while a request is running.
function setFormBusy(form, isBusy) {
  const submitButton = form.querySelector("button[type='submit']");

  form.querySelectorAll("input, select, button").forEach(control => {
    control.disabled = isBusy;
  });

  if (submitButton) {
    submitButton.dataset.originalText = submitButton.dataset.originalText || submitButton.textContent;
    submitButton.textContent = isBusy ? "..." : submitButton.dataset.originalText;
  }
}

// This function shows an error message inside a form.
function showFormError(form, message) {
  showFormMessage(form, message, "auth-error");
}

// This function shows a success message inside a form.
function showFormSuccess(form, message) {
  showFormMessage(form, message, "auth-success");
}

// This function creates or updates a form message.
function showFormMessage(form, message, className) {
  let messageNode = form.querySelector("[data-form-message]");

  if (!messageNode) {
    messageNode = document.createElement("p");
    messageNode.dataset.formMessage = "";
    form.append(messageNode);
  }

  messageNode.className = className;
  messageNode.textContent = message;
}

// This function removes the current form message.
function clearFormMessage(form) {
  form.querySelector("[data-form-message]")?.remove();
}

// This function turns auth errors into text safe for display.
function readableAuthError(error) {
  return error?.message || "Authentication failed. Please try again.";
}

// This function protects admin-only pages before running their initializer.
async function initAdminPage(initializer) {
  const allowed = await requireAdminAccess();
  if (allowed && initializer) initializer();
}

// This function verifies that the logged-in user is an admin.
async function requireAdminAccess() {
  try {
    const profile = await loadSessionIntoState();

    if (!profile) {
      window.location.href = "index.html";
      return false;
    }

    saveState();

    if (profile.role !== "admin") {
      window.location.href = "groups.html";
      return false;
    }

    return true;
  } catch (error) {
    console.warn(readableAuthError(error));
    window.location.href = "index.html";
    return false;
  }
}

// This function renders the current user summary text.
function renderCurrentUserSummary(node) {
  if (!node || !state.user) return;
  node.textContent = `היי ${state.user.username || state.user.name}, כיף שחזרת!`;
}

// This function renders the current user's avatar preview.
function renderCurrentAvatar() {
  if (!state.user) return;

  const summaryMarkup = renderAvatarBadge(
    state.user.username || state.user.name,
    state.user.avatar,
    "profile-avatar avatar-unity-preview"
  );

  document.querySelectorAll("[data-current-avatar-summary]").forEach(preview => {
    preview.innerHTML = `
      <div class="avatar-edit-preview">
        ${summaryMarkup}
        <button class="avatar-edit-button" type="button" data-open-avatar-editor aria-label="Edit avatar"></button>
      </div>
    `;
  });
}

// This function wires the avatar launch button to the Unity WebGL editor.
function initUnityAvatarLaunch() {
  renderCurrentAvatar();

  document.querySelectorAll("[data-open-avatar-editor]").forEach(openButton => openButton.addEventListener("click", () => {
    window.location.href = "avatar-editor.html?return=groups.html";
  }));
}

// This function starts the embedded Unity WebGL avatar editor page.
async function initUnityAvatarEditor() {
  const status = document.querySelector("[data-unity-status]");
  let profile = null;

  try {
    profile = await loadSessionIntoState();
    if (profile) saveState();
  } catch (error) {
    console.warn(readableAuthError(error));
  }

  wireLogoutLink();
  if (status) {
    status.textContent = profile ? `Logged in as ${profile.username}` : "Unity avatar editor";
  }
  await loadUnityAvatarEditor(profile);
}

// This function reads Unity build URLs from the host element.
function getUnityHostConfig(host) {
  return {
    codeUrl: host.dataset.codeUrl,
    dataUrl: host.dataset.dataUrl,
    frameworkUrl: host.dataset.frameworkUrl,
    loaderUrl: host.dataset.loaderUrl,
    streamingAssetsUrl: host.dataset.streamingAssetsUrl
  };
}

// This function loads the Unity WebGL loader script.
function loadScript(src) {
  return new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.async = true;
    script.src = src;
    script.onload = resolve;
    script.onerror = () => reject(new Error("Unity WebGL build was not found"));
    document.head.append(script);
  });
}

// This function loads the Unity instance and connects it to the web profile.
async function loadUnityAvatarEditor(profile) {
  const host = document.querySelector("[data-unity-host]");
  const canvas = document.querySelector("[data-unity-canvas]");

  if (!host || !canvas) {
    return;
  }

  const config = getUnityHostConfig(host);

  try {
    await loadScript(config.loaderUrl);
  } catch (error) {
    showUnityBuildMissing(error);
    return;
  }

  try {
    const unityInstance = await window.createUnityInstance(canvas, {
      codeUrl: config.codeUrl,
      dataUrl: config.dataUrl,
      frameworkUrl: config.frameworkUrl,
      streamingAssetsUrl: config.streamingAssetsUrl
    }, updateUnityProgress);

    sendWebSessionToUnity(unityInstance, profile);
    showUnityStatus("Avatar editor ready", "good");
  } catch (error) {
    showUnityStartupError(error);
  }
}

// This function updates the visible Unity loading progress.
function updateUnityProgress(progress) {
  const progressBar = document.querySelector("[data-unity-progress] span");

  if (progressBar) {
    progressBar.style.width = `${Math.round(progress * 100)}%`;
  }
}

// This function shows status text above the Unity editor.
function showUnityStatus(message, tone = "") {
  const status = document.querySelector("[data-unity-status]");

  if (!status) {
    return;
  }

  status.className = `notice ${tone}`.trim();
  status.textContent = message;
}

// This function shows local build instructions when the Unity build is missing.
function showUnityBuildMissing(error) {
  const missing = document.querySelector("[data-unity-missing]");
  const host = document.querySelector("[data-unity-host]");

  host?.classList.add("unity-webgl-host-missing");
  missing?.classList.remove("hidden");
  showUnityStatus(error?.message || "Unity WebGL build was not found", "warn");
}

// This function reports Unity runtime startup errors without hiding them as missing files.
function showUnityStartupError(error) {
  const missing = document.querySelector("[data-unity-missing]");
  const host = document.querySelector("[data-unity-host]");

  host?.classList.remove("unity-webgl-host-missing");
  missing?.classList.add("hidden");
  showUnityStatus(error?.message || "Unity editor could not start", "warn");
}

// This function sends the current web profile into the running Unity instance.
function sendWebSessionToUnity(unityInstance, profile) {
  if (!unityInstance?.SendMessage) {
    return;
  }

  const payload = {
    gatewayBaseUrl: window.location.origin,
    returnUrl: safeUnityReturnUrl(profile),
    draftAvatar: sessionStorage.getItem(SIGNUP_AVATAR_KEY) || "",
    profile
  };

  window.setTimeout(() => {
    unityInstance.SendMessage("SaferTogether Auth Controller", "ApplyWebSessionJson", JSON.stringify(payload));
  }, 250);
}

// This function wires logout links that appear outside the groups screen.
function wireLogoutLink() {
  document.querySelector("[data-logout]")?.addEventListener("click", async event => {
    event.preventDefault();

    try {
      await logout();
    } catch (error) {
      console.warn(readableAuthError(error));
    }

    state = initialState();
    sessionStorage.removeItem(STORAGE_KEY);
    window.location.href = "index.html";
  });
}

// This function wires the groups screen.
async function initGroups() {
  const currentUser = document.querySelector("[data-current-user]");
  const createButton = document.querySelector("[data-admin-create-group]");
  const joinForm = document.querySelector("[data-join-code-form]");

  try {
    const profile = await loadSessionIntoState();
    if (!profile) {
      window.location.href = "index.html";
      return;
    }
    saveState();
  } catch (error) {
    if (currentUser) currentUser.textContent = readableAuthError(error);
    window.location.href = "index.html";
    return;
  }

  const user = state.user;
  if (!user) {
    window.location.href = "index.html";
    return;
  }

  renderCurrentUserSummary(currentUser);

  renderGroupsList();
  initUnityAvatarLaunch();

  const memberPanel = document.querySelector("[data-member-group-panel]");
  const leaveConfirmDialog = document.querySelector("[data-leave-confirm-dialog]");
  const leaveGroupBtn = document.querySelector("[data-leave-group-btn]");
  const leaveConfirmYes = document.querySelector("[data-leave-confirm-yes]");
  const leaveConfirmNo = document.querySelector("[data-leave-confirm-no]");
  const leaveGroupError = document.querySelector("[data-leave-group-error]");
  const memberGroupDisplay = document.querySelector("[data-member-group-display]");

  const isMember = user.role !== "admin" && state.groups.length > 0;

  createButton?.classList.toggle("hidden", user.role !== "admin");
  joinForm?.classList.toggle("hidden", user.role === "admin" || isMember);
  memberPanel?.classList.toggle("hidden", !isMember);
  document.querySelector("[data-groups-list]")?.classList.toggle("hidden", isMember);

  if (isMember && memberGroupDisplay) {
    const activeGroup = state.groups.find(g => g.id === state.activeGroupId) || state.groups[0];
    memberGroupDisplay.innerHTML = `
      <article class="group-entry">
        <div class="group-card">
          <button class="group-card-main" type="button" data-open-group="${activeGroup.id}">
            <span class="group-icon">.</span>
            <span>
              <strong>${escapeHtml(activeGroup.name)}</strong>
              <small>Member</small>
            </span>
          </button>
        </div>
      </article>`;
    memberGroupDisplay.querySelector("[data-open-group]")?.addEventListener("click", () => openGroup(activeGroup.id));
  }

  leaveGroupBtn?.addEventListener("click", () => {
    leaveConfirmDialog?.classList.remove("hidden");
  });

  leaveConfirmNo?.addEventListener("click", () => {
    leaveConfirmDialog?.classList.add("hidden");
  });

  leaveConfirmYes?.addEventListener("click", async () => {
    const activeGroup = state.groups.find(g => g.id === state.activeGroupId) || state.groups[0];
    if (!activeGroup) return;
    if (leaveGroupError) leaveGroupError.classList.add("hidden");
    if (leaveConfirmYes) leaveConfirmYes.disabled = true;
    try {
      await leaveGroup(activeGroup.id);
      await refreshCurrentUserGroups("user");
      saveState();
      window.location.reload();
    } catch (error) {
      if (leaveGroupError) {
        leaveGroupError.textContent = readableAuthError(error);
        leaveGroupError.classList.remove("hidden");
      }
      if (leaveConfirmYes) leaveConfirmYes.disabled = false;
      leaveConfirmDialog?.classList.add("hidden");
    }
  });

  joinForm?.addEventListener("submit", async event => {
    event.preventDefault();
    clearFormMessage(joinForm);

    const data = new FormData(joinForm);
    const code = data.get("code")?.toString().trim() || "";

    try {
      setFormBusy(joinForm, true);
      await requestJoinByCode({ code });
      joinForm.reset();
      showFormSuccess(joinForm, "Request sent");
    } catch (error) {
      showFormError(joinForm, readableAuthError(error));
    } finally {
      setFormBusy(joinForm, false);
    }
  });

  document.querySelector("[data-logout]")?.addEventListener("click", async event => {
    event.preventDefault();
    try {
      await logout();
    } catch (error) {
      console.warn(readableAuthError(error));
    }
    state = initialState();
    sessionStorage.removeItem(STORAGE_KEY);
    window.location.href = "index.html";
  });
}

// This function opens the selected group.
function openGroup(groupId) {
  const group = state.groups.find(item => item.id === groupId);
  if (!group) return;

  state.activeGroupId = group.id;
  state.familyName = group.name;
  if (state.user) {
    state.user.familyRoomId = group.id;
  }

  saveState();
  window.location.href = "board.html";
}

// This function shows the groups on the groups page.
function renderGroupsList() {
  const container = document.querySelector("[data-groups-list]");
  if (!container) return;

  if (!state.groups.length) {
    container.innerHTML = `<p class="notice">No groups yet.</p>`;
    return;
  }

  container.innerHTML = state.groups.map(group => `
    <article class="group-entry">
      <div class="group-card">
        ${group.userRole === "admin" ? `
          <button class="group-delete-button" type="button" data-delete-group="${group.id}" aria-label="Delete group" title="Delete group">&#128465;</button>
        ` : ""}
        <button class="group-card-main" type="button" data-open-group="${group.id}">
          <span class="group-icon">${group.userRole === "admin" ? "*" : "."}</span>
          <span>
            <strong>${escapeHtml(group.name)}</strong>
            <small>${group.userRole === "admin" ? "Admin" : "Member"}</small>
          </span>
        </button>
      </div>
      ${group.userRole === "admin" ? `
        <div class="group-extra">
          <p class="notice">Team code: <strong class="join-code-value">${escapeHtml(group.joinCode)}</strong></p>
        </div>
      ` : ""}
    </article>
  `).join("");

  container.querySelectorAll("[data-open-group]").forEach(button => {
    button.addEventListener("click", () => openGroup(button.dataset.openGroup));
  });

  container.querySelectorAll("[data-delete-group]").forEach(button => {
    button.addEventListener("click", async () => {
      try {
        button.disabled = true;
        await deleteOwnedGroup(button.dataset.deleteGroup);
        await refreshCurrentUserGroups("admin");
        renderGroupsList();
      } catch (error) {
        const currentUser = document.querySelector("[data-current-user]");
        if (currentUser) {
          currentUser.textContent = readableAuthError(error);
        }
      }
    });
  });
}

// This function creates a new group for the admin.
async function initCreateGroup() {
  const form = document.querySelector("[data-create-group-form]");
  if (!form) return;

  try {
    await loadSessionIntoState();
    saveState();
  } catch (error) {
    showFormError(form, readableAuthError(error));
  }

  if (state.user?.role !== "admin") {
    window.location.href = "groups.html";
    return;
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    clearFormMessage(form);

    const data = new FormData(form);
    const name = data.get("groupName")?.toString().trim() || "New group";

    try {
      setFormBusy(form, true);
      const group = await createGroupForCurrentUser({ name });
      await refreshCurrentUserGroups("admin");
      state.activeGroupId = group.id;
      state.familyName = group.name;
      if (state.user) {
        state.user.familyRoomId = group.id;
        state.user.role = "admin";
      }
      saveState();
      window.location.href = "groups.html";
    } catch (error) {
      showFormError(form, readableAuthError(error));
    } finally {
      setFormBusy(form, false);
    }
  });
}

// This function wires the active group board screen.
async function initBoard() {
  try {
    const profile = await loadSessionIntoState();
    if (!profile) {
      window.location.href = "index.html";
      return;
    }
    saveState();
  } catch (error) {
    console.warn(readableAuthError(error));
    window.location.href = "index.html";
    return;
  }

  if (!state.groups.length) {
    window.location.href = "groups.html";
    return;
  }

  const group = getActiveGroup();
  setText("[data-active-group-name]", group.name);
  setText("[data-active-group-id]", group.id);
  renderBoardMembers(group);
  renderBoardPendingRequests(group);
  initAlertLocationControls();
  renderOrefStatus();
  refreshOrefStatus();
  startOrefStatusPolling();

  document.querySelectorAll("[data-admin-only]").forEach(node => {
    node.classList.toggle("hidden", !isCurrentUserAdminForActiveGroup());
  });

  const isAdmin = isCurrentUserAdminForActiveGroup();

  if (isAdmin) {
    document.querySelector("[data-start-drill]")?.addEventListener("click", async () => {
      const group = getActiveGroup();
      if (!group) return;
      try {
        await startDrill(group.id);
        window.location.href = "practice.html";
      } catch (error) {
        console.error("startDrill failed:", error);
        alert("שגיאה בהפעלת התרגול");
      }
    });
  } else {
    startDrillPolling();
    startMembersPolling();
  }

  document.querySelector("[data-trigger-emergency]")?.addEventListener("click", () => {
    startEmergency();
    window.location.href = "emergency.html";
  });

  document.querySelector("[data-open-oref-emergency]")?.addEventListener("click", () => {
    startEmergency(state.orefStatus);
    window.location.href = "emergency.html";
  });

  document.querySelector("[data-rename-group]")?.addEventListener("click", async () => {
    const group = getActiveGroup();
    const newName = prompt("שם חדש לקבוצה:", group.name);
    if (!newName || newName.trim() === group.name) return;

    try {
      await renameGroup(group.id, newName.trim());
      group.name = newName.trim();
      saveState();
      setText("[data-active-group-name]", group.name);
    } catch {
      alert("שגיאה בשינוי שם הקבוצה");
    }
  });

  if (isCurrentUserAdminForActiveGroup()) {
    startBoardRequestsPolling();
  }
}

// This function returns the currently selected group.
function getActiveGroup() {
  return state.groups.find(group => group.id === state.activeGroupId) || state.groups[0] || null;
}

// This function checks admin rights for the currently selected group.
function isCurrentUserAdminForActiveGroup() {
  const group = getActiveGroup();
  return Boolean(group) && state.user?.role === "admin" && group.userRole === "admin";
}

// This function shows the members of the active group.
function renderBoardMembers(group) {
  const container = document.querySelector("[data-group-members]");
  if (!container || !group) return;

  if (!group.members?.length) {
    container.innerHTML = `<p class="notice">No members yet.</p>`;
    return;
  }

  container.innerHTML = group.members.map(member => {
    const isCurrentMember = state.user?.userId && String(member.id) === String(state.user.userId);
    const liveStatus = getOrefMemberStatus(member.id);
    const location = liveStatus?.alertLocation || member.alertLocation;
    const statusClass = orefMemberStatusClass(liveStatus, location, true);
    const statusLabel = orefMemberStatusLabel(liveStatus, location);

    return `
      <article class="member-card ${isCurrentMember ? "member-card-current" : ""} ${liveStatus?.status === "alert" ? "member-card-alert" : ""}">
        ${renderAvatarBadge(member.username, member.avatar, "member-avatar avatar-unity-preview")}
        <div class="member-main">
          <p class="member-name">
            ${escapeHtml(member.username)}
            ${isCurrentMember ? `<span class="member-me-pill">Me</span>` : ""}
          </p>
        </div>
        <span class="status-pill ${statusClass}">${escapeHtml(statusLabel)}</span>
      </article>
    `;
  }).join("");
}

// This function returns one member's live HFC status.
function getOrefMemberStatus(memberId) {
  return (state.orefStatus?.members || []).find(member => member.memberId === memberId) || null;
}

// This function maps a live HFC member status to an existing visual state.
function orefMemberStatusClass(memberStatus, location = null, showLocation = false) {
  if (memberStatus?.status === "alert") return "at_risk";
  if (showLocation && (memberStatus?.status === "clear" || location?.areaName)) return "located";
  if (memberStatus?.status === "clear") return "safe";
  if (location?.areaName) return "located";
  return "offline";
}

// This function maps a live HFC member status to display text.
function orefMemberStatusLabel(memberStatus, location = null) {
  if (memberStatus?.status === "alert") return "ALERT";
  const areaName = alertLocationLabel(memberStatus?.alertLocation || location);
  if (areaName) return areaName;
  return "No area";
}

// This function returns the most readable saved Home Front Command area name.
function alertLocationLabel(location) {
  return location?.areaNameHebrew || location?.areaName || "";
}

// This function shows pending join requests for the active group on the board.
function renderBoardPendingRequests(group) {
  const container = document.querySelector("[data-pending-requests-list]");
  if (!container || !group) return;

  if (!group.pendingRequests?.length) {
    container.innerHTML = `<p class="notice">אין בקשות ממתינות.</p>`;
    return;
  }

  container.innerHTML = `
    <div class="join-request-list">
      ${group.pendingRequests.map(request => `
        <div class="join-request-card">
          <p>${escapeHtml(request.username || "משתמש")} מבקש להצטרף לקבוצה.</p>
          <div class="join-request-actions">
            <button class="btn btn-primary" type="button"
              data-group-id="${group.id}"
              data-request-id="${request.id}"
              data-board-review="approved">אשר</button>
            <button class="btn btn-secondary" type="button"
              data-group-id="${group.id}"
              data-request-id="${request.id}"
              data-board-review="declined">דחה</button>
          </div>
        </div>
      `).join("")}
    </div>
  `;

  container.querySelectorAll("[data-board-review]").forEach(button => {
    button.addEventListener("click", async () => {
      try {
        button.disabled = true;
        button.closest(".join-request-actions")
          ?.querySelectorAll("button")
          .forEach(btn => { btn.disabled = true; });

        await reviewJoinRequest({
          groupId: button.dataset.groupId,
          requestId: button.dataset.requestId,
          status: button.dataset.boardReview
        });

        await refreshCurrentUserGroups("admin");
        saveState();
        renderBoardPendingRequests(getActiveGroup());
        renderBoardMembers(getActiveGroup());
      } catch (error) {
        alert(readableAuthError(error));
        button.disabled = false;
        button.closest(".join-request-actions")
          ?.querySelectorAll("button")
          .forEach(btn => { btn.disabled = false; });
      }
    });
  });
}

// This function polls the server every 15 seconds to pick up new join requests and member changes.
function startBoardRequestsPolling() {
  const INTERVAL_MS = 15000;
  let previousCount = getActiveGroup()?.pendingRequests?.length ?? 0;

  const intervalId = setInterval(async () => {
    if (document.hidden) return;

    try {
      await refreshCurrentUserGroups("admin");
      saveState();

      const group = getActiveGroup();
      const newCount = group?.pendingRequests?.length ?? 0;

      if (newCount !== previousCount) {
        previousCount = newCount;
        renderBoardPendingRequests(group);
      }

      renderBoardMembers(group);
    } catch {
      // This retry loop lets the next poll recover from a transient request error.
    }
  }, INTERVAL_MS);

  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

// This function polls the server every 15 seconds so regular members see up-to-date member locations and avatars.
function startMembersPolling() {
  const INTERVAL_MS = 15000;

  const intervalId = setInterval(async () => {
    if (document.hidden) return;
    try {
      await refreshCurrentUserGroups("user");
      saveState();
      renderBoardMembers(getActiveGroup());
    } catch {
      // silently ignore polling errors
    }
  }, INTERVAL_MS);

  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

// This function polls the server every 5 seconds to detect when an admin starts a drill.
// It snapshots the drill state on the first poll so it only alarms on transitions
// (inactive → active), not on a drill that was already running when the user loaded.
function startDrillPolling() {
  const INTERVAL_MS = 5000;
  let initialized = false;
  let wasActive = false;

  async function poll() {
    if (document.hidden) return;
    try {
      const groups = await getCurrentUserGroups();
      const activeGroupId = getActiveGroup()?.id;
      const group = (groups || []).find(g => g.id === activeGroupId);
      const isActive = group?.drillActive ?? false;

      if (!initialized) {
        initialized = true;
        wasActive = isActive;
        return;
      }

      if (isActive && !wasActive) {
        clearInterval(intervalId);
        showDrillOverlay();
      }
      wasActive = isActive;
    } catch {
      // silently ignore polling errors
    }
  }

  poll();
  const intervalId = setInterval(poll, INTERVAL_MS);
  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

// This function shows the drill overlay and redirects to practice after a short delay.
function showDrillOverlay() {
  const overlay = document.querySelector("[data-drill-overlay]");
  if (overlay) {
    overlay.classList.remove("hidden");
  }
  setTimeout(() => {
    window.location.href = "practice.html";
  }, 3000);
}

// This function starts automatic GPS alert-area matching for the current user.
function initAlertLocationControls() {
  if (!state.user) {
    return;
  }

  void enableGpsAlertLocation();
}

// This function enables automatic GPS updates for the user's alert area.
async function enableGpsAlertLocation() {
  if (!navigator.geolocation) {
    renderGpsLocationStatus("GPS is not available in this browser", "warn");
    return;
  }

  try {
    renderGpsLocationStatus("Getting GPS location...");
    const position = await getCurrentGpsPosition();
    await saveGpsAlertLocation(position, { force: true });
    startGpsAlertLocationWatch();
  } catch (error) {
    renderGpsLocationStatus(readableGpsError(error), "warn");
  }
}

// This function gets the browser's current GPS position as a promise.
function getCurrentGpsPosition() {
  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(resolve, reject, {
      enableHighAccuracy: true,
      maximumAge: 30000,
      timeout: 12000
    });
  });
}

// This function starts watching GPS and updates the saved HFC area as the user moves.
function startGpsAlertLocationWatch() {
  if (!navigator.geolocation || orefGpsWatchId !== null) {
    return;
  }

  orefGpsWatchId = navigator.geolocation.watchPosition(
    position => {
      saveGpsAlertLocation(position).catch(error => {
        renderGpsLocationStatus(readableGpsError(error), "warn");
      });
    },
    error => {
      if (error?.code === 1) {
        stopGpsAlertLocationWatch();
      }

      renderGpsLocationStatus(readableGpsError(error), "warn");
    },
    {
      enableHighAccuracy: true,
      maximumAge: 45000,
      timeout: 15000
    }
  );
}

// This function stops the current GPS watch when permission is revoked or the page closes.
function stopGpsAlertLocationWatch() {
  if (!navigator.geolocation || orefGpsWatchId === null) {
    return;
  }

  navigator.geolocation.clearWatch(orefGpsWatchId);
  orefGpsWatchId = null;
}

// This function resolves one GPS position to an HFC area and saves it.
async function saveGpsAlertLocation(position, { force = false } = {}) {
  const coords = position.coords;
  const now = Date.now();
  const nextLocation = {
    latitude: coords.latitude,
    longitude: coords.longitude
  };

  if (!force && lastGpsLocationSave) {
    const elapsed = now - lastGpsLocationSave.at;
    const distance = distanceMeters(lastGpsLocationSave.coords, nextLocation);

    // Avoid hammering the gateway while still updating quickly when the user moves across areas.
    if (elapsed < GPS_LOCATION_SAVE_INTERVAL_MS && distance < GPS_LOCATION_DISTANCE_THRESHOLD_METERS) {
      return state.user?.alertLocation || null;
    }
  }

  renderGpsLocationStatus("Matching GPS to alert area...");
  const alertLocation = await saveCurrentUserAlertLocation({
    latitude: coords.latitude,
    longitude: coords.longitude
  });

  lastGpsLocationSave = {
    at: now,
    coords: nextLocation
  };
  state.user.alertLocation = alertLocation;
  updateActiveMemberAlertLocation(alertLocation);
  await refreshCurrentUserGroups(state.user.role);
  saveState();

  renderGpsLocationStatus(`GPS area: ${alertLocationLabel(alertLocation)}`, "good");
  renderBoardMembers(getActiveGroup());
  await refreshOrefStatus();
  return alertLocation;
}

// This function renders short GPS state near the location controls.
function renderGpsLocationStatus(message, kind = "") {
  const node = document.querySelector("[data-alert-location-status]");
  if (!node) return;

  if (!message) {
    node.classList.add("hidden");
    node.textContent = "";
    return;
  }

  node.textContent = message;
  node.className = `notice ${kind}`.trim();
}

// This function turns browser geolocation errors into short display text.
function readableGpsError(error) {
  if (error?.code === 1) return "GPS permission was denied";
  if (error?.code === 2) return "GPS location is unavailable";
  if (error?.code === 3) return "GPS request timed out";
  return error?.message || "Could not use GPS";
}

// This function calculates distance between two GPS coordinates.
function distanceMeters(a, b) {
  const radius = 6371000;
  const lat1 = toRadians(a.latitude);
  const lat2 = toRadians(b.latitude);
  const deltaLat = toRadians(b.latitude - a.latitude);
  const deltaLon = toRadians(b.longitude - a.longitude);
  const h = Math.sin(deltaLat / 2) ** 2 +
    Math.cos(lat1) * Math.cos(lat2) * (Math.sin(deltaLon / 2) ** 2);

  return 2 * radius * Math.atan2(Math.sqrt(h), Math.sqrt(1 - h));
}

// This function converts degrees to radians.
function toRadians(value) {
  return value * Math.PI / 180;
}

// This function updates the active group member after the user saves a location.
function updateActiveMemberAlertLocation(alertLocation) {
  const group = getActiveGroup();
  if (!group || !state.user?.userId) return;

  group.members = (group.members || []).map(member => (
    member.id === state.user.userId ? { ...member, alertLocation } : member
  ));
}

// This function starts live HFC polling while the board or emergency screen is open.
function startOrefStatusPolling() {
  const group = getActiveGroup();
  if (!group) return;

  const intervalId = window.setInterval(() => {
    if (!document.hidden) {
      refreshOrefStatus();
    }
  }, OREF_POLL_INTERVAL_MS);

  document.addEventListener("visibilitychange", () => {
    if (!document.hidden) {
      refreshOrefStatus();
    }
  });
  window.addEventListener("beforeunload", () => window.clearInterval(intervalId));
}

// This function refreshes live HFC status for the active group.
async function refreshOrefStatus() {
  const group = getActiveGroup();
  if (!group) return null;

  try {
    const status = await getGroupOrefStatus(group.id);
    state.orefStatus = status;

    if (state.emergency?.active && state.emergency.trigger === "pikud_haoref") {
      state.emergency.orefStatus = status;
    }

    saveState();
    renderOrefStatus(status);

    if (document.body.dataset.page === "board") {
      renderBoardMembers(getActiveGroup());
    }

    if (document.body.dataset.page === "emergency") {
      renderEmergency();
    }

    return status;
  } catch (error) {
    renderOrefStatus(null, error);
    return null;
  }
}

// This function renders live HFC state in the board and emergency screens.
function renderOrefStatus(status = state.orefStatus, error = null) {
  const summary = document.querySelector("[data-oref-alert-summary]");
  const refreshState = document.querySelector("[data-oref-refresh-state]");
  const emergencyButton = document.querySelector("[data-open-oref-emergency]");

  if (refreshState) {
    refreshState.textContent = status?.fetchedAt
      ? `Checked ${new Date(status.fetchedAt).toLocaleTimeString()}`
      : "Connecting";
  }

  if (summary) {
    if (error) {
      summary.textContent = error.message || "Could not check Pikud HaOref right now. Keep using official alerts.";
      summary.className = "notice warn";
    } else if (status?.hasGroupAlert) {
      const names = status.members
        .filter(member => member.status === "alert")
        .map(member => member.username)
        .join(", ");
      const areas = status.affectedAreas.slice(0, 6).join(", ");
      summary.textContent = `Real Home Front Command alert for ${names || "this group"}. Affected areas: ${areas || "see official alert"}.`;
      summary.className = "notice danger";
    } else if (status?.hasActiveAlert) {
      summary.textContent = `There is an active Home Front Command alert outside this group's saved areas. Affected areas: ${status.affectedAreas.slice(0, 6).join(", ")}.`;
      summary.className = "notice warn";
    } else if (!state.user?.alertLocation) {
      summary.textContent = "Set your alert area so real Home Front Command alarms can be matched to you.";
      summary.className = "notice warn";
    } else {
      summary.textContent = "No active Home Front Command alert for saved group areas.";
      summary.className = "notice good";
    }
  }

  emergencyButton?.classList.toggle("hidden", !status?.hasGroupAlert);
  renderEmergencyOrefSummary(status);
}

// This function renders the real-alert summary on the emergency screen.
function renderEmergencyOrefSummary(status = state.emergency?.orefStatus || state.orefStatus) {
  const node = document.querySelector("[data-oref-emergency-summary]");
  if (!node) return;

  if (!status?.hasGroupAlert) {
    node.classList.add("hidden");
    node.textContent = "";
    return;
  }

  const affectedMembers = status.members
    .filter(member => member.status === "alert")
    .map(member => member.username)
    .join(", ");

  node.textContent = `Pikud HaOref real alert: ${affectedMembers || "group member"} must enter protected space now.`;
  node.classList.remove("hidden");
}

// This function wires the trivia question builder.
function initTrivia() {
  renderQuestionList();

  document.querySelector("[data-trivia-form]")?.addEventListener("submit", event => {
    event.preventDefault();
    const form = event.currentTarget;
    const question = {
      id: `q${Date.now()}`,
      question: form.question.value.trim(),
      answers: [
        form.answerA.value.trim(),
        form.answerB.value.trim(),
        form.answerC.value.trim(),
        form.answerD.value.trim()
      ],
      correctAnswerIndex: Number(form.correctAnswer.value)
    };

    if (!question.question || question.answers.some(answer => !answer)) return;
    state.questions.push(question);
    saveState();
    renderQuestionList();
  });

  document.querySelector("[data-save-questions]")?.addEventListener("click", event => {
    event.currentTarget.textContent = "×”×©××œ×•×Ÿ × ×©×ž×¨ ×ž×§×•×ž×™×ª";
  });
}

// This function wires the mission builder.
function initMissions() {
  renderMissionList();

  document.querySelector("[data-mission-form]")?.addEventListener("submit", event => {
    event.preventDefault();
    const form = event.currentTarget;
    const mission = {
      id: `m${Date.now()}`,
      title: form.title.value.trim(),
      description: form.description.value.trim()
    };

    if (!mission.title || !mission.description) return;
    state.missions.push(mission);
    saveState();
    renderMissionList();
  });

  document.querySelector("[data-save-missions]")?.addEventListener("click", event => {
    event.currentTarget.textContent = "×”×ž×©×™×ž×•×ª × ×©×ž×¨×• ×ž×§×•×ž×™×ª";
  });
}

// This function wires the practice flow.
function initPractice() {
  const isAdmin = isCurrentUserAdminForActiveGroup();

  if (isAdmin) {
    initAdminDrillMonitor();
  } else {
    initMemberPractice();
  }
}

// This function runs the admin drill monitor view on practice.html.
function initAdminDrillMonitor() {
  const monitor = document.querySelector("[data-drill-monitor]");
  if (!monitor) return;

  monitor.classList.remove("hidden");

  const group = getActiveGroup();
  if (!group) return;

  renderDrillMembers(group.members || [], []);

  let drillPollInterval = setInterval(async () => {
    if (document.hidden) return;
    try {
      const safeUsers = await fetchDrillStatus(group.id);
      renderDrillMembers(group.members || [], safeUsers);
    } catch {
      // silently ignore polling errors
    }
  }, 5000);

  document.querySelector("[data-end-drill]")?.addEventListener("click", async () => {
    clearInterval(drillPollInterval);
    try {
      await endDrill(group.id);
    } catch {
      // best effort
    }
    window.location.href = "board.html";
  });

  window.addEventListener("beforeunload", () => clearInterval(drillPollInterval));
}

// This function renders the drill member grid with safe/pending status.
function renderDrillMembers(members, safeUsers) {
  const container = document.querySelector("[data-drill-members]");
  if (!container) return;

  if (!members.length) {
    container.innerHTML = `<p class="notice">אין חברים בקבוצה.</p>`;
    return;
  }

  container.innerHTML = members.map(member => {
    const isSafe = safeUsers.includes(member.id);
    return `
      <article class="member-card">
        ${renderAvatarBadge(member.username, member.avatar, `member-avatar ${isSafe ? "" : "avatar-grayscale"}`)}
        <p class="member-name">${escapeHtml(member.username)}</p>
        <span class="status-pill ${isSafe ? "safe" : "offline"}">${isSafe ? "מוגן" : "ממתין"}</span>
      </article>
    `;
  }).join("");
}

// This function runs the regular member practice flow.
function initMemberPractice() {
  const intro = document.querySelector("[data-practice-intro]");
  const active = document.querySelector("[data-practice-active]");
  const summary = document.querySelector("[data-practice-summary]");

  renderPracticeQuestion();

  document.querySelector("[data-start-practice]")?.addEventListener("click", () => {
    state.practiceSession = {
      startedAt: Date.now(),
      safeAt: null,
      answerStartedAt: Date.now(),
      answers: [],
      taps: 0,
      movementLevel: round(0.18 + Math.random() * 0.08)
    };
    saveState();
    intro?.classList.add("hidden");
    active?.classList.remove("hidden");
    summary?.classList.add("hidden");
  });

  document.querySelector("[data-practice-safe]")?.addEventListener("click", async event => {
    if (!state.practiceSession) return;
    state.practiceSession.safeAt = Date.now();
    state.practiceSession.taps += 1;
    saveState();
    event.currentTarget.textContent = "אישור מוגן נשמר";
    event.currentTarget.disabled = true;

    const group = getActiveGroup();
    if (group) {
      try {
        await markSafe(group.id);
      } catch {
        // best effort — local state already saved
      }
    }
  });

  document.querySelector("[data-complete-practice]")?.addEventListener("click", () => {
    completePractice();
  });
}

// This function wires the emergency check-in flow.
async function initEmergency() {
  try {
    await loadSessionIntoState();
    saveState();
  } catch {
    // Emergency mode can still show the locally stored event if the session refresh fails.
  }

  if (!state.emergency?.active) {
    startEmergency(state.orefStatus);
  }

  renderOrefStatus();
  renderEmergency();
  refreshOrefStatus();
  startOrefStatusPolling();

  document.querySelector("[data-emergency-safe]")?.addEventListener("click", () => {
    if (allMembersSafe()) {
      window.location.href = "game.html";
      return;
    }

    markMemberSafe(currentFamilyMemberId());
    state.emergency.telemetry.safeClickTime = secondsSince(state.emergency.startedAt);
    state.emergency.telemetry.tapCount += 1;
    saveState();
    renderEmergency();

    if (state.emergency.trigger !== "pikud_haoref") {
      simulateFamilyCheckIns();
    }
  });
}

// This function starts the unlocked activity game.
function initGame() {
  renderGame();
}

// This function renders the emergency summary screen.
function initSummary() {
  const container = document.querySelector("[data-summary-list]");
  if (!container) return;

  container.innerHTML = state.familyMembers.map((member, index) => {
    const report = buildMemberReport(member, index);
    return `
      <article class="member-card">
        <span class="status-dot ${member.status}"></span>
        <div class="member-main">
          <p class="member-name">${escapeHtml(member.name)}</p>
          <p class="member-role">×–×ž×Ÿ ××™×©×•×¨: ${report.checkInTime}</p>
          <p class="member-role">×¤×¢×™×œ×•×ª: ${report.participation}</p>
          <p class="member-role">× ×›×•×Ÿ: ${report.correct} | ×˜×¢×•×™×•×ª: ${report.mistakes}</p>
        </div>
        <span class="stress-level ${stressClass(report.stressLevel)}">${stressLabel(report.stressLevel)}</span>
      </article>
    `;
  }).join("");
}

// This function wires the report member selector.
function initReport() {
  const select = document.querySelector("[data-report-member]");
  if (!select) return;

  select.innerHTML = state.familyMembers.map(member => (
    `<option value="${member.id}">${escapeHtml(member.name)}</option>`
  )).join("");

  select.addEventListener("change", () => renderReport(select.value));
  renderReport(select.value || state.familyMembers[0]?.id);
}

// This function renders the local family member cards.
function renderFamilyList(container) {
  if (!container) return;

  container.innerHTML = state.familyMembers.map(member => `
    <article class="member-card">
      ${renderAvatarBadge(member.name, member.avatar, `avatar-badge ${member.status}`)}
      <div class="member-main">
        <p class="member-name">${escapeHtml(member.name)}</p>
        <p class="member-role">${escapeHtml(member.alertLocation?.areaName || member.role)}</p>
      </div>
      <span class="status-pill ${member.status}">${statusLabel(member.status)}</span>
    </article>
  `).join("");
}

// This function renders the saved trivia questions.
function renderQuestionList() {
  const container = document.querySelector("[data-question-list]");
  if (!container) return;

  container.innerHTML = state.questions.map((question, index) => `
    <article class="added-item">
      <strong>${index + 1}. ${escapeHtml(question.question)}</strong>
      <span>${escapeHtml(question.answers[question.correctAnswerIndex])} ×ž×¡×•×ž× ×ª ×›×ª×©×•×‘×” × ×›×•× ×”.</span>
    </article>
  `).join("");
}

// This function renders the saved missions.
function renderMissionList() {
  const container = document.querySelector("[data-mission-list]");
  if (!container) return;

  container.innerHTML = state.missions.map((mission, index) => `
    <article class="added-item">
      <strong>${index + 1}. ${escapeHtml(mission.title)}</strong>
      <span>${escapeHtml(mission.description)}</span>
    </article>
  `).join("");
}

// This function renders the active practice question.
function renderPracticeQuestion() {
  const container = document.querySelector("[data-practice-question]");
  const question = state.questions[0] || DEFAULT_QUESTIONS[0];
  if (!container) return;

  container.innerHTML = `
    <h2>×©××œ×ª ×ª×¨×’×•×œ</h2>
    <p class="subtitle">${escapeHtml(question.question)}</p>
    <div class="answer-grid">
      ${question.answers.map((answer, index) => `
        <button class="answer-btn" type="button" data-practice-answer="${index}">${escapeHtml(answer)}</button>
      `).join("")}
    </div>
    <p class="notice hidden" data-practice-feedback></p>
  `;

  container.querySelectorAll("[data-practice-answer]").forEach(button => {
    button.addEventListener("click", () => {
      if (!state.practiceSession) return;
      const answerIndex = Number(button.dataset.practiceAnswer);
      const correct = answerIndex === question.correctAnswerIndex;
      const timeToAnswer = secondsSince(state.practiceSession.answerStartedAt);
      state.practiceSession.answers.push({ timeToAnswer, correct });
      state.practiceSession.taps += 1;
      saveState();

      button.classList.add(correct ? "correct" : "wrong");
      container.querySelectorAll("[data-practice-answer]").forEach(item => item.disabled = true);
      const feedback = container.querySelector("[data-practice-feedback]");
      feedback.textContent = correct ? "×›×œ ×”×›×‘×•×“. ×ª×©×•×‘×” × ×›×•× ×”." : "× ×™×¡×™×•×Ÿ ×˜×•×‘. × ×ª×¨×’×œ ××ª ×–×” ×©×•×‘.";
      feedback.classList.remove("hidden");
    });
  });
}

// This function saves the practice baseline and shows its summary.
function completePractice() {
  const session = state.practiceSession || {
    startedAt: Date.now() - 4000,
    answers: [],
    taps: 1,
    movementLevel: 0.2
  };
  const answers = session.answers.length ? session.answers : [{ timeToAnswer: 2.1, correct: true }];
  const mistakes = answers.filter(answer => !answer.correct).length;
  const duration = Math.max(secondsSince(session.startedAt), 1);

  state.baseline = {
    userId: state.user?.userId || null,
    averageAnswerTime: round(average(answers.map(answer => answer.timeToAnswer))),
    mistakeRate: round(mistakes / answers.length),
    averageTapRate: round((session.taps || 1) / duration),
    averageMovementLevel: session.movementLevel || 0.2
  };
  saveState();

  document.querySelector("[data-practice-active]")?.classList.add("hidden");
  const summary = document.querySelector("[data-practice-summary]");
  if (!summary) return;

  summary.innerHTML = `
    <h2>×”××™×ž×•×Ÿ ×”×¡×ª×™×™×</h2>
    <div class="comparison-grid">
      <div class="comparison-box"><span>×–×ž×Ÿ ×ª×©×•×‘×” ×ž×ž×•×¦×¢</span><strong>${state.baseline.averageAnswerTime}s</strong></div>
      <div class="comparison-box"><span>×ª×©×•×‘×•×ª × ×›×•× ×•×ª</span><strong>${answers.length - mistakes}</strong></div>
      <div class="comparison-box"><span>×˜×¢×•×™×•×ª</span><strong>${mistakes}</strong></div>
      <div class="comparison-box"><span>×ª× ×•×¢×” ×ž×“×•×ž×”</span><strong>${state.baseline.averageMovementLevel}</strong></div>
    </div>
    <p class="notice good">× ×ª×•× ×™ ×”×‘×¡×™×¡ × ×©×ž×¨×• ×ž×§×•×ž×™×ª ×œ×”×©×•×•××” ×‘×–×ž×Ÿ ××ž×ª.</p>
  `;
  summary.classList.remove("hidden");
}

// This function starts emergency mode and resets emergency telemetry.
function startEmergency(orefStatus = null) {
  state.familyMembers = buildEmergencyMembers(orefStatus);
  state.emergency = {
    active: true,
    trigger: orefStatus?.hasGroupAlert ? "pikud_haoref" : "manual",
    orefStatus: orefStatus || null,
    startedAt: Date.now(),
    checkIns: {},
    telemetry: {
      safeClickTime: null,
      tapCount: 0,
      answerTimes: [],
      mistakes: 0,
      correct: 0,
      incorrect: 0,
      movementLevel: round(0.58 + Math.random() * 0.22)
    },
    missionCompletedAt: null,
    activityStartedAt: null,
    activityAnswer: null
  };
  saveState();
}

// This function creates the emergency member list from the active group and live alert state.
function buildEmergencyMembers(orefStatus = null) {
  const group = getActiveGroup();

  if (!group?.members?.length) {
    return state.familyMembers.map(member => ({ ...member, status: "at_risk" }));
  }

  const statusMap = new Map((orefStatus?.members || []).map(member => [member.memberId, member]));

  return group.members.map(member => {
    const liveStatus = statusMap.get(member.id);
    const status = orefStatus?.hasGroupAlert
      ? orefMemberStatusClass(liveStatus)
      : "at_risk";

    return {
      alertLocation: liveStatus?.alertLocation || member.alertLocation || null,
      avatar: member.avatar,
      id: member.id,
      name: member.username,
      role: member.role === "admin" ? "Admin" : "User",
      status
    };
  });
}

// This function returns the current user's member id in the emergency list.
function currentFamilyMemberId() {
  return state.user?.userId || "1";
}

// This function renders the emergency check-in state.
function renderEmergency() {
  renderFamilyList(document.querySelector("[data-emergency-family]"));
  renderEmergencyOrefSummary();
  const button = document.querySelector("[data-emergency-safe]");
  const message = document.querySelector("[data-emergency-message]");
  const current = state.familyMembers.find(member => member.id === currentFamilyMemberId());

  if (!button || !message) return;

  if (allMembersSafe()) {
    button.textContent = "×¤×ª×™×—×ª ×¤×¢×™×œ×•×ª";
    button.disabled = false;
    message.textContent = "×›×•×œ× ×ž×•×’× ×™×. ×”×¤×¢×™×œ×•×™×•×ª ×¤×ª×•×—×•×ª.";
    message.className = "notice good";
    return;
  }

  if (current?.status === "safe") {
    button.textContent = "××™×©×•×¨ ×ž×•×’×Ÿ × ×©×œ×—";
    button.disabled = true;
    message.textContent = "×ž×ž×ª×™×Ÿ ×œ××™×©×•×¨ ×›×œ ×—×‘×¨×™ ×”×§×‘×•×¦×”...";
    message.className = "notice warn";
    return;
  }

  button.textContent = "×× ×™ ×ž×•×’×Ÿ!";
  button.disabled = false;
  message.textContent = "×›×•×œ× ×ž×¡×•×ž× ×™× ×‘×¡×™×›×•×Ÿ ×¢×“ ×œ××™×©×•×¨.";
  message.className = "notice danger";
}

// This function marks one local family member as safe.
function markMemberSafe(id) {
  const member = state.familyMembers.find(item => item.id === id);
  if (!member || member.status === "safe") return;
  member.status = "safe";
  state.emergency.checkIns[id] = formatSeconds(secondsSince(state.emergency.startedAt));
}

// This function simulates other family members checking in.
function simulateFamilyCheckIns() {
  const currentId = currentFamilyMemberId();
  const pending = state.familyMembers.filter(member => member.status !== "safe" && member.id !== currentId);
  pending.forEach((member, index) => {
    window.setTimeout(() => {
      markMemberSafe(member.id);
      saveState();
      renderEmergency();
    }, 900 + index * 900);
  });
}

// This function renders the post-check-in game.
function renderGame() {
  const locked = document.querySelector("[data-game-locked]");
  const unlocked = document.querySelector("[data-game-unlocked]");
  const area = document.querySelector("[data-game-area]");
  if (!locked || !unlocked || !area) return;

  if (!allMembersSafe()) {
    locked.classList.remove("hidden");
    unlocked.classList.add("hidden");
    return;
  }

  locked.classList.add("hidden");
  unlocked.classList.remove("hidden");

  if (!state.emergency) startEmergency();
  if (!state.emergency.activityStartedAt) {
    state.emergency.activityStartedAt = Date.now();
    saveState();
  }

  const question = state.questions[0] || DEFAULT_QUESTIONS[0];
  const mission = state.missions[0] || DEFAULT_MISSIONS[0];
  const answered = state.emergency.activityAnswer;

  area.innerHTML = `
    <p class="eyebrow">×©××œ×•×Ÿ</p>
    <h2>${escapeHtml(question.question)}</h2>
    <div class="answer-grid">
      ${question.answers.map((answer, index) => {
        const className = answered && index === answered.answerIndex ? (answered.correct ? "correct" : "wrong") : "";
        return `<button class="answer-btn ${className}" type="button" data-game-answer="${index}" ${answered ? "disabled" : ""}>${escapeHtml(answer)}</button>`;
      }).join("")}
    </div>
    <p class="notice ${answered ? "good" : "hidden"}" data-game-feedback>${answered ? feedbackText(answered.correct) : ""}</p>
    <div class="card">
      <p class="eyebrow">×ž×©×™×ž×ª ×‘×˜×™×—×•×ª</p>
      <h3>${escapeHtml(mission.title)}</h3>
      <p class="subtitle">${escapeHtml(mission.description)}</p>
      <button class="btn btn-secondary" type="button" data-mission-done>${state.emergency.missionCompletedAt ? "×”×ž×©×™×ž×” ×”×•×©×œ×ž×”" : "×¡×™×•×"}</button>
    </div>
    <a class="btn btn-primary" href="summary.html">×¡×™×•× ×•×¦×¤×™×™×” ×‘×¡×™×›×•×</a>
  `;

  area.querySelectorAll("[data-game-answer]").forEach(button => {
    button.addEventListener("click", () => {
      const answerIndex = Number(button.dataset.gameAnswer);
      const correct = answerIndex === question.correctAnswerIndex;
      const timeToAnswer = secondsSince(state.emergency.activityStartedAt);

      state.emergency.activityAnswer = { answerIndex, correct, timeToAnswer };
      state.emergency.telemetry.answerTimes.push(timeToAnswer);
      state.emergency.telemetry.tapCount += 1;
      if (correct) state.emergency.telemetry.correct += 1;
      if (!correct) {
        state.emergency.telemetry.incorrect += 1;
        state.emergency.telemetry.mistakes += 1;
      }

      saveState();
      renderGame();
    });
  });

  area.querySelector("[data-mission-done]")?.addEventListener("click", event => {
    state.emergency.missionCompletedAt = secondsSince(state.emergency.activityStartedAt);
    state.emergency.telemetry.tapCount += 1;
    saveState();
    event.currentTarget.textContent = "×”×ž×©×™×ž×” ×”×•×©×œ×ž×”";
    event.currentTarget.disabled = true;
  });
}

// This function renders the stress report for one member.
function renderReport(memberId) {
  const detail = document.querySelector("[data-report-detail]");
  const member = state.familyMembers.find(item => item.id === memberId) || state.familyMembers[0];
  if (!detail || !member) return;

  const index = state.familyMembers.findIndex(item => item.id === member.id);
  const report = buildMemberReport(member, index);
  const baseline = state.baseline || DEFAULT_BASELINE;
  const currentAverage = report.currentAverage;
  const currentMistakeRate = report.currentMistakeRate;
  const movement = report.movementLevel;
  const stressByMinute = report.stressByMinute;
  const highest = stressByMinute.reduce((top, item) => item.stress > top.stress ? item : top, stressByMinute[0]);

  detail.innerHTML = `
    <section class="summary-card">
      <p class="eyebrow">×¡×™×›×•× ×ž×¦×‘</p>
      <h2>${escapeHtml(member.name)}: <span class="stress-level ${stressClass(report.stressLevel)}">${stressLabel(report.stressLevel)}</span></h2>
      <p class="subtitle">×“×§×ª ×œ×—×¥ ×’×‘×•×”×” ×‘×™×•×ª×¨: ×“×§×” ${highest.minute}. ×¡×™×‘×” ×ž×¨×›×–×™×ª: ${escapeHtml(report.reason)}.</p>
    </section>

    <section class="card">
      <h2>×¨×ž×ª ×œ×—×¥ ×œ×¤×™ ×“×§×”</h2>
      <div class="bar-chart">
        ${stressByMinute.map(item => `
          <div class="bar-row">
            <span>×“×§×” ${item.minute}</span>
            <span class="bar-track"><span class="bar-fill" style="--value:${item.stress}%"></span></span>
            <span>${item.stress}</span>
          </div>
        `).join("")}
      </div>
    </section>

    <section class="card">
      <h2>×–×ž×Ÿ ×ž×¢× ×”</h2>
      <div class="comparison-grid">
        <div class="comparison-box"><span>×‘×¡×™×¡ ××™×ž×•×Ÿ</span><strong>${baseline.averageAnswerTime}s</strong></div>
        <div class="comparison-box"><span>××™×¨×•×¢ × ×•×›×—×™</span><strong>${currentAverage}s</strong></div>
      </div>
    </section>

    <section class="card">
      <h2>× ×›×•×Ÿ ×ž×•×œ ×©×’×•×™</h2>
      <div class="comparison-grid">
        <div class="comparison-box"><span>× ×›×•×Ÿ</span><strong>${report.correct}</strong></div>
        <div class="comparison-box"><span>×©×’×•×™</span><strong>${report.mistakes}</strong></div>
      </div>
    </section>

    <section class="card">
      <h2>×‘×¡×™×¡ ×ž×•×œ ××™×¨×•×¢ × ×•×›×—×™</h2>
      <div class="comparison-grid">
        <div class="comparison-box"><span>×©×™×¢×•×¨ ×˜×¢×•×™×•×ª ×‘×¡×™×¡</span><strong>${baseline.mistakeRate}</strong></div>
        <div class="comparison-box"><span>×©×™×¢×•×¨ ×˜×¢×•×™×•×ª × ×•×›×—×™</span><strong>${currentMistakeRate}</strong></div>
        <div class="comparison-box"><span>×ª× ×•×¢×” ×‘×¡×™×¡×™×ª</span><strong>${baseline.averageMovementLevel}</strong></div>
        <div class="comparison-box"><span>×ª× ×•×¢×” × ×•×›×—×™×ª</span><strong>${movement}</strong></div>
      </div>
    </section>

    <section class="notice warn">
      ××™× ×“×™×§×¦×™×” ××¤×©×¨×™×ª ×œ×œ×—×¥: ${escapeHtml(report.explanation)}
      ×”×ž×œ×¦×”: ×œ×©×•×—×— ×‘×¨×•×’×¢ ×œ××—×¨ ×”××™×¨×•×¢ ×•×œ×©××•×œ ××™×š ×”×¨×’×™×©×•.
    </section>
  `;
}

// This function builds the local analytics report data for one member.
function buildMemberReport(member, index) {
  const baseline = state.baseline || DEFAULT_BASELINE;
  const telemetry = state.emergency?.telemetry || {};
  const answerTimes = telemetry.answerTimes?.length ? telemetry.answerTimes : [4.8];
  const currentAverage = round(average(answerTimes) + index * 0.35);
  const correct = Math.max((telemetry.correct || 1) - (index === 2 ? 1 : 0), 0);
  const mistakes = (telemetry.mistakes || 0) + (index === 2 ? 2 : index === 1 ? 1 : 0);
  const currentMistakeRate = round(mistakes / Math.max(correct + mistakes, 1));
  const movementLevel = round((telemetry.movementLevel || 0.7) + index * 0.06);
  const checkInTime = state.emergency?.checkIns?.[member.id] || (member.status === "safe" ? "00:08" : "Not checked in");

  let stressLevel = "Low";
  let reason = "×”×ª× ×”×’×•×ª ×§×¨×•×‘×” ×œ×‘×¡×™×¡ ×”××™×ž×•×Ÿ";

  if (currentAverage > baseline.averageAnswerTime * 1.8 && currentMistakeRate > 0.25 && movementLevel > baseline.averageMovementLevel + 0.3) {
    stressLevel = "High";
    reason = "×–×ž×Ÿ ×ª×’×•×‘×” ××™×˜×™ ×™×•×ª×¨, ×™×•×ª×¨ ×˜×¢×•×™×•×ª ×•×ª× ×•×¢×” ×’×‘×•×”×” ×ž×”×¨×’×™×œ";
  } else if (currentAverage > baseline.averageAnswerTime * 1.25 || currentMistakeRate > baseline.mistakeRate + 0.12) {
    stressLevel = "Medium";
    reason = "×ž×¢× ×” ×ž×¢×˜ ××™×˜×™ ×™×•×ª×¨ ×ž×‘×¡×™×¡ ×”××™×ž×•×Ÿ";
  }

  const stressBase = stressLevel === "High" ? 78 : stressLevel === "Medium" ? 56 : 32;
  const stressByMinute = [0, 1, 2, 3, 4, 5].map(offset => ({
    minute: offset + 1,
    stress: Math.max(stressBase - offset * 8, 18)
  }));

  return {
    checkInTime,
    participation: member.id === "1" ? "×©××œ×•×Ÿ ×•×ž×©×™×ž×”" : "××™×©×•×¨ ×ž×•×’×Ÿ ×‘×œ×‘×“",
    correct,
    mistakes,
    stressLevel,
    reason,
    currentAverage,
    currentMistakeRate,
    movementLevel,
    stressByMinute,
    explanation: `${member.name} ×”×¨××”/×” ${reason} ×‘×ž×”×œ×š ×”××™×¨×•×¢. ×™×™×ª×›×Ÿ ×©×›×“××™ ×œ×©×™× ×œ×‘, ××š ×–×” ××™× ×• ××‘×—×•×Ÿ.`
  };
}

// This function starts the visible emergency timer.
function startEventTimer() {
  if (!document.querySelector("[data-event-timer]")) return;

  updateEventTimer();
  window.setInterval(updateEventTimer, 1000);
}

// This function updates timer labels and progress bars.
function updateEventTimer() {
  if (!state.emergency?.startedAt) return;
  const elapsed = secondsSince(state.emergency.startedAt);
  const remaining = Math.max(EVENT_DURATION_SECONDS - elapsed, 0);
  const progress = Math.max((remaining / EVENT_DURATION_SECONDS) * 100, 0);

  document.querySelectorAll("[data-event-timer]").forEach(node => {
    node.textContent = formatSeconds(remaining);
  });

  document.querySelectorAll("[data-event-progress]").forEach(node => {
    node.style.setProperty("--progress", `${progress}%`);
  });
}

// This function counts local family members by status.
function countStatus(status) {
  return state.familyMembers.filter(member => member.status === status).length;
}

// This function checks whether every local family member is safe.
function allMembersSafe() {
  return state.familyMembers.every(member => member.status === "safe");
}

// This function maps a status id to display text.
function statusLabel(status) {
  if (status === "safe") return "×ž×•×’×Ÿ";
  if (status === "at_risk") return "×‘×¡×™×›×•×Ÿ";
  return "OFFLINE";
}

// This function writes text into a selected element when it exists.
function setText(selector, value) {
  const node = document.querySelector(selector);
  if (node) node.textContent = value;
}

// This function escapes text before placing it in HTML strings.
function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

// This function calculates seconds elapsed since a timestamp.
function secondsSince(timestamp) {
  return Math.max((Date.now() - timestamp) / 1000, 0);
}

// This function formats seconds as mm:ss.
function formatSeconds(seconds) {
  const total = Math.max(Math.floor(seconds), 0);
  const minutes = Math.floor(total / 60).toString().padStart(2, "0");
  const rest = (total % 60).toString().padStart(2, "0");
  return `${minutes}:${rest}`;
}

// This function calculates an average for numeric values.
function average(values) {
  if (!values.length) return 0;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

// This function rounds a number to two decimals.
function round(value) {
  return Math.round(value * 100) / 100;
}

// This function maps a stress level to its CSS class.
function stressClass(level) {
  if (level === "High") return "stress-high";
  if (level === "Medium") return "stress-medium";
  return "stress-low";
}

// This function maps a stress level to display text.
function stressLabel(level) {
  if (level === "High") return "×’×‘×•×”";
  if (level === "Medium") return "×‘×™× ×•× ×™";
  return "× ×ž×•×š";
}

// This function chooses activity feedback text for a result.
function feedbackText(correct) {
  return correct ? "×›×œ ×”×›×‘×•×“! ×”×ž×©×™×›×• ×›×š." : "× ×™×¡×™×•×Ÿ ×˜×•×‘. ××ª× ×¢×•×©×™× ×¢×‘×•×“×” ×˜×•×‘×”.";
}
