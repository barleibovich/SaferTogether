import {
  getCurrentUserProfile,
  loginWithUsername,
  logout,
  signUpWithUsername
} from "./src/api/authGateway.js";
import {
  activateGroupActivity,
  createGroupActivity,
  deactivateGroupActivity,
  deleteGroupActivity,
  getActiveGroupActivities,
  getGroupActivities,
  getGroupActivityResults,
  getGroupStatistics,
  getUserStatsSummary,
  reviewGroupActivityResult,
  submitGroupActivityResult
} from "./src/api/activityGateway.js";
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
  reviewJoinRequest
} from "./src/api/groupGateway.js";
import {
  getGroupOrefStatus,
  saveCurrentUserAlertLocation
} from "./src/api/orefGateway.js";
import { alarmAudio } from "./src/alarmAudio.js";
import { gameAudio } from "./src/gameAudio.js";
import {
  endAlarm,
  getActiveAlarm,
  markAlarmSafe,
  reportAlarmProgress,
  startAlarm,
  unlockAlarm
} from "./src/api/alarmGateway.js";
import { sendPresenceHeartbeat } from "./src/api/presenceGateway.js";
import { deletePushSubscription, getPushConfig, savePushSubscription } from "./src/api/pushGateway.js";
import { startRotationTracking, takeRotationForItem } from "./src/sensors/rotation.js";

const STORAGE_KEY = "saferTogetherState.v5";
const SIGNUP_AVATAR_KEY = "saferTogetherSignupAvatar.v1";
const EVENT_DURATION_SECONDS = 600;
const OREF_POLL_INTERVAL_MS = 5000;
const GPS_LOCATION_SAVE_INTERVAL_MS = 15000;
const GPS_LOCATION_DISTANCE_THRESHOLD_METERS = 50;
const PRESENCE_HEARTBEAT_MS = 15000;
const PRESENCE_ONLINE_THRESHOLD_MS = 45000;
const ALARM_POLL_INTERVAL_MS = 5000;
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
const CHARACTER_ACCESSORIES = ["none", "bandana", "crown", "glasses", "mask"];
const CHARACTER_BACKGROUNDS = [...AVATAR_BUILDER_COLORS, "navy", "white", "black", "red", "green", "denim"];
const CHARACTER_BOTTOMS = ["jeans", "cargo", "sports"];
const CHARACTER_CLOTHING_COLORS = ["black", "blue", "green", "red", "white", "yellow"];
const CHARACTER_EYE_COLORS = ["brown", "blue", "green", "hazel", "violet", "amber", "gray"];
const CHARACTER_EYES = ["dot", "almond", "happy", "focused", "sleepy"];
const CHARACTER_FACE_SHAPES = ["round", "soft", "sharp", "snout", "long"];
const CHARACTER_HAIR_COLORS = ["black", "brown", "blonde", "red", "blue", "pink", "silver", "white"];
const CHARACTER_HAIR_STYLES = ["short", "bob", "curls", "spiky", "long", "ponytail", "bun", "mohawk", "hijab", "none"];
const CHARACTER_SEXES = ["female", "male"];
const CHARACTER_SHOES = ["sneakers", "boots", "space-shoes"];
const CHARACTER_SKINS = ["porcelain", "light", "tan", "brown", "deep", "green", "red", "gray", "gold"];
const CHARACTER_SPECIES = [
  "male",
  "female",
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
const CHARACTER_TOPS = ["peasant", "ranger"];
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
  sex: "male",
  shoes: "sneakers",
  shoeColor: "white",
  skin: "tan",
  species: "male",
  top: "peasant",
  topColor: "blue"
};
const DEFAULT_FAMILY = [
  { id: "1", name: "דקל", role: "ילד", status: "offline", avatar: "🐯" },
  { id: "2", name: "שירה", role: "ילדה", status: "offline", avatar: "🐬" },
  { id: "3", name: "אביב", role: "ילד", status: "offline", avatar: "🦩" },
  { id: "4", name: "יהלי", role: "ילד", status: "offline", avatar: "🦋" }
];

const DEFAULT_QUESTIONS = [
  {
    id: "q1",
    question: "מה צריך לעשות אחרי שנכנסים למרחב המוגן?",
    answers: [
      "לצאת אחרי דקה",
      "להישאר 10 דקות",
      "לפתוח את החלון",
      "לעמוד ליד הדלת"
    ],
    correctAnswerIndex: 1
  }
];

const DEFAULT_MISSIONS = [
  {
    id: "m1",
    title: "משחק ערכת חירום",
    description: "בחרו בכל שלב את הפריט שצריך להביא למרחב המוגן.",
    expectedChannel: "",
    requiredAction: "בחירת פריטי חירום",
    target: "puzzle"
  },
  {
    id: "m2",
    title: "משחק קוד הדלת",
    description: "זכרו את רצף הספרות וחזרו עליו בלוח המקשים.",
    expectedChannel: "",
    requiredAction: "חזרה על רצף קוד הדלת",
    target: "code"
  },
  {
    id: "m3",
    title: "משחק הטילים",
    description: "התחמקו מטילים נופלים במשך דקה בעזרת תנועה או הטיית הטלפון.",
    expectedChannel: "",
    requiredAction: "התחמקות מטילים",
    target: "missile"
  }
];

const MISSION_GAME_DEFINITIONS = {
  puzzle: {
    label: "ערכת חירום",
    description: "משחק בחירת פריטים למרחב המוגן: בכל אחד מארבעה שלבים מוצגות ארבע תמונות, והילד בוחר מה צריך להביא."
  },
  code: {
    label: "קוד הדלת",
    description: "משחק זיכרון רצפים: המערכת מאירה ספרות בלוח המקשים, והילד חוזר על הרצף בשלבים באורך 3, 4, 5 ו-5 ספרות."
  },
  missile: {
    label: "טילים",
    description: "משחק התחמקות מטילים במשך דקה: הילד מזיז את הדמות, ובטלפון גם מטה את המכשיר, כדי להימנע מפגיעות."
  }
};
const MISSION_GAME_IDS = Object.keys(MISSION_GAME_DEFINITIONS);

const ACTIVITY_MODES = ["real", "training"];
// play an encouragement clip if the player is stuck on a single mission-room
// stage (a step inside a task) this long
const MISSION_INACTIVITY_DELAY_MS = 15000;

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
let emergencyActivityRedirectTimer = null;

document.addEventListener("DOMContentLoaded", () => {
  ensureDefaults();
  routePage();
  startEventTimer();
});

// default app state in the browser
function initialState() {
  return {
    user: null,
    activeGroupId: "",
    groups: [],
    familyName: "",
    familyMembers: copy(DEFAULT_FAMILY),
    questions: copy(DEFAULT_QUESTIONS),
    missions: copy(DEFAULT_MISSIONS),
    activityDraft: null,
    groupActivities: [],
    activityResults: [],
    baseline: copy(DEFAULT_BASELINE),
    practiceSession: null,
    orefStatus: null,
    emergency: null
  };
}

// clone simple json-safe stuff
function copy(value) {
  return JSON.parse(JSON.stringify(value));
}

// load saved state from localStorage
function loadState() {
  try {
    const saved = sessionStorage.getItem(STORAGE_KEY);
    return saved ? { ...initialState(), ...JSON.parse(saved) } : initialState();
  } catch {
    return initialState();
  }
}

// save the app state to localStorage
function saveState() {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(stateForStorage()));
}

// strip the big avatar pngs so they don't bloat storage
function stateForStorage() {
  const snapshot = copy(state);

  if (snapshot.user) {
    delete snapshot.user.avatarImage;
  }

  snapshot.groups = (snapshot.groups || []).map(group => ({
    ...group,
    members: (group.members || []).map(member => {
      const cleanMember = { ...member };
      delete cleanMember.avatarImage;
      return cleanMember;
    })
  }));

  snapshot.familyMembers = (snapshot.familyMembers || []).map(member => {
    const cleanMember = { ...member };
    delete cleanMember.avatarImage;
    return cleanMember;
  });

  return snapshot;
}

// stash the avatar unity sends back so signup can use it before there's an account
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

// keep unity redirects inside the app, no random external urls
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

// fill in any missing state bits with defaults
function ensureDefaults() {
  state.familyMembers = state.familyMembers?.length ? state.familyMembers : copy(DEFAULT_FAMILY);
  state.groups = Array.isArray(state.groups) ? state.groups : [];
  state.activeGroupId = state.groups.some(group => group.id === state.activeGroupId)
    ? state.activeGroupId
    : state.groups[0]?.id || "";
  state.familyName = state.groups.find(group => group.id === state.activeGroupId)?.name || "";
  state.questions = state.questions?.length ? state.questions : copy(DEFAULT_QUESTIONS);
  state.missions = state.missions?.length ? state.missions : copy(DEFAULT_MISSIONS);
  state.activityDraft = state.activityDraft || null;
  state.groupActivities = Array.isArray(state.groupActivities) ? state.groupActivities : [];
  state.activityResults = Array.isArray(state.activityResults) ? state.activityResults : [];
  state.baseline = state.baseline || copy(DEFAULT_BASELINE);
  state.orefStatus = state.orefStatus || null;
  delete state.gpsAlertLocationEnabled;
  saveState();
}

// turn a username into a stable number
function seedFromUsername(username) {
  return String(username || "")
    .trim()
    .toLowerCase()
    .split("")
    .reduce((sum, char) => sum + char.charCodeAt(0), 0);
}

// return the value if it's a known option, else the fallback
function optionValue(value, options, fallback) {
  const cleanValue = String(value || "").trim().toLowerCase();
  return options.includes(cleanValue) ? cleanValue : fallback;
}

// pick a valid species, mapping removed legacy creature ids back to a selectable avatar
function characterSpeciesValue(value) {
  const cleanValue = String(value || "").trim().toLowerCase();
  if (cleanValue === "human") return "male";
  if (cleanValue === "dragon" || cleanValue === "devil") return "male";
  return optionValue(cleanValue, CHARACTER_SPECIES, DEFAULT_CHARACTER_SPEC.species);
}

// normalize a full character spec
function normalizeCharacterSpec(spec = {}) {
  const species = characterSpeciesValue(spec.species);
  const bottom = optionValue(spec.bottom, CHARACTER_BOTTOMS, DEFAULT_CHARACTER_SPEC.bottom);
  return {
    accessory: optionValue(spec.accessory, CHARACTER_ACCESSORIES, DEFAULT_CHARACTER_SPEC.accessory),
    background: optionValue(spec.background, CHARACTER_BACKGROUNDS, DEFAULT_CHARACTER_SPEC.background),
    bottom,
    bottomColor: bottom === "jeans" ? "denim" : optionValue(spec.bottomColor, CHARACTER_CLOTHING_COLORS, "blue"),
    eyeColor: optionValue(spec.eyeColor, CHARACTER_EYE_COLORS, DEFAULT_CHARACTER_SPEC.eyeColor),
    eyes: optionValue(spec.eyes, CHARACTER_EYES, DEFAULT_CHARACTER_SPEC.eyes),
    face: optionValue(spec.face, CHARACTER_FACE_SHAPES, DEFAULT_CHARACTER_SPEC.face),
    hair: optionValue(spec.hair, CHARACTER_HAIR_STYLES, DEFAULT_CHARACTER_SPEC.hair),
    hairColor: optionValue(spec.hairColor, CHARACTER_HAIR_COLORS, DEFAULT_CHARACTER_SPEC.hairColor),
    sex: species === "female" ? "female" : species === "male" ? "male" : optionValue(spec.sex, CHARACTER_SEXES, DEFAULT_CHARACTER_SPEC.sex),
    shoes: optionValue(spec.shoes, CHARACTER_SHOES, DEFAULT_CHARACTER_SPEC.shoes),
    shoeColor: optionValue(spec.shoeColor, CHARACTER_CLOTHING_COLORS, DEFAULT_CHARACTER_SPEC.shoeColor),
    skin: optionValue(spec.skin, CHARACTER_SKINS, DEFAULT_CHARACTER_SPEC.skin),
    species,
    top: optionValue(spec.top, CHARACTER_TOPS, DEFAULT_CHARACTER_SPEC.top),
    topColor: optionValue(spec.topColor, CHARACTER_CLOTHING_COLORS, DEFAULT_CHARACTER_SPEC.topColor)
  };
}

// build the saved avatar id string
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

// stable default avatar spec from the username
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

// same but returns the id string
function avatarFromUsername(username) {
  return buildCharacterAvatar(avatarSpecFromUsername(username));
}

// parse a unity builder avatar id into parts
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

// parse an old (v1) character avatar id into parts
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

// parse a full (v2) character avatar id into parts
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

  const normalizedId = buildCharacterAvatar(spec);
  const legacyHumanId = normalizedId.replace(":v2:male:", ":v2:human:");

  if (normalizedId !== cleanAvatar && legacyHumanId !== cleanAvatar) {
    return null;
  }

  return {
    ...spec,
    id: normalizedId
  };
}

// the Quaternius pack characters are the only selectable avatars now ("pack:<character>")
const PACK_CHARACTERS = [
  "adventurer", "beach", "casual", "casual2", "farmer", "king",
  "punk", "spacesuit", "suit", "swat", "worker"
];

// parse a pack avatar id ("pack:<character>")
function parsePackAvatar(avatar) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();

  if (!cleanAvatar.startsWith("pack:")) {
    return null;
  }

  const name = cleanAvatar.slice("pack:".length);
  return PACK_CHARACTERS.includes(name) ? `pack:${name}` : null;
}

// accept pack, preset, builder, or legacy character avatar ids
function normalizeAvatar(avatar, username) {
  const cleanAvatar = String(avatar || "").trim().toLowerCase();
  const packAvatar = parsePackAvatar(cleanAvatar);

  if (packAvatar) {
    return packAvatar;
  }

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

// pick a letter for the avatar badge
function avatarInitial(username) {
  return String(username || "?").trim().charAt(0).toUpperCase() || "?";
}

// is this a base64 png data url?
function isAvatarImage(value) {
  return /^data:image\/png;base64,[a-zA-Z0-9+/=]+$/.test(String(value || "").trim());
}

// use the unity png if we have one, else the initial
function renderAvatarBadge(username, className = "profile-avatar", avatarImage = "") {
  if (isAvatarImage(avatarImage)) {
    return `
      <span class="${className} avatar-image-badge">
        <img class="avatar-image-render" src="${escapeHtml(avatarImage)}" alt="" aria-hidden="true">
      </span>
    `;
  }

  return `
    <span class="${className} avatar-initial-badge">
      ${escapeHtml(avatarInitial(username))}
    </span>
  `;
}

// send each page to its init function
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
  if (page === "statistics") initAdminPage(initStatistics);
}

// hook up the login form to the auth api
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

// hook up signup form + initial avatar to the auth api
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
      showFormSuccess(form, "נשמר בהצלחה");
      window.setTimeout(() => {
        window.location.href = "groups.html";
      }, 900);
    } catch (error) {
      showFormError(form, readableAuthError(error));
      setFormBusy(form, false);
    }
  });
}

// save the logged-in user into local state
function setSessionUser(username, role, userId, avatar, avatarImage = "", alertLocation = null) {
  const cleanRole = role === "admin" ? "admin" : "user";

  state.user = {
    avatar: normalizeAvatar(avatar, username),
    avatarImage: isAvatarImage(avatarImage) ? avatarImage : "",
    userId,
    username,
    name: username,
    role: cleanRole,
    alertLocation,
    familyRoomId: state.activeGroupId || ""
  };

  state.groups = state.groups.map(group => ({ ...group, userRole: cleanRole }));
}

// pull the server session profile into local state
async function loadSessionIntoState() {
  const profile = await getCurrentUserProfile();

  if (!profile) {
    return null;
  }

  setSessionUser(profile.username, profile.role, profile.id, profile.avatar, profile.avatarImage, profile.alertLocation);
  state.groups = [];
  await refreshCurrentUserGroups(profile.role);
  return profile;
}

// refresh the user's groups
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
            : normalizeAvatar(member.avatar, member.username),
          avatarImage: member.id === state.user?.userId
            ? (state.user.avatarImage || member.avatarImage || "")
            : (member.avatarImage || "")
        }))
        : [],
      name: group.name || "קבוצה ללא שם",
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

// lock/unlock a form while a request runs
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

// show an error in a form
function showFormError(form, message) {
  showFormMessage(form, message, "auth-error");
}

// show a success msg in a form
function showFormSuccess(form, message) {
  showFormMessage(form, message, "auth-success");
}

// add or update the form message node
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

// remove the form message
function clearFormMessage(form) {
  form.querySelector("[data-form-message]")?.remove();
}

// pull a readable message out of an error, else use the fallback
function readableError(error, fallback) {
  if (typeof error === "string") {
    return error;
  }

  return error?.message || fallback;
}

// auth error -> displayable text
function readableAuthError(error) {
  return readableError(error, "ההתחברות נכשלה. נסו שוב.");
}

// guard admin-only pages, then run their init
async function initAdminPage(initializer) {
  const allowed = await requireAdminAccess();
  if (allowed && initializer) await initializer();
}

// make sure the user is logged in + admin, else redirect
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

// the little "hi user" greeting
function renderCurrentUserSummary(node) {
  if (!node || !state.user) return;
  node.textContent = `היי ${state.user.username || state.user.name}, כיף שחזרת!`;
}

// draw the current user's avatar preview + edit button
function renderCurrentAvatar() {
  if (!state.user) return;

  const summaryMarkup = renderAvatarBadge(
    state.user.username || state.user.name,
    "profile-avatar avatar-unity-preview",
    state.user.avatarImage
  );

  document.querySelectorAll("[data-current-avatar-summary]").forEach(preview => {
    preview.innerHTML = `
      <div class="avatar-edit-preview">
        ${summaryMarkup}
        <button class="avatar-edit-button" type="button" data-open-avatar-editor aria-label="עריכת דמות"></button>
      </div>
    `;
  });
}

// edit-avatar button opens the unity editor page
function initUnityAvatarLaunch() {
  renderCurrentAvatar();

  document.querySelectorAll("[data-open-avatar-editor]").forEach(openButton => openButton.addEventListener("click", () => {
    window.location.href = "avatar-editor.html?return=groups.html";
  }));
}

// boot the embedded unity avatar editor page
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
    status.textContent = profile ? `מחובר/ת כ-${profile.username}` : "עורך הדמות";
  }
  await loadUnityAvatarEditor(profile);
}

// grab the unity build urls off the host element
function getUnityHostConfig(host) {
  return {
    codeUrl: host.dataset.codeUrl,
    dataUrl: host.dataset.dataUrl,
    frameworkUrl: host.dataset.frameworkUrl,
    loaderUrl: host.dataset.loaderUrl,
    streamingAssetsUrl: host.dataset.streamingAssetsUrl
  };
}

// load a script tag, resolve when ready
function loadScript(src) {
  return new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.async = true;
    script.src = src;
    script.onload = resolve;
    script.onerror = () => reject(new Error("בניית ה-Unity WebGL לא נמצאה"));
    document.head.append(script);
  });
}

// load the unity instance and hand it the web profile
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
    showUnityStatus("");
  } catch (error) {
    showUnityStartupError(error);
  }
}

// move the unity loading bar
function updateUnityProgress(progress) {
  const progressBar = document.querySelector("[data-unity-progress] span");

  if (progressBar) {
    progressBar.style.width = `${Math.round(progress * 100)}%`;
  }
}

// status text above the unity editor
function showUnityStatus(message, tone = "") {
  const status = document.querySelector("[data-unity-status]");

  if (!status) {
    return;
  }

  if (!message) {
    status.className = "notice hidden";
    status.textContent = "";
    return;
  }

  status.className = `notice ${tone}`.trim();
  status.textContent = message;
}

// show build instructions when the unity build is missing
function showUnityBuildMissing(error) {
  const missing = document.querySelector("[data-unity-missing]");
  const host = document.querySelector("[data-unity-host]");

  host?.classList.add("unity-webgl-host-missing");
  missing?.classList.remove("hidden");
  showUnityStatus(readableError(error, "בניית ה-Unity WebGL לא נמצאה"), "warn");
}

// report a real unity startup error (not a missing-file one)
function showUnityStartupError(error) {
  const missing = document.querySelector("[data-unity-missing]");
  const host = document.querySelector("[data-unity-host]");

  host?.classList.remove("unity-webgl-host-missing");
  missing?.classList.add("hidden");
  showUnityStatus(readableError(error, "לא ניתן להפעיל את עורך הדמות"), "warn");
}

// send the web profile into the running unity instance
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

// wire logout links on pages other than groups
function wireLogoutLink() {
  document.querySelector("[data-logout]")?.addEventListener("click", async event => {
    event.preventDefault();

    try {
      await unsubscribeAlarmPush();
      await logout();
    } catch (error) {
      console.warn(readableAuthError(error));
    }

    alarmAudio.stop();
    state = initialState();
    sessionStorage.removeItem(STORAGE_KEY);
    window.location.href = "index.html";
  });
}

// --- web push (alarm notifications even when the app is closed) -------------
// A device only receives alarms after the member logged in AND allowed
// notifications at least once — that's when we register it against their user.
// Re-affirmed on every board load; removed on logout so a logged-out user (or
// former member) stops getting that group's alarms.

let pushSubscriptionEnsured = false;

// VAPID public key (base64url) -> Uint8Array for PushManager.subscribe
function urlBase64ToUint8Array(base64String) {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64);
  const output = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; i += 1) {
    output[i] = raw.charCodeAt(i);
  }
  return output;
}

function pushSupported() {
  return "serviceWorker" in navigator && "PushManager" in window && "Notification" in window;
}

// register the service worker + subscribe this device for the current user.
// Safe to call repeatedly; only acts once per page and only when logged in.
async function ensureAlarmPushSubscription() {
  if (pushSubscriptionEnsured || !pushSupported() || !state.user) {
    return;
  }
  pushSubscriptionEnsured = true;

  try {
    const config = await getPushConfig();
    if (!config?.enabled || !config.publicKey) {
      return; // server has no VAPID keys configured — push is off
    }

    await navigator.serviceWorker.register("/sw.js");

    const subscribeNow = async () => {
      try {
        const ready = await navigator.serviceWorker.ready;
        const existing = await ready.pushManager.getSubscription();
        const subscription = existing || await ready.pushManager.subscribe({
          applicationServerKey: urlBase64ToUint8Array(config.publicKey),
          userVisibleOnly: true
        });
        await savePushSubscription(subscription.toJSON(), navigator.userAgent);
      } catch (error) {
        console.warn("push subscribe failed:", error?.message || error);
      }
    };

    if (Notification.permission === "granted") {
      await subscribeNow();
      return;
    }

    if (Notification.permission === "default") {
      // Mobile browsers only allow the permission prompt from a user gesture,
      // so ask on the first tap rather than on load.
      document.addEventListener("pointerdown", async () => {
        const permission = await Notification.requestPermission();
        if (permission === "granted") {
          await subscribeNow();
        }
      }, { once: true });
    }
  } catch (error) {
    console.warn("push setup failed:", error?.message || error);
  }
}

// drop this device's subscription so a logged-out user stops getting alarms.
// Call before logout() so the DELETE request is still authenticated.
async function unsubscribeAlarmPush() {
  if (!pushSupported()) {
    return;
  }

  try {
    const registration = await navigator.serviceWorker.getRegistration();
    const subscription = await registration?.pushManager.getSubscription();
    if (!subscription) {
      return;
    }

    await deletePushSubscription(subscription.endpoint).catch(() => {});
    await subscription.unsubscribe().catch(() => {});
  } catch (error) {
    console.warn("push unsubscribe failed:", error?.message || error);
  }
}

// set up the groups screen
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
  startPresenceHeartbeat();
  renderOrefStatus();
  refreshOrefStatus();
  startOrefStatusPolling();

  renderGroupsList();
  if (user.role !== "admin" && state.groups.length) {
    startAlarmBroadcastPolling();
  }
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
              <small>חבר/ה</small>
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
      showFormSuccess(joinForm, "הבקשה נשלחה");
    } catch (error) {
      showFormError(joinForm, readableAuthError(error));
    } finally {
      setFormBusy(joinForm, false);
    }
  });

  document.querySelector("[data-logout]")?.addEventListener("click", async event => {
    event.preventDefault();
    try {
      await unsubscribeAlarmPush();
      await logout();
    } catch (error) {
      console.warn(readableAuthError(error));
    }
    state = initialState();
    sessionStorage.removeItem(STORAGE_KEY);
    window.location.href = "index.html";
  });
}

// open the picked group
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

// draw the group list
function renderGroupsList() {
  const container = document.querySelector("[data-groups-list]");
  if (!container) return;

  if (!state.groups.length) {
    container.innerHTML = `<p class="notice">אין עדיין קבוצות.</p>`;
    return;
  }

  container.innerHTML = state.groups.map(group => `
    <article class="group-entry">
      <div class="group-card">
        ${group.userRole === "admin" ? `
          <button class="group-delete-button" type="button" data-delete-group="${group.id}" aria-label="מחיקת קבוצה" title="מחיקת קבוצה">&#128465;</button>
        ` : ""}
        <button class="group-card-main" type="button" data-open-group="${group.id}">
          <span class="group-icon">${group.userRole === "admin" ? "*" : "."}</span>
          <span>
            <strong>${escapeHtml(group.name)}</strong>
            <small>${group.userRole === "admin" ? "מנהל/ת" : "חבר/ה"}</small>
          </span>
        </button>
      </div>
      ${group.userRole === "admin" ? `
        <div class="group-extra">
          <p class="notice">קוד קבוצה: <strong class="join-code-value">${escapeHtml(group.joinCode)}</strong></p>
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

// draw the connected members of the active group (used on the groups page)
function renderGroupPresence() {
  const section = document.querySelector("[data-group-presence]");
  const list = document.querySelector("[data-group-presence-list]");
  if (!section || !list) return;

  const group = getActiveGroup();
  const members = group?.members || [];

  if (!members.length) {
    section.classList.add("hidden");
    return;
  }

  section.classList.remove("hidden");
  list.innerHTML = members.map(member => {
    const isMe = state.user?.userId && String(member.id) === String(state.user.userId);
    const online = isMe || isMemberOnline(member);
    return `
      <article class="member-card">
        ${renderAvatarBadge(member.username, "member-avatar avatar-unity-preview", member.avatarImage)}
        <div class="member-main">
          <p class="member-name">
            <span class="presence-dot ${online ? "online" : "offline"}" title="${online ? "מחובר" : "לא מחובר"}"></span>
            ${escapeHtml(member.username)}
            ${isMe ? `<span class="member-me-pill">אני</span>` : ""}
          </p>
        </div>
        <span class="status-pill ${online ? "safe" : "offline"}">${online ? "מחובר" : "לא מחובר"}</span>
      </article>
    `;
  }).join("");
}

// keep the groups-page connected list fresh
function startGroupsPresencePolling() {
  const intervalId = setInterval(async () => {
    if (document.hidden) return;
    try {
      await refreshCurrentUserGroups(state.user?.role || "user");
      saveState();
      renderGroupPresence();
    } catch {
      // ignore polling errors
    }
  }, PRESENCE_HEARTBEAT_MS);
  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

// create-group page (admin only)
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
    const name = data.get("groupName")?.toString().trim() || "קבוצה חדשה";

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

// set up the group board screen
async function initBoard() {
  // Paint the cached group name/id right away. state is hydrated from
  // sessionStorage synchronously, so this avoids the header flashing the
  // "הקבוצה שלי" placeholder and then jumping to the real group name once the
  // network refresh below resolves.
  const cachedGroup = getActiveGroup();
  if (cachedGroup) {
    setText("[data-active-group-name]", cachedGroup.name);
  }

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
  startPresenceHeartbeat();
  initUnityAvatarLaunch();
  void ensureAlarmPushSubscription();
  renderBoardMembers(group);
  renderBoardPendingRequests(group);
  initAlertLocationControls();
  renderOrefStatus();
  refreshOrefStatus();
  startOrefStatusPolling();

  const isAdmin = applyAdminOnlyVisibility();

  if (!isAdmin) {
    // Members get pulled into whatever alarm (real or training) the admin raises.
    startAlarmBroadcastPolling();
    startMembersPolling();
  }

  // admin raises a real alarm for the whole group, then drops into the emergency screen
  document.querySelector("[data-trigger-emergency]")?.addEventListener("click", async () => {
    await raiseGroupAlarm("real");
  });

  // admin raises a training alarm for the whole group
  document.querySelector("[data-trigger-training]")?.addEventListener("click", async () => {
    await raiseGroupAlarm("training");
  });

  document.querySelectorAll("[data-open-oref-emergency]").forEach(button => {
    button.addEventListener("click", () => {
      if (!state.orefStatus?.hasGroupAlert) return;
      startEmergency(state.orefStatus, "real");
      window.location.href = "emergency.html";
    });
  });

  document.querySelector("[data-open-pending-requests]")?.addEventListener("click", event => {
    const panel = document.querySelector("[data-pending-requests-panel]");
    if (!panel) return;

    const isOpen = panel.classList.toggle("hidden") === false;
    document.querySelector("[data-activity-admin-panel]")?.classList.add("hidden");
    document.querySelector("[data-open-group-games]")?.setAttribute("aria-expanded", "false");
    event.currentTarget.setAttribute("aria-expanded", String(isOpen));
  });

  document.querySelector("[data-old-rename-group]")?.addEventListener("click", async () => {
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

  document.querySelector("[data-open-group-games]")?.addEventListener("click", event => {
    const panel = document.querySelector("[data-activity-admin-panel]");
    if (!panel) return;

    const isOpen = panel.classList.toggle("hidden") === false;
    document.querySelector("[data-pending-requests-panel]")?.classList.add("hidden");
    document.querySelector("[data-open-pending-requests]")?.setAttribute("aria-expanded", "false");
    event.currentTarget.setAttribute("aria-expanded", String(isOpen));
    if (isOpen) {
      void renderAdminActivities(getActiveGroup());
    }
  });

  initBoardGroupNameEditor();

  if (isCurrentUserAdminForActiveGroup()) {
    startBoardRequestsPolling();
    renderAdminActivities(group);
  }
}

function initBoardGroupNameEditor() {
  document.querySelector("[data-rename-group]")?.addEventListener("click", () => {
    setBoardGroupNameEditing(true);
  });

  document.querySelector("[data-save-group-name]")?.addEventListener("click", () => {
    void saveBoardGroupNameEdit();
  });

  document.querySelector("[data-cancel-group-name]")?.addEventListener("click", () => {
    setBoardGroupNameEditing(false);
  });

  document.querySelector("[data-group-name-input]")?.addEventListener("keydown", event => {
    if (event.key === "Enter") {
      event.preventDefault();
      void saveBoardGroupNameEdit();
    }

    if (event.key === "Escape") {
      event.preventDefault();
      setBoardGroupNameEditing(false);
    }
  });
}

function setBoardGroupNameEditing(isEditing) {
  const group = getActiveGroup();
  const label = document.querySelector("[data-active-group-name]");
  const input = document.querySelector("[data-group-name-input]");
  const editButton = document.querySelector("[data-rename-group]");
  const saveButton = document.querySelector("[data-save-group-name]");
  const cancelButton = document.querySelector("[data-cancel-group-name]");

  if (!label || !input || !editButton || !saveButton || !cancelButton) return;

  if (isEditing) {
    input.value = group?.name || label.textContent.trim();
    window.requestAnimationFrame(() => {
      input.focus();
      input.select();
    });
  }

  label.classList.toggle("hidden", isEditing);
  input.classList.toggle("hidden", !isEditing);
  saveButton.classList.toggle("hidden", !isEditing);
  cancelButton.classList.toggle("hidden", !isEditing);
  editButton.classList.toggle("hidden", isEditing || !isCurrentUserAdminForActiveGroup());
}

async function saveBoardGroupNameEdit() {
  const group = getActiveGroup();
  const input = document.querySelector("[data-group-name-input]");
  const saveButton = document.querySelector("[data-save-group-name]");
  const cancelButton = document.querySelector("[data-cancel-group-name]");

  if (!group || !input) return;

  const newName = input.value.trim();
  if (!newName) {
    input.focus();
    return;
  }

  if (newName === group.name) {
    setBoardGroupNameEditing(false);
    return;
  }

  input.disabled = true;
  if (saveButton) saveButton.disabled = true;
  if (cancelButton) cancelButton.disabled = true;

  try {
    await renameGroup(group.id, newName);
    group.name = newName;
    state.familyName = newName;
    saveState();
    setText("[data-active-group-name]", group.name);
    setBoardGroupNameEditing(false);
  } catch {
    alert("×©×’×™××” ×‘×©×™× ×•×™ ×©× ×”×§×‘×•×¦×”");
  } finally {
    input.disabled = false;
    if (saveButton) saveButton.disabled = false;
    if (cancelButton) cancelButton.disabled = false;
  }
}

// the currently selected group
function getActiveGroup() {
  return state.groups.find(group => group.id === state.activeGroupId) || state.groups[0] || null;
}

// am i admin of the active group?
function isCurrentUserAdminForActiveGroup() {
  const group = getActiveGroup();
  return Boolean(group) && state.user?.role === "admin" && group.userRole === "admin";
}

// show/hide every admin-only element on the current page
function applyAdminOnlyVisibility() {
  const isAdmin = isCurrentUserAdminForActiveGroup();
  document.querySelectorAll("[data-admin-only]").forEach(node => {
    node.classList.toggle("hidden", !isAdmin);
  });
  return isAdmin;
}

// --- presence (who is connected) -------------------------------------------

let presenceHeartbeatStarted = false;

// tell the server we're online now, then keep doing it on an interval
function startPresenceHeartbeat() {
  if (presenceHeartbeatStarted) return;
  presenceHeartbeatStarted = true;

  const beat = () => {
    if (document.hidden) return;
    sendPresenceHeartbeat().catch(() => {});
  };

  beat();
  const intervalId = setInterval(beat, PRESENCE_HEARTBEAT_MS);
  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

// is this member's client connected (recent heartbeat)?
function isMemberOnline(member) {
  if (!member?.lastSeenAt) return false;
  const seen = Date.parse(member.lastSeenAt);
  if (Number.isNaN(seen)) return false;
  return (Date.now() - seen) <= PRESENCE_ONLINE_THRESHOLD_MS;
}

// --- group alarm sync (broadcast + all-safe gating) ------------------------

// reflect server-known safe users onto the local emergency member list
function syncFamilyFromAlarm() {
  if (!state.alarmStatus?.active) {
    return;
  }

  const safe = new Set(state.alarmStatus?.safeUserIds || []);
  (state.familyMembers || []).forEach(member => {
    member.status = safe.has(member.id) ? "safe" : "at_risk";
  });
}

// store the latest alarm state from the server
function setAlarmStatus({ active, alarmId, mode, unlocked, safeUserIds, progress }) {
  const previous = state.alarmStatus || {};
  state.alarmStatus = {
    active: Boolean(active),
    alarmId: alarmId || previous.alarmId || "",
    mode: mode || previous.mode || "real",
    progress: Array.isArray(progress) ? progress : (previous.progress || []),
    safeUserIds: Array.isArray(safeUserIds) ? safeUserIds : [],
    unlocked: Boolean(unlocked)
  };

  const currentUserSafe = state.alarmStatus.safeUserIds.includes(state.user?.userId);

  if (state.alarmStatus.active) {
    const playbackAlarmId = state.alarmStatus.alarmId || `${getActiveGroup()?.id || "group"}:${state.alarmStatus.mode}`;
    if (currentUserSafe) {
      alarmAudio.dismiss(playbackAlarmId);
    } else {
      alarmAudio.start(state.alarmStatus.mode, playbackAlarmId);
    }
  }

  syncFamilyFromAlarm();
  saveState();
}

// pull the current alarm state for the active group from the server
async function refreshAlarmStatus() {
  const group = getActiveGroup();
  if (!group) {
    state.alarmStatus = null;
    return null;
  }

  try {
    const { alarm, safeUserIds, unlocked, progress } = await getActiveAlarm(group.id);

    if (alarm && alarm.status === "active") {
      setAlarmStatus({
        active: true,
        alarmId: alarm.id,
        mode: alarm.mode,
        progress,
        safeUserIds,
        unlocked
      });
    } else {
      state.alarmStatus = state.alarmStatus ? { ...state.alarmStatus, active: false } : null;
      saveState();
    }
  } catch {
    // leave the last known status in place on a transient error
  }

  const currentUserSafe = state.alarmStatus?.safeUserIds?.includes(state.user?.userId);
  if (state.alarmStatus?.active) {
    const playbackAlarmId = state.alarmStatus.alarmId || `${group.id}:${state.alarmStatus.mode}`;
    if (currentUserSafe) {
      alarmAudio.dismiss(playbackAlarmId);
    } else {
      alarmAudio.start(state.alarmStatus.mode, playbackAlarmId);
    }
  }

  return state.alarmStatus;
}

// are the games/trivia/mission room open? (all safe, or admin override)
function activitiesUnlocked() {
  if (state.alarmStatus?.active) {
    return Boolean(state.alarmStatus.unlocked) || allMembersSafe();
  }
  return allMembersSafe();
}

// members should move into the opened activity automatically; admins stay here
// to watch live progress, and members who finished stay here until the alarm ends.
function shouldAutoOpenActivityFromEmergency() {
  return document.body.dataset.page === "emergency" &&
    activitiesUnlocked() &&
    !isCurrentUserAdminForActiveGroup() &&
    !state.emergency?.activitiesFinished;
}

function clearActivityAutoOpen() {
  if (!emergencyActivityRedirectTimer) {
    return;
  }

  window.clearTimeout(emergencyActivityRedirectTimer);
  emergencyActivityRedirectTimer = null;
}

function scheduleActivityAutoOpen() {
  if (!shouldAutoOpenActivityFromEmergency()) {
    clearActivityAutoOpen();
    return;
  }

  if (emergencyActivityRedirectTimer) {
    return;
  }

  emergencyActivityRedirectTimer = window.setTimeout(() => {
    emergencyActivityRedirectTimer = null;

    if (shouldAutoOpenActivityFromEmergency()) {
      window.location.href = "game.html";
    }
  }, 700);
}

// report play progress to the server so the admin can watch it live (best effort)
function reportActivityProgress(activity, type, completed, total) {
  const group = getActiveGroup();
  if (!group || !activity?.id || !state.alarmStatus?.active) return;
  reportAlarmProgress(group.id, {
    activityId: activity.id,
    completed,
    total,
    type
  }).catch(() => {});
}

// draw the active group's members
function renderBoardMembers(group) {
  const container = document.querySelector("[data-group-members]");
  if (!container || !group) return;

  if (!group.members?.length) {
    container.innerHTML = `<p class="notice">אין עדיין חברים.</p>`;
    return;
  }

  container.innerHTML = group.members.map(member => {
    const isCurrentMember = state.user?.userId && String(member.id) === String(state.user.userId);
    const isOnline = isCurrentMember || isMemberOnline(member);
    const liveStatus = getOrefMemberStatus(member.id);
    const location = liveStatus?.alertLocation || member.alertLocation;
    const statusClass = orefMemberStatusClass(liveStatus, location, true);
    const statusLabel = orefMemberStatusLabel(liveStatus, location);

    return `
      <article class="member-card ${isCurrentMember ? "member-card-current" : ""} ${liveStatus?.status === "alert" ? "member-card-alert" : ""}">
        ${renderAvatarBadge(member.username, "member-avatar avatar-unity-preview", member.avatarImage)}
        <div class="member-main">
          <p class="member-name">
            <span class="presence-dot ${isOnline ? "online" : "offline"}" title="${isOnline ? "מחובר" : "לא מחובר"}"></span>
            ${escapeHtml(member.username)}
            ${isCurrentMember ? `<span class="member-me-pill">אני</span>` : ""}
          </p>
        </div>
        <span class="status-pill ${statusClass}">${escapeHtml(statusLabel)}</span>
      </article>
    `;
  }).join("");
}

// one member's live HFC status
function getOrefMemberStatus(memberId) {
  return (state.orefStatus?.members || []).find(member => member.memberId === memberId) || null;
}

// HFC member status -> css visual state
function orefMemberStatusClass(memberStatus, location = null, showLocation = false) {
  if (memberStatus?.status === "alert") return "at_risk";
  if (showLocation && (memberStatus?.status === "clear" || location?.areaName)) return "located";
  if (memberStatus?.status === "clear") return "safe";
  if (location?.areaName) return "located";
  return "offline";
}

// HFC member status -> display text
function orefMemberStatusLabel(memberStatus, location = null) {
  if (memberStatus?.status === "alert") return "אזעקה";
  const areaName = alertLocationLabel(memberStatus?.alertLocation || location);
  if (areaName) return areaName;
  return "אין אזור";
}

// best area name we have (hebrew first)
function alertLocationLabel(location) {
  return location?.areaNameHebrew || location?.areaName || "";
}

function updatePendingRequestsButton(group) {
  const count = group?.pendingRequests?.length || 0;
  const button = document.querySelector("[data-open-pending-requests]");
  const badge = document.querySelector("[data-pending-requests-count]");

  if (button) {
    button.setAttribute("aria-label", count ? `בקשות הצטרפות: ${count}` : "בקשות הצטרפות");
  }

  if (!badge) return;
  badge.textContent = String(count);
  badge.classList.toggle("hidden", count === 0);
}

// draw pending join requests on the board
function renderBoardPendingRequests(group) {
  const container = document.querySelector("[data-pending-requests-list]");
  updatePendingRequestsButton(group);
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

// draw the admin's saved games for the active group
async function renderAdminActivities(group = getActiveGroup()) {
  const container = document.querySelector("[data-admin-activity-list]");
  if (!container || !group) return;

  container.innerHTML = `<p class="notice">טוען משחקים...</p>`;

  try {
    const activities = await getGroupActivities(group.id);
    state.groupActivities = activities;
    saveState();

    if (!activities.length) {
      container.innerHTML = `
        <p class="notice">אין עדיין משחקים. צרו שאלון טריוויה או חדר משימות לקבוצה זו.</p>
        <div class="button-grid">
          <a class="btn btn-secondary" href="trivia.html">טריוויה</a>
          <a class="btn btn-secondary" href="missions.html">משימה</a>
        </div>
      `;
      return;
    }

    container.innerHTML = activities.map(activity => {
      const modes = activity.activeModes || [];
      const assigned = modes.includes("real") && modes.includes("training");
      return `
      <article class="added-item activity-admin-item">
        <div>
          <strong>${escapeHtml(activity.title)}</strong>
          <span>${escapeHtml(activity.type === "trivia" ? "טריוויה" : "חדר משימות")} - ${activityItemCount(activity)} פריטים</span>
          <span class="activity-mode-line">${assigned ? "משויך לאזעקה + תרגול" : "לא משויך"}</span>
        </div>
        <div class="activity-action-grid">
          <button class="mini-btn ${assigned ? "active safe" : ""}" type="button" data-assign-activity="${activity.id}" data-assigned="${assigned ? "1" : ""}" title="שיוך לאזעקה ולתרגול">${assigned ? "✓ משויך" : "📌 שיוך"}</button>
          <button class="mini-btn icon-btn danger" type="button" data-delete-activity="${activity.id}" data-activity-title="${escapeHtml(activity.title)}" aria-label="מחיקה" title="מחיקה">🗑</button>
        </div>
      </article>
    `;
    }).join("");

    container.querySelectorAll("[data-assign-activity]").forEach(button => {
      button.addEventListener("click", async () => {
        const activityId = button.dataset.assignActivity;
        const isAssigned = button.dataset.assigned === "1";
        await runBoardActivityAction(button, async () => {
          if (isAssigned) {
            await deactivateGroupActivity(group.id, activityId, "real");
            await deactivateGroupActivity(group.id, activityId, "training");
          } else {
            await activateGroupActivity(group.id, activityId, "real");
            await activateGroupActivity(group.id, activityId, "training");
          }
        });
      });
    });

    container.querySelectorAll("[data-delete-activity]").forEach(button => {
      button.addEventListener("click", async () => {
        const activityId = button.dataset.deleteActivity;
        const title = button.dataset.activityTitle || "המשחק הזה";
        if (!window.confirm(`למחוק את "${title}"? לא ניתן לבטל פעולה זו.`)) return;
        await runBoardActivityAction(button, async () => {
          await deleteGroupActivity(group.id, activityId);
        });
      });
    });
  } catch (error) {
    container.innerHTML = `<p class="notice warn">${escapeHtml(readableAuthError(error))}</p>`;
  }
}

// run an admin game action then refresh the list
async function runBoardActivityAction(button, action) {
  const group = getActiveGroup();
  if (!group) return;

  try {
    button.disabled = true;
    await action();
    await renderAdminActivities(group);
  } catch (error) {
    alert(readableAuthError(error));
    button.disabled = false;
  }
}

// how many items in this activity (questions or tasks)
function activityItemCount(activity) {
  if (activity.type === "trivia") {
    return activity.payload?.questions?.length || 0;
  }

  return activity.payload?.tasks?.length || 0;
}

// draw submitted results waiting on admin review
async function renderAdminActivityResults(group = getActiveGroup()) {
  const container = document.querySelector("[data-admin-results-list]");
  if (!container || !group) return;

  container.innerHTML = `<p class="notice">טוען תוצאות...</p>`;

  try {
    const results = await getGroupActivityResults(group.id);
    state.activityResults = results;
    saveState();

    if (!results.length) {
      container.innerHTML = `<p class="notice">אין עדיין תוצאות משחק.</p>`;
      return;
    }

    container.innerHTML = results.map(result => `
      <article class="added-item activity-result-item">
        <div>
          <strong>${escapeHtml(result.username)} - ${escapeHtml(result.activity?.title || "משחק")}</strong>
          <span>${escapeHtml(modeLabel(result.mode))} - ${escapeHtml(resultStatusLabel(result.status))} - ${resultSummaryText(result)}</span>
        </div>
        ${result.status === "pending" ? `
          <div class="activity-action-grid">
            <button class="mini-btn active safe" type="button" data-review-result="${result.id}" data-review-status="approved">אישור</button>
            <button class="mini-btn active at_risk" type="button" data-review-result="${result.id}" data-review-status="rejected">דחייה</button>
          </div>
        ` : ""}
      </article>
    `).join("");

    container.querySelectorAll("[data-review-result]").forEach(button => {
      button.addEventListener("click", async () => {
        try {
          button.disabled = true;
          await reviewGroupActivityResult(
            group.id,
            button.dataset.reviewResult,
            button.dataset.reviewStatus
          );
          await renderAdminActivityResults(group);
        } catch (error) {
          alert(readableAuthError(error));
          button.disabled = false;
        }
      });
    });
  } catch (error) {
    container.innerHTML = `<p class="notice warn">${escapeHtml(readableAuthError(error))}</p>`;
  }
}

// short summary text for a result
function resultSummaryText(result) {
  const payload = result.payload || {};

  if (payload.kind === "trivia") {
    return `${payload.correctCount || 0}/${payload.totalQuestions || 0} נכון`;
  }

  if (payload.kind === "mission") {
    const tasks = Array.isArray(payload.tasks) ? payload.tasks : [];
    const labels = Object.fromEntries(
      Object.entries(MISSION_GAME_DEFINITIONS).map(([id, definition]) => [id, definition.label])
    );
    const done = tasks.map(task => labels[task] || task).join(", ");
    return done ? `הושלם: ${done}` : "משימות החדר הושלמו";
  }

  return "נשלח";
}

// check the server every 15s for new join requests
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
      // ignore, next poll retries
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

// This function polls every 5s so members get pulled into an alarm the admin raised.
// It only pulls a member in while they still need to mark safe (active, not yet safe,
// not already unlocked), so members aren't yanked back after they've checked in.
function startAlarmBroadcastPolling() {
  let initialized = false;
  let wasActiveUnsafe = false;

  async function poll() {
    if (document.hidden) return;
    try {
      const group = getActiveGroup();
      if (!group) return;

      const { alarm, safeUserIds, unlocked } = await getActiveAlarm(group.id);
      const isActive = Boolean(alarm) && alarm.status === "active";
      const meSafe = (safeUserIds || []).includes(state.user?.userId);
      const shouldPull = isActive && !meSafe && !unlocked;

      if (isActive) {
        setAlarmStatus({
          active: true,
          alarmId: alarm.id,
          mode: alarm.mode,
          safeUserIds,
          unlocked
        });
      }

      if (!initialized) {
        initialized = true;
        wasActiveUnsafe = shouldPull;
        if (shouldPull) {
          clearInterval(intervalId);
          enterAlarmFromBroadcast(alarm.mode);
        }
        return;
      }

      if (shouldPull && !wasActiveUnsafe) {
        clearInterval(intervalId);
        enterAlarmFromBroadcast(alarm.mode);
      }
      wasActiveUnsafe = shouldPull;
    } catch {
      // silently ignore polling errors
    }
  }

  poll();
  const intervalId = setInterval(poll, ALARM_POLL_INTERVAL_MS);
  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

// set up local emergency state for the broadcast alarm, then show the overlay + redirect
function enterAlarmFromBroadcast(mode) {
  startEmergency(null, mode);
  saveState();
  showAlarmBroadcastOverlay(mode);
}

// reuse the drill overlay element to announce the alarm, then go to emergency.html
function showAlarmBroadcastOverlay(mode) {
  const overlay = document.querySelector("[data-drill-overlay]");
  if (overlay) {
    const heading = overlay.querySelector("h2");
    const subtitle = overlay.querySelector(".subtitle");
    if (heading) heading.textContent = mode === "training" ? "תרגול התחיל!" : "אזעקה!";
    if (subtitle) {
      subtitle.textContent = mode === "training"
        ? "המנהל הפעיל תרגול. עוברים למסך החירום..."
        : "המנהל הפעיל אזעקה. עוברים למסך החירום...";
    }
    overlay.classList.remove("hidden");
  }
  setTimeout(() => {
    window.location.href = "emergency.html";
  }, 2500);
}

// admin action: raise an alarm for the active group, then open the emergency screen.
// degrades to a local-only emergency if the alarm tables aren't set up yet.
async function raiseGroupAlarm(mode) {
  const group = getActiveGroup();
  if (!group) return;

  alarmAudio.start(mode, `pending:${group.id}:${Date.now()}`);
  startEmergency(null, mode);
  // statistics stay locked until this alarm is finished
  state.statsUnlocked = false;

  try {
    const alarm = await startAlarm(group.id, mode);
    setAlarmStatus({
      active: true,
      alarmId: alarm?.id,
      mode: alarm?.mode || mode,
      safeUserIds: [],
      unlocked: false
    });
  } catch (error) {
    console.error("startAlarm failed:", error);
    alert(readableAuthError(error));
  }

  saveState();
  window.location.href = "emergency.html";
}

// kick off auto gps alert-area matching
function initAlertLocationControls() {
  if (!state.user) {
    return;
  }

  void enableGpsAlertLocation();
}

// get gps once, save it, then start watching
async function enableGpsAlertLocation() {
  if (!navigator.geolocation) {
    renderGpsLocationStatus("איתור GPS אינו זמין בדפדפן הזה", "warn");
    return;
  }

  try {
    renderGpsLocationStatus("מאתר מיקום GPS…");
    const position = await getCurrentGpsPosition();
    await saveGpsAlertLocation(position, { force: true });
    startGpsAlertLocationWatch();
  } catch (error) {
    renderGpsLocationStatus(readableGpsError(error), "warn");
  }
}

// getCurrentPosition wrapped in a promise
function getCurrentGpsPosition() {
  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(resolve, reject, {
      enableHighAccuracy: true,
      maximumAge: 30000,
      timeout: 12000
    });
  });
}

// watch gps and update the saved area as the user moves
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

// stop the gps watch (permission revoked / page closing)
function stopGpsAlertLocationWatch() {
  if (!navigator.geolocation || orefGpsWatchId === null) {
    return;
  }

  navigator.geolocation.clearWatch(orefGpsWatchId);
  orefGpsWatchId = null;
}

// resolve a gps position to an HFC area and save it
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

    // don't spam the gateway, but still update fast when they actually move
    if (elapsed < GPS_LOCATION_SAVE_INTERVAL_MS && distance < GPS_LOCATION_DISTANCE_THRESHOLD_METERS) {
      return state.user?.alertLocation || null;
    }
  }

  renderGpsLocationStatus("מתאים GPS לאזור התרעה…");
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

  renderGpsLocationStatus(`אזור GPS: ${alertLocationLabel(alertLocation)}`, "good");
  renderBoardMembers(getActiveGroup());
  await refreshOrefStatus();
  return alertLocation;
}

// little gps status line near the location controls
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

// geolocation error -> short text
function readableGpsError(error) {
  if (error?.code === 1) return "הגישה למיקום GPS נדחתה";
  if (error?.code === 2) return "מיקום ה-GPS אינו זמין";
  if (error?.code === 3) return "בקשת ה-GPS חרגה מהזמן המוקצב";
  return error?.message || "לא ניתן להשתמש ב-GPS";
}

// distance between two gps coords (haversine)
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

// degrees -> radians
function toRadians(value) {
  return value * Math.PI / 180;
}

// update my member entry after saving a location
function updateActiveMemberAlertLocation(alertLocation) {
  const group = getActiveGroup();
  if (!group || !state.user?.userId) return;

  group.members = (group.members || []).map(member => (
    member.id === state.user.userId ? { ...member, alertLocation } : member
  ));
}

// poll live HFC status while board/emergency is open
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

// refresh live HFC status for the active group
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

function getOrefHeaderView(status = state.orefStatus, error = null) {
  if (error || !state.user?.alertLocation) {
    return {
      className: "oref-header-status-offline",
      canOpenEmergency: false,
      title: "לא מחובר לאזור התרעה"
    };
  }

  if (status?.hasGroupAlert) {
    return {
      className: "oref-header-status-danger",
      canOpenEmergency: true,
      title: "אזעקה באזור שלך"
    };
  }

  if (status?.hasActiveAlert) {
    return {
      className: "oref-header-status-warn",
      canOpenEmergency: false,
      title: "אזעקה באזור אחר"
    };
  }

  return {
    className: "oref-header-status-good",
    canOpenEmergency: false,
    title: "אין אזעקה באזור שלך"
  };
}

function renderOrefHeaderStatus(status = state.orefStatus, error = null) {
  const view = getOrefHeaderView(status, error);
  const stateClasses = [
    "oref-header-status-good",
    "oref-header-status-danger",
    "oref-header-status-warn",
    "oref-header-status-offline",
    "oref-header-status-action"
  ];

  document.querySelectorAll("[data-oref-header-status]").forEach(node => {
    node.classList.remove(...stateClasses);
    node.classList.add(view.className);
    node.classList.toggle("oref-header-status-action", view.canOpenEmergency);
    node.title = view.title;
    node.setAttribute("aria-label", `פיקוד העורף: ${view.title}`);

    if (node instanceof HTMLButtonElement) {
      node.disabled = !view.canOpenEmergency;
    }
  });
}

// draw the HFC summary on board + emergency screens
function renderOrefStatus(status = state.orefStatus, error = null) {
  const summary = document.querySelector("[data-oref-alert-summary]");
  const refreshState = document.querySelector("[data-oref-refresh-state]");
  const emergencyButtons = document.querySelectorAll("[data-open-oref-emergency]");

  renderOrefHeaderStatus(status, error);

  if (refreshState) {
    refreshState.textContent = status?.fetchedAt
      ? `נבדק ב-${new Date(status.fetchedAt).toLocaleTimeString("he-IL")}`
      : "מתחבר…";
  }

  if (summary) {
    if (error) {
      summary.textContent = error.message || "לא ניתן לבדוק את פיקוד העורף כרגע. המשיכו להסתמך על ההתרעות הרשמיות.";
      summary.className = "notice warn";
    } else if (status?.hasGroupAlert) {
      const names = status.members
        .filter(member => member.status === "alert")
        .map(member => member.username)
        .join(", ");
      const areas = status.affectedAreas.slice(0, 6).join(", ");
      summary.textContent = `התרעת פיקוד העורף אמיתית עבור ${names || "הקבוצה"}. אזורים מושפעים: ${areas || "ראו התרעה רשמית"}.`;
      summary.className = "notice danger";
    } else if (status?.hasActiveAlert) {
      summary.textContent = `קיימת התרעת פיקוד העורף פעילה מחוץ לאזורים השמורים של הקבוצה. אזורים מושפעים: ${status.affectedAreas.slice(0, 6).join(", ")}.`;
      summary.className = "notice warn";
    } else if (!state.user?.alertLocation) {
      summary.textContent = "הגדירו את אזור ההתרעה שלכם כדי שאזעקות אמת של פיקוד העורף יותאמו אליכם.";
      summary.className = "notice warn";
    } else {
      summary.textContent = "אין התרעת פיקוד העורף פעילה עבור האזורים השמורים של הקבוצה.";
      summary.className = "notice good";
    }
  }

  emergencyButtons.forEach(button => {
    if (button.matches("[data-oref-header-status]")) {
      return;
    }

    button.classList.toggle("hidden", !status?.hasGroupAlert);
  });
  renderEmergencyOrefSummary(status);
}

// real-alert summary line on the emergency screen
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

  node.textContent = `התרעת אמת של פיקוד העורף: ${affectedMembers || "חבר/ת קבוצה"} חייבים להיכנס עכשיו למרחב מוגן.`;
  node.classList.remove("hidden");
}

// a fresh blank activity draft
function createActivityDraft(type) {
  const group = getActiveGroup();

  return {
    exercises: [],
    groupId: group?.id || "",
    questions: [],
    tasks: [],
    title: type === "trivia" ? "משחק טריוויה חדש" : "חדר משימות חדש",
    type
  };
}

// get the current draft, making a new one if type/group changed
function getActivityDraft(type) {
  const group = getActiveGroup();

  if (
    !state.activityDraft ||
    state.activityDraft.type !== type ||
    state.activityDraft.groupId !== group?.id
  ) {
    state.activityDraft = createActivityDraft(type);
  }

  return state.activityDraft;
}

// clear the draft once it's saved
function clearActivityDraft(type) {
  if (state.activityDraft?.type === type) {
    state.activityDraft = null;
  }
}

// save an authored activity to the active group
async function saveActivityDraft(type, button) {
  const group = getActiveGroup();
  const draft = getActivityDraft(type);

  if (!group) {
    throw new Error("בחרו קבוצה לפני שמירת משחק");
  }

  const activity = {
    title: cleanActivityTitle(draft.title, type),
    type
  };

  if (type === "trivia") {
    activity.questions = draft.questions;

    if (!activity.questions.length) {
      throw new Error("הוסיפו לפחות שאלה אחת");
    }
  } else {
    activity.tasks = (draft.tasks || []).filter(task => MISSION_GAME_IDS.includes(task));
    activity.exercises = [];

    if (!activity.tasks.length) {
      throw new Error("בחרו לפחות משימה אחת לחדר");
    }
  }

  if (button) {
    button.disabled = true;
    button.dataset.originalText = button.dataset.originalText || button.textContent;
    button.textContent = "שומר...";
  }

  try {
    const saved = await createGroupActivity(group.id, activity);
    clearActivityDraft(type);
    saveState();
    if (button) button.textContent = "נשמר";
    window.setTimeout(() => {
      window.location.href = "board.html";
    }, 650);
    return saved;
  } catch (error) {
    if (button) {
      button.disabled = false;
      button.textContent = button.dataset.originalText || "שמירה";
    }

    throw error;
  }
}

// fallback title if empty
function cleanActivityTitle(title, type) {
  const cleanTitle = String(title || "").trim();
  if (cleanTitle) return cleanTitle;
  return type === "trivia" ? "משחק טריוויה" : "חדר משימות";
}

// set up the trivia question builder
function initTrivia() {
  const draft = getActivityDraft("trivia");
  const titleInput = document.querySelector("[data-activity-title]");

  if (titleInput) {
    titleInput.value = draft.title;
    titleInput.addEventListener("input", () => {
      draft.title = titleInput.value;
      saveState();
    });
  }

  document.querySelector("[data-save-questions]")?.addEventListener("click", async event => {
    event.stopImmediatePropagation();
    try {
      await saveActivityDraft("trivia", event.currentTarget);
    } catch (error) {
      alert(readableAuthError(error));
    }
  }, true);

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
    draft.questions.push(question);
    saveState();
    renderQuestionList();
  });

  document.querySelector("[data-save-questions]")?.addEventListener("click", event => {
    event.currentTarget.textContent = "השאלון נשמר מקומית";
  });
  document.querySelector("[data-save-questions]")?.addEventListener("click", async event => {
    try {
      await saveActivityDraft("trivia", event.currentTarget);
    } catch (error) {
      alert(readableAuthError(error));
    }
  });
}

// set up the mission-room builder: pick the new room mini-games
function initMissions() {
  const draft = getActivityDraft("mission");
  draft.tasks = Array.isArray(draft.tasks)
    ? draft.tasks.filter(task => MISSION_GAME_IDS.includes(task))
    : [];
  draft.exercises = [];

  const titleInput = document.querySelector("[data-activity-title]");
  if (titleInput) {
    titleInput.value = draft.title;
    titleInput.addEventListener("input", () => {
      draft.title = titleInput.value;
      saveState();
    });
  }

  document.querySelectorAll("[data-task]").forEach(checkbox => {
    const task = checkbox.dataset.task;
    checkbox.checked = draft.tasks.includes(task);
    checkbox.addEventListener("change", () => {
      if (checkbox.checked) {
        if (!draft.tasks.includes(task)) draft.tasks.push(task);
      } else {
        draft.tasks = draft.tasks.filter(item => item !== task);
      }
      saveState();
    });
  });

  document.querySelector("[data-save-missions]")?.addEventListener("click", async event => {
    try {
      await saveActivityDraft("mission", event.currentTarget);
    } catch (error) {
      alert(readableAuthError(error));
    }
  });
}

// set up the practice flow
function initPractice() {
  startPresenceHeartbeat();
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

// set up the emergency check-in flow
async function initEmergency() {
  try {
    await loadSessionIntoState();
    saveState();
  } catch {
    // if the refresh fails we just use the locally stored event
  }

  // sync with the server alarm before deciding what to render
  await refreshAlarmStatus();

  if (!state.emergency?.active) {
    startEmergency(state.orefStatus, state.alarmStatus?.active ? state.alarmStatus.mode : null);
  }

  syncFamilyFromAlarm();
  startPresenceHeartbeat();
  applyAdminOnlyVisibility();

  renderOrefStatus();
  renderEmergency();
  refreshOrefStatus();
  startOrefStatusPolling();

  if (state.alarmStatus?.active) {
    // admin: learn how many games are active so we can tell when everyone is done
    if (isCurrentUserAdminForActiveGroup()) {
      try {
        const group = getActiveGroup();
        const activeActivities = group ? await getActiveGroupActivities(group.id, state.alarmStatus.mode) : [];
        adminExpectedActivityCount = (activeActivities || []).length;
      } catch {
        adminExpectedActivityCount = 0;
      }
    }
    startEmergencyAlarmPolling();
  }

  document.querySelector("[data-emergency-safe]")?.addEventListener("click", () => {
    void handleEmergencySafeClick();
  });

  // iOS won't auto-play the siren when the app is opened from a notification tap,
  // so show a full-screen prompt; the member's first tap sounds the alarm.
  const alarmSoundGate = document.querySelector("[data-alarm-sound-gate]");
  if (alarmSoundGate) {
    const syncSoundGate = blocked => {
      alarmSoundGate.classList.toggle("hidden", !blocked);
    };
    document.addEventListener("saferAlarmAudioState", event => {
      syncSoundGate(Boolean(event.detail?.blocked));
    });
    // the document-level pointerdown handler in alarmAudio retries playback on tap;
    // hide the prompt right away so it doesn't sit over the "אני מוגן" button.
    alarmSoundGate.addEventListener("click", () => alarmSoundGate.classList.add("hidden"));
    syncSoundGate(alarmAudio.isBlocked());
  }

  // admin: open the activities for everyone now (override the all-safe gate)
  document.querySelector("[data-alarm-unlock]")?.addEventListener("click", async () => {
    const group = getActiveGroup();
    if (!group) return;
    try {
      const result = await unlockAlarm(group.id);
      setAlarmStatus({
        active: true,
        mode: state.alarmStatus?.mode,
        safeUserIds: result.safeUserIds,
        unlocked: true
      });
      renderEmergency();
    } catch (error) {
      alert(readableAuthError(error));
    }
  });

  // admin: end the alarm and go straight to the statistics page
  document.querySelector("[data-alarm-end]")?.addEventListener("click", async () => {
    const group = getActiveGroup();
    if (group) {
      try {
        await endAlarm(group.id);
      } catch {
        // best effort
      }
    }
    state.alarmStatus = null;
    state.statsUnlocked = true;
    saveState();
    window.location.href = "statistics.html";
  });
}

// the user taps "I'm safe": open activities if unlocked, else record safe (server-backed)
async function handleEmergencySafeClick() {
  if (activitiesUnlocked()) {
    window.location.href = "game.html";
    return;
  }

  alarmAudio.dismiss(state.alarmStatus?.alarmId);
  markMemberSafe(currentFamilyMemberId());
  if (state.emergency) {
    state.emergency.telemetry.safeClickTime = secondsSince(state.emergency.startedAt);
    state.emergency.telemetry.tapCount += 1;
  }
  saveState();
  renderEmergency();

  const group = getActiveGroup();

  if (group && state.alarmStatus?.active) {
    try {
      const result = await markAlarmSafe(group.id);
      setAlarmStatus({
        active: true,
        mode: state.alarmStatus?.mode,
        safeUserIds: result.safeUserIds,
        unlocked: result.unlocked
      });
      renderEmergency();
      if (activitiesUnlocked()) {
        window.location.href = "game.html";
      }
    } catch (error) {
      console.error("markAlarmSafe failed:", error);
    }
    return;
  }

  // no group alarm running -> keep the local demo behavior
  if (state.emergency?.trigger !== "pikud_haoref") {
    simulateFamilyCheckIns();
  }
}

// poll the server alarm while the emergency screen is open so the member list,
// unlock state, and "alarm ended" all stay in sync across the group.
function startEmergencyAlarmPolling() {
  let redirectingToStats = false;
  const intervalId = setInterval(async () => {
    if (document.hidden) return;
    const status = await refreshAlarmStatus();
    if (!status?.active) {
      clearInterval(intervalId);
      window.location.href = "board.html";
      return;
    }
    renderEmergency();

    // admin: the moment every member has finished all the games, end the alarm
    // (releasing the waiting members) and open the statistics page
    if (
      !redirectingToStats &&
      isCurrentUserAdminForActiveGroup() &&
      activitiesUnlocked() &&
      allMembersFinishedActivities()
    ) {
      redirectingToStats = true;
      clearInterval(intervalId);
      const group = getActiveGroup();
      try {
        if (group) await endAlarm(group.id);
      } catch {
        // best effort — open stats anyway
      }
      state.alarmStatus = null;
      state.statsUnlocked = true;
      saveState();
      window.location.href = "statistics.html";
    }
  }, ALARM_POLL_INTERVAL_MS);

  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

// start the unlocked activity game
async function initGame() {
  try {
    await loadSessionIntoState();
    saveState();
  } catch {
  }

  await refreshAlarmStatus();
  startPresenceHeartbeat();
  startGamePageAlarmPolling();
  window.addEventListener("pagehide", () => {
    gameAudio.shutdown();
    stopUnityMissionRoom();
  }, { once: true });
  await renderGame();
}

// while a member is mid-game, send them back if the admin ends the alarm
function startGamePageAlarmPolling() {
  const intervalId = setInterval(async () => {
    if (document.hidden) return;
    const status = await refreshAlarmStatus();
    if (!status?.active) {
      clearInterval(intervalId);
      window.location.href = "board.html";
    }
  }, ALARM_POLL_INTERVAL_MS);

  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

// draw the emergency summary screen
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
          <p class="member-role">זמן אישור: ${report.checkInTime}</p>
          <p class="member-role">פעילות: ${report.participation}</p>
          <p class="member-role">נכון: ${report.correct} | טעויות: ${report.mistakes}</p>
        </div>
        <span class="stress-level ${stressClass(report.stressLevel)}">${stressLabel(report.stressLevel)}</span>
      </article>
    `;
  }).join("");
}

// set up the report member dropdown
function initReport() {
  const select = document.querySelector("[data-report-member]");
  if (!select) return;

  select.innerHTML = state.familyMembers.map(member => (
    `<option value="${member.id}">${escapeHtml(member.name)}</option>`
  )).join("");

  select.addEventListener("change", () => renderReport(select.value));
  renderReport(select.value || state.familyMembers[0]?.id);
}

// ---- admin statistics page (per-user performance charts) ----
const STATS_COLORS = { current: "#0b1220", real: "#e63f4f", training: "#4b8ff0" };
const statsState = { activityId: "", charts: {}, data: null, memberId: "" };
const STATS_PDF_CHARTS = [
  { empty: "אין נתוני זמן להצגה בגרף זה.", key: "time", title: "זמן לכל שאלה / משימה" },
  { empty: "אין נתוני תשובות להצגה בגרף זה.", key: "pie", title: "תשובות נכונות מול שגויות" },
  { empty: "אין נתוני טעויות או פגיעות להצגה בגרף זה.", key: "mistakes", title: "טעויות / פגיעות" },
  { empty: "אין נתוני סיבוב יד להצגה בגרף זה.", key: "rotation", title: "סיבוב היד" }
];
const STATS_PDF_PRINT_STYLE = `
  body { margin: 0; background: #ffffff; color: #111827; font-family: Heebo, Arial, sans-serif; direction: rtl; }
  .stats-pdf-report { box-sizing: border-box; width: 100%; max-width: 820px; margin: 0 auto; padding: 32px; }
  .stats-pdf-report h1 { margin: 0 0 8px; font-size: 30px; }
  .stats-pdf-report h2 { margin: 26px 0 10px; font-size: 20px; }
  .stats-pdf-report h3 { margin: 0 0 10px; font-size: 16px; }
  .stats-pdf-meta { color: #4b5563; font-size: 13px; margin: 0; }
  .stats-pdf-summary { white-space: pre-wrap; line-height: 1.7; border: 1px solid #d1d5db; border-radius: 10px; padding: 14px; }
  .stats-pdf-activity { margin-top: 22px; }
  .stats-pdf-chart { border: 1px solid #d1d5db; border-radius: 10px; padding: 12px; margin-top: 14px; break-inside: avoid; page-break-inside: avoid; }
  .stats-pdf-chart img { display: block; width: 100%; height: auto; }
  .stats-pdf-empty { margin: 0; color: #6b7280; }
`;

function round1(value) {
  return Math.round(Number(value) * 10) / 10;
}

// load the group's stats and render the per-member dropdown + charts
async function initStatistics() {
  // the statistics page only opens for the admin once an alarm has been finished
  if (!state.statsUnlocked) {
    window.location.href = "board.html";
    return;
  }

  const status = document.querySelector("[data-stats-status]");
  const group = getActiveGroup();

  if (!group) {
    setStatsStatus(status, "לא נבחרה קבוצה.", "warn");
    return;
  }

  try {
    const data = await getGroupStatistics(group.id);
    statsState.data = data;
    const members = data?.members || [];
    const activities = data?.activities || [];

    if (!activities.length) {
      setStatsStatus(status, "עדיין אין משחקים בקבוצה זו.", "");
      return;
    }

    if (!members.length) {
      setStatsStatus(status, "עדיין אין משתתפים בקבוצה זו.", "");
      return;
    }

    status?.classList.add("hidden");
    statsState.memberId = members[0].userId;
    statsState.activityId = activities[0].id;

    renderStatsMemberSelect();
    renderStatsActivitySelect();
    renderStatsCharts();
    wireStatsSummary();
    wireStatsPdfExport();
    void maybeShowStatsEndAlarm();
  } catch (error) {
    setStatsStatus(status, readableAuthError(error), "warn");
  }
}

// if a live alarm is still running, let the admin end it from here (this
// releases members who are waiting on the alarm screen)
async function maybeShowStatsEndAlarm() {
  const button = document.querySelector("[data-stats-end-alarm]");
  const group = getActiveGroup();
  if (!button || !group) return;

  let active = false;
  try {
    const { alarm } = await getActiveAlarm(group.id);
    active = Boolean(alarm) && alarm.status === "active";
  } catch {
    active = false;
  }

  button.classList.toggle("hidden", !active);
  if (!active) return;

  button.addEventListener("click", async () => {
    button.disabled = true;
    try {
      await endAlarm(group.id);
      state.alarmStatus = null;
      saveState();
      button.classList.add("hidden");
    } catch (error) {
      alert(readableAuthError(error));
      button.disabled = false;
    }
  }, { once: true });
}

function setStatsStatus(node, message, tone) {
  if (!node) return;
  node.textContent = message;
  node.className = `notice ${tone}`.trim();
  node.classList.remove("hidden");
}

// member picker (dropdown of everyone in the group)
function renderStatsMemberSelect() {
  const wrap = document.querySelector("[data-stats-member-wrap]");
  const select = document.querySelector("[data-stats-member]");
  if (!wrap || !select) return;

  wrap.hidden = false;
  select.innerHTML = (statsState.data?.members || []).map(member => (
    `<option value="${escapeHtml(member.userId)}">${escapeHtml(member.username)}</option>`
  )).join("");
  select.value = statsState.memberId;

  select.addEventListener("change", () => {
    statsState.memberId = select.value;
    resetStatsSummary();
    renderStatsCharts();
  });
}

// game (activity) picker
function renderStatsActivitySelect() {
  const wrap = document.querySelector("[data-stats-activity-wrap]");
  const select = document.querySelector("[data-stats-activity]");
  if (!wrap || !select) return;

  wrap.hidden = false;
  select.innerHTML = (statsState.data?.activities || []).map(activity => {
    const typeLabel = activity.type === "mission" ? "חדר משימות" : "טריוויה";
    return `<option value="${escapeHtml(activity.id)}">${escapeHtml(activity.title || typeLabel)} · ${typeLabel}</option>`;
  }).join("");
  select.value = statsState.activityId;

  select.addEventListener("change", () => {
    statsState.activityId = select.value;
    renderStatsCharts();
  });
}

// (re)draw the three charts for the selected member + activity
function renderStatsCharts() {
  const data = statsState.data;
  if (!data) return;

  document.querySelector("[data-stats-charts]")?.removeAttribute("hidden");

  const activity = (data.activities || []).find(item => item.id === statsState.activityId);
  if (!activity) return;

  const items = activity.items || [];
  const labels = items.map(item => item.label);

  // average for a metric+mode, aligned to the activity's canonical item order
  const aggSeries = (metric, mode, targetItems = items) => {
    const byIndex = new Map();
    (data.aggregates || [])
      .filter(row => row.activityId === activity.id && row.metric === metric && row.mode === mode)
      .forEach(row => byIndex.set(row.itemIndex, row.avgValue));
    return targetItems.map(item => (byIndex.has(item.index) ? round1(byIndex.get(item.index)) : null));
  };

  // this member's latest result for this activity (black series + pie)
  const latest = (data.latestResults || []).find(row => (
    row.userId === statsState.memberId && row.activityId === activity.id
  ));
  const latestByIndex = new Map();
  (latest?.items || []).forEach(item => {
    if (Number.isInteger(item.index)) latestByIndex.set(item.index, item);
  });
  const currentSeries = (key, targetItems = items) => targetItems.map(item => {
    const value = latestByIndex.get(item.index)?.[key];
    return typeof value === "number" ? round1(value) : null;
  });

  // 1) time per item
  const timeItems = items.filter(item => !isMissileStatsItem(item));
  drawSeriesChart("time", "bar", timeItems.map(item => item.label), {
    current: currentSeries("timeSeconds", timeItems),
    real: aggSeries("time", "real", timeItems),
    training: aggSeries("time", "training", timeItems)
  });

  // 2) correct vs wrong
  drawPieChart(activity, latest);

  // 3) mistakes / hits per item
  const mistakes = {
    current: currentSeries("mistakes"),
    real: aggSeries("mistakes", "real"),
    training: aggSeries("mistakes", "training")
  };
  const hasMistakes = [mistakes.current, mistakes.real, mistakes.training]
    .some(series => series.some(value => value !== null));

  if (hasMistakes) {
    toggleStatsEmpty("[data-mistakes-empty]", "[data-chart-mistakes]", true);
    drawSeriesChart("mistakes", "bar", labels, mistakes);
  } else {
    destroyStatsChart("mistakes");
    toggleStatsEmpty("[data-mistakes-empty]", "[data-chart-mistakes]", false);
  }

  // 4) hand rotation per item
  const rotation = {
    current: currentSeries("rotation"),
    real: aggSeries("rotation", "real"),
    training: aggSeries("rotation", "training")
  };
  const hasRotation = [rotation.current, rotation.real, rotation.training]
    .some(series => series.some(value => value !== null));

  if (hasRotation) {
    toggleStatsEmpty("[data-rotation-empty]", "[data-chart-rotation]", true);
    drawSeriesChart("rotation", "line", labels, rotation);
  } else {
    destroyStatsChart("rotation");
    toggleStatsEmpty("[data-rotation-empty]", "[data-chart-rotation]", false);
  }
}

function isMissileStatsItem(item) {
  const label = String(item?.label || "").trim().toLowerCase();
  return item?.game === "missile" || label === "טילים" || label.includes("missile");
}

function destroyStatsChart(key) {
  if (statsState.charts[key]) {
    statsState.charts[key].destroy();
    statsState.charts[key] = null;
  }
}

// shared style for the 3-series bar/line charts (red/blue/black)
function statsDataset(seriesKey, label, values, type) {
  const color = STATS_COLORS[seriesKey];

  if (type === "line") {
    return {
      backgroundColor: color,
      borderColor: seriesKey === "current" ? "rgba(248,250,252,0.95)" : color,
      borderWidth: 2,
      data: values,
      label,
      pointBackgroundColor: seriesKey === "current" ? "#0b1220" : color,
      pointBorderColor: "rgba(255,255,255,0.6)",
      pointRadius: 3,
      spanGaps: true,
      tension: 0.3
    };
  }

  return {
    backgroundColor: color,
    borderColor: seriesKey === "current" ? "rgba(255,255,255,0.75)" : color,
    borderWidth: seriesKey === "current" ? 1.5 : 1,
    data: values,
    label
  };
}

function drawSeriesChart(key, type, labels, series) {
  const canvas = document.querySelector(`[data-chart-${key}]`);
  if (!canvas || typeof Chart === "undefined") return;

  destroyStatsChart(key);
  canvas.classList.remove("hidden");

  statsState.charts[key] = new Chart(canvas, {
    data: {
      datasets: [
        statsDataset("real", "אזעקת אמת", series.real, type),
        statsDataset("training", "תרגול", series.training, type),
        statsDataset("current", "נוכחי", series.current, type)
      ],
      labels
    },
    options: {
      animation: false,
      maintainAspectRatio: false,
      plugins: { legend: { display: false } },
      responsive: true,
      scales: {
        x: { grid: { color: "rgba(255,255,255,0.08)" }, ticks: { color: "#9fb0c7" } },
        y: { beginAtZero: true, grid: { color: "rgba(255,255,255,0.08)" }, ticks: { color: "#9fb0c7" } }
      }
    },
    type
  });
}

function drawPieChart(activity, latest) {
  const canvas = document.querySelector("[data-chart-pie]");
  const empty = document.querySelector("[data-pie-empty]");
  if (!canvas || typeof Chart === "undefined") return;

  destroyStatsChart("pie");

  const scored = (latest?.items || []).filter(item => typeof item.correct === "boolean");
  const correct = scored.filter(item => item.correct).length;
  const wrong = scored.length - correct;

  if (!scored.length) {
    canvas.classList.add("hidden");
    if (empty) {
      empty.textContent = activity.type === "trivia"
        ? "אין עדיין נתוני תשובות עבור משתתף זה."
        : "אין עדיין נתוני הצלחה/טעויות עבור משחק זה.";
      empty.classList.remove("hidden");
    }
    return;
  }

  canvas.classList.remove("hidden");
  empty?.classList.add("hidden");

  statsState.charts.pie = new Chart(canvas, {
    data: {
      datasets: [{
        backgroundColor: ["#29b36a", "#e63f4f"],
        borderColor: "rgba(0,0,0,0.25)",
        borderWidth: 1,
        data: [correct, wrong]
      }],
      labels: ["נכון", "שגוי"]
    },
    options: {
      animation: false,
      maintainAspectRatio: false,
      plugins: { legend: { labels: { color: "#f8fafc" } } },
      responsive: true
    },
    type: "pie"
  });
}

function toggleStatsEmpty(emptySelector, canvasSelector, hasData) {
  document.querySelector(emptySelector)?.classList.toggle("hidden", hasData);
  document.querySelector(canvasSelector)?.classList.toggle("hidden", !hasData);
}

// clear any AI summary shown for the previously selected member
function resetStatsSummary() {
  const text = document.querySelector("[data-stats-summary-text]");
  const status = document.querySelector("[data-stats-summary-status]");
  if (text) {
    text.textContent = "";
    delete text.dataset.memberId;
    text.classList.add("hidden");
  }
  status?.classList.add("hidden");
}

function currentStatsSummaryText() {
  const text = document.querySelector("[data-stats-summary-text]");
  if (!text || text.classList.contains("hidden")) return "";
  if (text.dataset.memberId && text.dataset.memberId !== statsState.memberId) return "";
  return text.textContent.trim();
}

async function generateStatsSummary() {
  const group = getActiveGroup();
  if (!group || !statsState.memberId) {
    throw new Error("אין נתוני משתתף לניתוח.");
  }

  const status = document.querySelector("[data-stats-summary-status]");
  const text = document.querySelector("[data-stats-summary-text]");

  text?.classList.add("hidden");
  setStatsStatus(status, "מנתח את המדידות האמיתיות...", "");

  const result = await getUserStatsSummary(group.id, statsState.memberId);
  const summary = result?.summary || "לא התקבל סיכום.";
  status?.classList.add("hidden");

  if (text) {
    text.textContent = summary;
    text.dataset.memberId = statsState.memberId;
    text.classList.remove("hidden");
  }

  return summary;
}

// hook up the "generate AI summary" button: send the selected member's
// measurements to Groq (via the gateway) and show the returned situation summary
function wireStatsSummary() {
  const button = document.querySelector("[data-stats-summary-btn]");
  if (!button || button.dataset.wired === "1") return;
  button.dataset.wired = "1";

  button.addEventListener("click", async () => {
    const group = getActiveGroup();
    if (!group || !statsState.memberId) return;

    const status = document.querySelector("[data-stats-summary-status]");

    button.disabled = true;

    try {
      await generateStatsSummary();
    } catch (error) {
      setStatsStatus(status, readableAuthError(error), "warn");
    } finally {
      button.disabled = false;
    }
  });
}

function wireStatsPdfExport() {
  const button = document.querySelector("[data-stats-pdf-btn]");
  if (!button || button.dataset.wired === "1") return;
  button.dataset.wired = "1";

  button.addEventListener("click", async () => {
    const status = document.querySelector("[data-stats-summary-status]");
    const summaryButton = document.querySelector("[data-stats-summary-btn]");

    button.disabled = true;
    if (summaryButton) summaryButton.disabled = true;

    try {
      const summary = currentStatsSummaryText() || await generateStatsSummary();
      setStatsStatus(status, "מכין PDF עם הסיכום והגרפים...", "");
      await downloadStatsPdfReport(summary);
      setStatsStatus(status, "ה-PDF נוצר בהצלחה.", "good");
    } catch (error) {
      setStatsStatus(status, readableAuthError(error), "warn");
    } finally {
      button.disabled = false;
      if (summaryButton) summaryButton.disabled = false;
    }
  });
}

async function downloadStatsPdfReport(summary) {
  if (!statsState.data || !statsState.memberId || !statsState.activityId) {
    throw new Error("אין נתונים זמינים ליצירת PDF.");
  }

  const activityChartGroups = await collectStatsPdfActivityChartGroups();
  const report = buildStatsPdfReport(summary, activityChartGroups);
  document.body.appendChild(report);

  try {
    await nextAnimationFrame();

    if (typeof window.html2pdf === "function") {
      await window.html2pdf()
        .set({
          filename: statsPdfFilename(),
          html2canvas: {
            backgroundColor: "#ffffff",
            scale: 2,
            useCORS: true
          },
          image: { quality: 0.98, type: "jpeg" },
          jsPDF: { format: "a4", orientation: "portrait", unit: "mm" },
          margin: [8, 8, 8, 8],
          pagebreak: { mode: ["avoid-all", "css", "legacy"] }
        })
        .from(report)
        .save();
      return;
    }

    printStatsPdfReport(report);
  } finally {
    report.remove();
  }
}

async function collectStatsPdfActivityChartGroups() {
  const activities = statsState.data?.activities || [];
  const originalActivityId = statsState.activityId;
  const activitySelect = document.querySelector("[data-stats-activity]");
  const groups = [];

  try {
    for (const activity of activities) {
      statsState.activityId = activity.id;
      if (activitySelect) activitySelect.value = activity.id;
      renderStatsCharts();
      await nextAnimationFrame();

      groups.push({
        activity,
        charts: STATS_PDF_CHARTS.map(chart => ({
          ...chart,
          image: statsChartImage(chart.key)
        }))
      });
    }
  } finally {
    statsState.activityId = originalActivityId;
    if (activitySelect) activitySelect.value = originalActivityId;
    renderStatsCharts();
  }

  return groups;
}

function buildStatsPdfReport(summary, activityChartGroups = []) {
  const member = selectedStatsMember();
  const group = getActiveGroup();
  const generatedAt = new Date().toLocaleString("he-IL", { dateStyle: "short", timeStyle: "short" });
  const chartMarkup = activityChartGroups.map(grouped => {
    const charts = grouped.charts.map(chart => {
      const body = chart.image
        ? `<img src="${escapeHtml(chart.image)}" alt="${escapeHtml(chart.title)}">`
        : `<p class="stats-pdf-empty">${escapeHtml(chart.empty)}</p>`;

      return `
        <section class="stats-pdf-chart">
          <h3>${escapeHtml(chart.title)}</h3>
          ${body}
        </section>
      `;
    }).join("");

    const typeLabel = grouped.activity?.type === "mission" ? "חדר משימות" : "טריוויה";
    return `
      <section class="stats-pdf-activity">
        <h2>${escapeHtml(grouped.activity?.title || typeLabel)} · ${typeLabel}</h2>
        ${charts}
      </section>
    `;
  }).join("");

  const report = document.createElement("article");
  report.className = "stats-pdf-report";
  report.setAttribute("dir", "rtl");
  report.innerHTML = `
    <header>
      <h1>דוח ביצועים וניתוח AI</h1>
      <p class="stats-pdf-meta">קבוצה: ${escapeHtml(group?.name || "לא ידוע")} · משתתף: ${escapeHtml(member?.username || "לא ידוע")}</p>
      <p class="stats-pdf-meta">משחקים בדוח: ${activityChartGroups.length} · נוצר: ${escapeHtml(generatedAt)}</p>
    </header>
    <section>
      <h2>סיכום AI</h2>
      <div class="stats-pdf-summary">${escapeHtml(summary).replaceAll("\n", "<br>")}</div>
    </section>
    <section>
      <h2>כל הגרפים</h2>
      ${chartMarkup}
    </section>
  `;

  return report;
}

function selectedStatsMember() {
  return (statsState.data?.members || []).find(member => member.userId === statsState.memberId) || null;
}

function selectedStatsActivity() {
  return (statsState.data?.activities || []).find(activity => activity.id === statsState.activityId) || null;
}

function nextAnimationFrame() {
  return new Promise(resolve => {
    const requestFrame = typeof requestAnimationFrame === "function"
      ? requestAnimationFrame
      : callback => window.setTimeout(callback, 0);
    requestFrame(resolve);
  });
}

function statsChartImage(key) {
  const chart = statsState.charts[key];
  if (!chart || chart.canvas?.classList.contains("hidden")) return "";

  if (typeof chart.toBase64Image === "function") {
    const image = chart.toBase64Image("image/png", 1);
    if (image && image !== "data:,") return image;
  }

  return typeof chart.canvas?.toDataURL === "function"
    ? chart.canvas.toDataURL("image/png")
    : "";
}

function statsPdfFilename() {
  const member = selectedStatsMember();
  const memberName = safeFilenamePart(member?.username || "member");
  return `SaferTogether-${memberName}-statistics.pdf`;
}

function safeFilenamePart(value) {
  return String(value || "report")
    .trim()
    .replace(/[\\/:*?"<>|]+/g, "-")
    .replace(/\s+/g, "-")
    .slice(0, 60) || "report";
}

function printStatsPdfReport(report) {
  const printWindow = window.open("", "_blank");
  if (!printWindow) {
    throw new Error("לא ניתן לפתוח חלון הדפסה. בדקו שחוסם חלונות קופצים לא פעיל.");
  }

  printWindow.document.write(`
    <!DOCTYPE html>
    <html lang="he" dir="rtl">
    <head>
      <meta charset="UTF-8">
      <title>SaferTogether PDF</title>
      <style>${STATS_PDF_PRINT_STYLE}</style>
    </head>
    <body>
      <article class="stats-pdf-report">${report.innerHTML}</article>
    </body>
    </html>
  `);
  printWindow.document.close();
  printWindow.focus();
  printWindow.print();
}

// draw the local family member cards
function renderFamilyList(container) {
  if (!container) return;

  container.innerHTML = state.familyMembers.map(member => `
    <article class="member-card">
      ${renderAvatarBadge(member.name, `avatar-badge ${member.status}`, member.avatarImage)}
      <div class="member-main">
        <p class="member-name">${escapeHtml(member.name)}</p>
        <p class="member-role">${escapeHtml(member.alertLocation?.areaName || member.role)}</p>
      </div>
      <span class="status-pill ${member.status}">${statusLabel(member.status)}</span>
    </article>
  `).join("");
}

// draw the saved trivia questions
function renderQuestionList() {
  const container = document.querySelector("[data-question-list]");
  if (!container) return;
  const questions = state.activityDraft?.type === "trivia"
    ? state.activityDraft.questions
    : state.questions;

  if (!questions.length) {
    container.innerHTML = `<p class="notice">אין עדיין שאלות.</p>`;
    return;
  }

  container.innerHTML = questions.map((question, index) => `
    <article class="added-item">
      <strong>${index + 1}. ${escapeHtml(question.question)}</strong>
      <span>${escapeHtml(question.answers[question.correctAnswerIndex])} מסומנת כתשובה נכונה.</span>
    </article>
  `).join("");
}

// draw the current practice question
function renderPracticeQuestion() {
  const container = document.querySelector("[data-practice-question]");
  const question = state.questions[0] || DEFAULT_QUESTIONS[0];
  if (!container) return;

  container.innerHTML = `
    <h2>שאלת תרגול</h2>
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

      // always show the correct answer in green; mark a wrong pick red
      container.querySelectorAll("[data-practice-answer]").forEach(item => {
        const itemIndex = Number(item.dataset.practiceAnswer);
        if (itemIndex === question.correctAnswerIndex) item.classList.add("correct");
        else if (itemIndex === answerIndex) item.classList.add("wrong");
        item.disabled = true;
      });
      const feedback = container.querySelector("[data-practice-feedback]");
      feedback.textContent = correct ? "כל הכבוד. תשובה נכונה." : "ניסיון טוב. נתרגל את זה שוב.";
      feedback.classList.remove("hidden");
    });
  });
}

// save the practice baseline and show the summary
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
    <h2>האימון הסתיים</h2>
    <div class="comparison-grid">
      <div class="comparison-box"><span>זמן תשובה ממוצע</span><strong>${state.baseline.averageAnswerTime}s</strong></div>
      <div class="comparison-box"><span>תשובות נכונות</span><strong>${answers.length - mistakes}</strong></div>
      <div class="comparison-box"><span>טעויות</span><strong>${mistakes}</strong></div>
      <div class="comparison-box"><span>תנועה מדומה</span><strong>${state.baseline.averageMovementLevel}</strong></div>
    </div>
    <p class="notice good">נתוני הבסיס נשמרו מקומית להשוואה בזמן אמת.</p>
    <a class="btn btn-primary" href="board.html">חזרה ללוח</a>
  `;
  summary.classList.remove("hidden");
}

// start emergency mode + reset telemetry
function startEmergency(orefStatus = null, trigger = null) {
  const cleanTrigger = trigger || (orefStatus?.hasGroupAlert ? "pikud_haoref" : "real");

  state.familyMembers = buildEmergencyMembers(orefStatus);
  state.emergency = {
    active: true,
    trigger: cleanTrigger,
    activityMode: cleanTrigger === "training" ? "training" : "real",
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
    missionIndex: 0,
    missionResults: [],
    activityStartedAt: null,
    activityAnswer: null,
    activityQueue: [],
    activityQueueIndex: 0,
    submittedResults: {},
    triviaAnswers: [],
    triviaIndex: 0
  };
  saveState();
}

// build the emergency member list from the group + live alert state
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
      avatarImage: member.avatarImage,
      id: member.id,
      name: member.username,
      role: member.role === "admin" ? "מנהל/ת" : "חבר/ה",
      status
    };
  });
}

// my member id in the emergency list
function currentFamilyMemberId() {
  return state.user?.userId || "1";
}

// draw the emergency check-in state
function renderEmergency() {
  renderFamilyList(document.querySelector("[data-emergency-family]"));
  renderEmergencyOrefSummary();
  toggleAlarmAdminControls();
  renderAdminProgress();
  const isAdmin = isCurrentUserAdminForActiveGroup();
  const button = document.querySelector("[data-emergency-safe]");
  const message = document.querySelector("[data-emergency-message]");
  const current = state.familyMembers.find(member => member.id === currentFamilyMemberId());

  if (!button || !message) return;

  button.classList.remove("hidden");

  if (activitiesUnlocked()) {
    // the admin doesn't play — they watch the group's live progress instead
    if (isAdmin) {
      clearActivityAutoOpen();
      button.classList.add("hidden");
      message.textContent = "הפעילויות פתוחות. מעקב אחר התקדמות הקבוצה:";
      message.className = "notice good";
      return;
    }
    // member who finished all the games: wait here until the admin ends the alert
    if (state.emergency?.activitiesFinished) {
      clearActivityAutoOpen();
      button.classList.add("hidden");
      message.textContent = "סיימת את כל הפעילויות. ממתין שהמנהל יסיים את האזעקה...";
      message.className = "notice good";
      return;
    }
    button.classList.add("hidden");
    button.disabled = true;
    message.textContent = "טוען פעילות...";
    message.className = "notice good";
    scheduleActivityAutoOpen();
    return;
  }

  clearActivityAutoOpen();

  if (current?.status === "safe") {
    button.textContent = "אישור מוגן נשלח";
    button.disabled = true;
    message.textContent = "ממתין לאישור כל חברי הקבוצה...";
    message.className = "notice warn";
    return;
  }

  button.textContent = "אני מוגן!";
  button.disabled = false;
  message.textContent = "כולם מסומנים בסיכון עד לאישור.";
  message.className = "notice danger";
}

// show the admin override / end controls only to an admin during a live alarm
function toggleAlarmAdminControls() {
  const isAdmin = isCurrentUserAdminForActiveGroup();
  const alarmActive = Boolean(state.alarmStatus?.active);
  const unlockButton = document.querySelector("[data-alarm-unlock]");
  const endButton = document.querySelector("[data-alarm-end]");

  if (unlockButton) {
    unlockButton.classList.toggle("hidden", !(isAdmin && alarmActive && !state.alarmStatus?.unlocked));
  }
  if (endButton) {
    endButton.classList.toggle("hidden", !(isAdmin && alarmActive));
  }
}

// number of activities active for the current alarm mode; set when an admin
// opens the emergency screen, used to know when everyone has finished everything
let adminExpectedActivityCount = 0;

// have all group members completed every active activity?
function allMembersFinishedActivities() {
  const group = getActiveGroup();
  const members = group?.members || [];
  if (!members.length || adminExpectedActivityCount <= 0) return false;

  const progress = state.alarmStatus?.progress || [];
  return members.every(member => {
    const finishedIds = new Set(
      progress
        .filter(row => row.userId === member.id && row.total > 0 && row.completed >= row.total)
        .map(row => row.activityId)
    );
    return finishedIds.size >= adminExpectedActivityCount;
  });
}

// admin live view: each member's play progress (e.g. "טריוויה 2/3")
function renderAdminProgress() {
  const section = document.querySelector("[data-admin-progress]");
  const list = document.querySelector("[data-admin-progress-list]");
  if (!section || !list) return;

  const show = isCurrentUserAdminForActiveGroup() && Boolean(state.alarmStatus?.active) && activitiesUnlocked();
  section.classList.toggle("hidden", !show);
  if (!show) return;

  const group = getActiveGroup();
  const members = group?.members || [];
  const progress = state.alarmStatus?.progress || [];

  if (!members.length) {
    list.innerHTML = `<p class="notice">אין חברים בקבוצה.</p>`;
    return;
  }

  list.innerHTML = members.map(member => {
    const rows = progress.filter(row => row.userId === member.id);
    const summary = rows.length
      ? rows.map(row => `${activityTypeLabel(row.activityType)} ${row.completed}/${row.total}`).join(" · ")
      : "טרם התחיל";
    const done = rows.length > 0 && rows.every(row => row.total > 0 && row.completed >= row.total);
    return `
      <article class="member-card">
        ${renderAvatarBadge(member.username, "member-avatar avatar-unity-preview", member.avatarImage)}
        <div class="member-main">
          <p class="member-name">${escapeHtml(member.username)}</p>
          <p class="member-role">${escapeHtml(summary)}</p>
        </div>
        <span class="status-pill ${done ? "safe" : "located"}">${done ? "סיים" : "בתהליך"}</span>
      </article>
    `;
  }).join("");
}

// activity type -> short Hebrew label
function activityTypeLabel(type) {
  return type === "mission" ? "חדר משימות" : "טריוויה";
}

// mark a family member safe
function markMemberSafe(id) {
  const member = state.familyMembers.find(item => item.id === id);
  if (!member || member.status === "safe") return;
  member.status = "safe";
  state.emergency.checkIns[id] = formatSeconds(secondsSince(state.emergency.startedAt));
}

// fake the other members checking in
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

// draw the active group game for the current alarm mode
async function renderGame() {
  const locked = document.querySelector("[data-game-locked]");
  const unlocked = document.querySelector("[data-game-unlocked]");
  const area = document.querySelector("[data-game-area]");
  if (!locked || !unlocked || !area) return;

  if (!activitiesUnlocked()) {
    gameAudio.stopActivity();
    locked.classList.remove("hidden");
    unlocked.classList.add("hidden");
    return;
  }

  locked.classList.add("hidden");
  unlocked.classList.remove("hidden");

  if (!state.emergency) startEmergency(null, "real");
  if (!state.emergency.activityStartedAt) {
    state.emergency.activityStartedAt = Date.now();
    saveState();
  }

  // start sampling hand rotation (phone only; no-op / no data on desktop)
  startRotationTracking();

  area.innerHTML = `<p class="notice">טוען פעילות...</p>`;

  const group = getActiveGroup();
  const mode = activeEmergencyMode();

  if (!group) {
    gameAudio.stopActivity();
    area.innerHTML = `
      <p class="notice warn">לא נבחרה קבוצה.</p>
      <a class="btn btn-secondary" href="groups.html">חזרה לקבוצות</a>
    `;
    return;
  }

  try {
    const activities = await getActiveGroupActivities(group.id, mode);
    state.emergency.activityQueue = activities.map(activity => activity.id);
    saveState();

    if (!activities.length) {
      gameAudio.stopActivity();
      area.innerHTML = `
        <p class="notice warn">אין משחק ${escapeHtml(modeLabel(mode))} פעיל לקבוצה זו.</p>
        <a class="btn btn-secondary" href="board.html">חזרה לקבוצה</a>
      `;
      return;
    }

    const index = Math.min(state.emergency.activityQueueIndex || 0, activities.length);

    if (index >= activities.length) {
      gameAudio.stopActivity();
      area.innerHTML = `
        <p class="eyebrow">סיימתם</p>
        <h2>כל המשחקים הושלמו</h2>
        <a class="btn btn-primary" href="summary.html">סיום</a>
      `;
      return;
    }

    const activity = activities[index];
    state.emergency.activeActivity = activity;
    saveState();

    if (activity.type === "trivia") {
      renderTriviaGame(area, activity, mode);
      return;
    }

    renderMissionGame(area, activity, mode);
  } catch (error) {
    gameAudio.stopActivity();
    area.innerHTML = `<p class="notice warn">${escapeHtml(readableAuthError(error))}</p>`;
  }
}

// "training" or "real" for the current run
function activeEmergencyMode() {
  if (state.alarmStatus?.active && (state.alarmStatus.mode === "training" || state.alarmStatus.mode === "real")) {
    return state.alarmStatus.mode;
  }
  return state.emergency?.activityMode === "training" || state.emergency?.trigger === "training"
    ? "training"
    : "real";
}

// draw a multi-question trivia game
function renderTriviaGame(area, activity, mode) {
  const audioActivityKey = `trivia:${activity.id}`;
  const questions = activity.payload?.questions?.length
    ? activity.payload.questions
    : DEFAULT_QUESTIONS;
  const index = Math.min(state.emergency.triviaIndex || 0, questions.length);
  const answers = state.emergency.triviaAnswers || [];

  if (index >= questions.length) {
    renderTriviaComplete(area, activity, mode, questions, answers).catch(error => {
      area.innerHTML = `<p class="notice warn">${escapeHtml(readableAuthError(error))}</p>`;
    });
    return;
  }

  const question = questions[index];
  // reset the per-question timer so each question is timed on its own, not
  // cumulatively from when the whole trivia started
  state.emergency.questionStartedAt = Date.now();
  saveState();
  gameAudio.startActivity(audioActivityKey);
  gameAudio.watchStage();

  area.innerHTML = `
    <p class="eyebrow">${escapeHtml(activity.title)}</p>
    <h2>${escapeHtml(question.question)}</h2>
    <p class="subtitle">${index + 1} / ${questions.length}</p>
    <div class="answer-grid">
      ${question.answers.map((answer, answerIndex) => (
        `<button class="answer-btn" type="button" data-game-answer="${answerIndex}">${escapeHtml(answer)}</button>`
      )).join("")}
    </div>
    <p class="notice hidden" data-game-feedback></p>
  `;

  area.querySelectorAll("[data-game-answer]").forEach(button => {
    button.addEventListener("click", () => {
      const answerIndex = Number(button.dataset.gameAnswer);
      const correct = answerIndex === question.correctAnswerIndex;
      const timeToAnswer = secondsSince(state.emergency.questionStartedAt || state.emergency.activityStartedAt);
      const rotation = takeRotationForItem();
      gameAudio.stopInactivity();
      if (correct) {
        gameAudio.stageSucceeded();
      }

      // always highlight the correct answer in green; if the user picked a
      // wrong one, also paint their pick red so the mistake is clear.
      area.querySelectorAll("[data-game-answer]").forEach(item => {
        const itemIndex = Number(item.dataset.gameAnswer);
        if (itemIndex === question.correctAnswerIndex) item.classList.add("correct");
        else if (itemIndex === answerIndex) item.classList.add("wrong");
        item.disabled = true;
      });
      const feedback = area.querySelector("[data-game-feedback]");
      if (feedback) {
        feedback.textContent = feedbackText(correct);
        feedback.className = `notice ${correct ? "good" : "warn"}`;
      }

      state.emergency.triviaAnswers = [
        ...answers,
        {
          answerIndex,
          correct,
          correctAnswerIndex: question.correctAnswerIndex,
          index,
          label: `Q${index + 1}`,
          question: question.question,
          rotation,
          selectedAnswer: question.answers[answerIndex],
          timeToAnswer
        }
      ];
      state.emergency.triviaIndex = index + 1;
      state.emergency.telemetry.answerTimes.push(timeToAnswer);
      state.emergency.telemetry.tapCount += 1;
      if (correct) state.emergency.telemetry.correct += 1;
      if (!correct) {
        state.emergency.telemetry.incorrect += 1;
        state.emergency.telemetry.mistakes += 1;
      }
      saveState();
      reportActivityProgress(activity, "trivia", index + 1, questions.length);

      window.setTimeout(() => renderTriviaGame(area, activity, mode), 700);
    });
  });
}

// draw the finished trivia score
async function renderTriviaComplete(area, activity, mode, questions, answers) {
  const audioActivityKey = `trivia:${activity.id}`;
  const resultKey = `${activity.id}:trivia`;
  state.emergency.submittedResults = state.emergency.submittedResults || {};
  state.emergency.submittedResults[resultKey] = true;
  saveState();
  reportActivityProgress(activity, "trivia", questions.length, questions.length);
  gameAudio.completeActivity(audioActivityKey);

  const correctCount = answers.filter(answer => answer.correct).length;

  area.innerHTML = `
    <p class="eyebrow">${escapeHtml(activity.title)}</p>
    <h2>${correctCount}/${questions.length} נכונות</h2>
    <p class="notice good">הטריוויה הושלמה.</p>
    ${nextOrFinishButton()}
  `;
  wireNextActivityButton(area);

  // persist the per-question results so the admin statistics page can chart them
  await persistTriviaResult(activity, mode, answers, correctCount, questions.length);
}

// send the per-question trivia results to the backend (feeds the stats charts).
// failures are swallowed so a flaky network never blocks the score screen.
async function persistTriviaResult(activity, mode, answers, correctCount, totalQuestions) {
  const group = getActiveGroup();
  if (!group || !activity?.id) return;

  const items = (answers || []).map((answer, position) => {
    const index = Number.isInteger(answer.index) ? answer.index : position;
    return {
      correct: Boolean(answer.correct),
      index,
      label: answer.label || `Q${index + 1}`,
      rotation: typeof answer.rotation === "number" ? answer.rotation : null,
      timeSeconds: round(answer.timeToAnswer || 0)
    };
  });

  try {
    await submitGroupActivityResult(group.id, {
      activityId: activity.id,
      mode,
      payload: { correctCount, items, kind: "trivia", totalQuestions }
    });
  } catch (error) {
    console.warn("Failed to submit trivia result", error);
  }
}

// is there another game after this one?
function hasMoreActivities() {
  const queue = state.emergency.activityQueue || [];
  const index = state.emergency.activityQueueIndex || 0;
  return index + 1 < queue.length;
}

// "next game" button if more remain, else "finish"
function nextOrFinishButton() {
  return hasMoreActivities()
    ? `<button class="btn btn-primary" type="button" data-next-activity>המשחק הבא</button>`
    : `<button class="btn btn-primary" type="button" data-next-activity>סיום</button>`;
}

// hook the "next game" button up
function wireNextActivityButton(area) {
  area.querySelector("[data-next-activity]")?.addEventListener("click", advanceToNextActivity);
}

// go to the next game, or finish up if there are none left
function advanceToNextActivity() {
  if (hasMoreActivities()) {
    state.emergency.activityQueueIndex = (state.emergency.activityQueueIndex || 0) + 1;
    state.emergency.triviaIndex = 0;
    state.emergency.triviaAnswers = [];
    saveState();
    renderGame();
    return;
  }

  // finished every game: during a live alarm, members go back to the alarm
  // screen and wait there for the admin to end the alert. Outside an alarm
  // (local demo) keep the old summary behavior.
  if (state.alarmStatus?.active) {
    if (state.emergency) {
      state.emergency.activitiesFinished = true;
      saveState();
    }
    window.location.href = "emergency.html";
  } else {
    window.location.href = "summary.html";
  }
}

// draw the unity room with the tasks the admin picked
function renderMissionGame(area, activity, mode) {
  const missionKey = `${activity.id}:room`;
  const audioActivityKey = `mission:${activity.id}`;
  const submitted = state.emergency.submittedResults?.[missionKey];
  const completedStages = new Set();
  // per-task timing + rotation, captured as each stage completes (this sitting only)
  const stageTimings = [];
  let lastStageAt = state.emergency.activityStartedAt || Date.now();
  let missionCompletionHandled = false;
  let missionCompletionAudio = null;
  stopUnityMissionRoom();
  gameAudio.startActivity(audioActivityKey);

  window.saferTogetherMissionCompleted = async detail => {
    if (missionCompletionHandled) return;
    missionCompletionHandled = true;

    try {
      await submitMissionCompletion(activity, mode, detail || {}, stageTimings);
      missionCompletionAudio = missionCompletionAudio || gameAudio.completeActivity(audioActivityKey);
      await missionCompletionAudio;
      // skip the "sent to admin" screen, just go to the next game
      advanceToNextActivity();
    } catch (error) {
      missionCompletionHandled = false;
      renderUnityMissionStatus(readableAuthError(error), "warn");
    }
  };

  // already done (e.g. after a reload)? skip it
  if (submitted) {
    advanceToNextActivity();
    return;
  }

  const missionTotal = Math.max(1, Array.isArray(activity.payload?.tasks) ? activity.payload.tasks.length : 0);
  reportActivityProgress(activity, "mission", 0, missionTotal);
  gameAudio.watchStage(MISSION_INACTIVITY_DELAY_MS);

  // each step inside a task (a new bolt, the dial, the next exercise, the next
  // wire level) pings this so the idle timer restarts from that step
  window.saferTogetherMissionStageProgress = () => {
    gameAudio.watchStage(MISSION_INACTIVITY_DELAY_MS);
  };

  window.saferTogetherMissionStageCompleted = detail => {
    const target = String(detail?.target || detail || "").trim();
    if (!target || completedStages.has(target)) return;

    // time this task took = gap since the previous task completed (or activity start)
    const now = Date.now();
    stageTimings.push({
      rotation: takeRotationForItem(),
      target,
      timeSeconds: (now - lastStageAt) / 1000
    });
    lastStageAt = now;

    completedStages.add(target);
    gameAudio.stageSucceeded();
    reportActivityProgress(activity, "mission", Math.min(completedStages.size, missionTotal), missionTotal);

    if (completedStages.size >= missionTotal) {
      missionCompletionAudio = gameAudio.completeActivity(audioActivityKey);
    } else {
      // more stages to go: re-arm the idle timer so encouragement clips can
      // play again if the user gets stuck on the next stage
      gameAudio.watchStage(MISSION_INACTIVITY_DELAY_MS);
    }
  };

  area.innerHTML = `
    <p class="eyebrow">${escapeHtml(activity.title)}</p>
    <h2>חדר משימות</h2>
    <p class="subtitle">השלימו את כל המשימות שנבחרו, ולאחר מכן שלחו.</p>
    <p class="notice" data-unity-mission-status>טוען את חדר המשימות...</p>
    <section
      class="unity-webgl-host mission-unity-host"
      data-unity-room-host
      data-loader-url="/unity/mission-room/Build/mission-room.loader.js"
      data-data-url="/unity/mission-room/Build/mission-room.data.gz"
      data-framework-url="/unity/mission-room/Build/mission-room.framework.js.gz"
      data-code-url="/unity/mission-room/Build/mission-room.wasm.gz"
      data-streaming-assets-url="/unity/mission-room/StreamingAssets"
      aria-label="Unity mission room"
    >
      <canvas id="unity-mission-room-canvas" class="unity-webgl-canvas mission-unity-canvas" data-unity-room-canvas tabindex="-1"></canvas>
      <div class="unity-progress" data-unity-room-progress>
        <span></span>
      </div>
    </section>
    <section class="unity-build-missing hidden" data-unity-room-missing>
      <p>יש לבנות את חדר המשימות (Unity WebGL) אל:</p>
      <code>SaferTogetherUI/unity/mission-room</code>
    </section>
  `;

  loadUnityMissionRoom(activity, mode).catch(error => {
    const message = readableError(error, "לא ניתן להפעיל את חדר המשימות");
    const host = document.querySelector("[data-unity-room-host]");
    const missing = document.querySelector("[data-unity-room-missing]");

    console.warn("Unity mission room startup failed", error);

    if (/לא נמצאה|Unity WebGL build was not found/i.test(message)) {
      host?.classList.add("unity-webgl-host-missing");
      missing?.classList.remove("hidden");
    } else {
      host?.classList.remove("unity-webgl-host-missing");
      missing?.classList.add("hidden");
    }

    renderUnityMissionStatus(message, "warn");
  });
}

// tear down the old mission-room unity instance before reusing the canvas
function stopUnityMissionRoom() {
  clearMissionPayloadSender();
  window.saferTogetherMissionCompleted = null;
  window.saferTogetherMissionStageCompleted = null;
  window.saferTogetherMissionStageProgress = null;

  const unityInstance = window.saferTogetherMissionUnityInstance;
  window.saferTogetherMissionUnityInstance = null;

  if (unityInstance?.Quit) {
    unityInstance.Quit().catch(() => {});
  }
}

// load the mission-room unity build and send it the chosen tasks
async function loadUnityMissionRoom(activity, mode) {
  const host = document.querySelector("[data-unity-room-host]");
  const canvas = document.querySelector("[data-unity-room-canvas]");

  if (!host || !canvas) {
    return;
  }

  const config = getUnityHostConfig(host);
  canvas.id = canvas.id || "unity-mission-room-canvas";
  await loadScript(config.loaderUrl);

  const unityInstance = await window.createUnityInstance(canvas, {
    arguments: [],
    codeUrl: config.codeUrl,
    dataUrl: config.dataUrl,
    frameworkUrl: config.frameworkUrl,
    streamingAssetsUrl: config.streamingAssetsUrl,
    companyName: "DefaultCompany",
    productName: "mission-room",
    productVersion: "1.0",
    showBanner: (message, type) => {
      if (!message) {
        return;
      }

      renderUnityMissionStatus(message, type === "error" || type === "warning" ? "warn" : "");
    }
  }, progress => {
    const progressBar = document.querySelector("[data-unity-room-progress] span");
    if (progressBar) {
      progressBar.style.width = `${Math.round(progress * 100)}%`;
    }
  });

  window.saferTogetherMissionUnityInstance = unityInstance;

  renderUnityMissionStatus("");
  sendUnityMissionPayload(unityInstance, createUnityMissionPayload(activity, mode));
}

// keep sending mission data to unity until it acks
function sendUnityMissionPayload(unityInstance, payload) {
  clearMissionPayloadSender();

  if (!unityInstance?.SendMessage) {
    return;
  }

  const payloadJson = JSON.stringify(payload);
  const targets = [
    "SaferTogether Mission Room Controller",
    "Mission Room Controller",
    "MissionRoomController",
    "GameObject"
  ];
  const maxAttempts = 15;
  let attempts = 0;
  let acknowledged = false;

  window.saferTogetherMissionRoomAck = () => {
    acknowledged = true;
    clearMissionPayloadSender();
    renderUnityMissionStatus("");
  };

  // one send attempt, gives up after maxAttempts
  const trySend = () => {
    attempts += 1;

    targets.forEach(targetName => {
      try {
        unityInstance.SendMessage(targetName, "ApplyMissionJson", payloadJson);
      } catch (error) {
      }
    });

    if (acknowledged) {
      clearMissionPayloadSender();
      return;
    }

    if (attempts >= maxAttempts) {
      clearMissionPayloadSender();
      renderUnityMissionStatus(
        "חדר המשימות לא הגיב. בנו אותו מחדש ב-Unity (SaferTogether > Build WebGL Mission Room) וטענו מחדש.",
        "warn"
      );
    }
  };

  window.saferTogetherMissionSendTimer = window.setInterval(trySend, 400);
  trySend();
}

// stop the payload retries + clear the ack handler
function clearMissionPayloadSender() {
  if (window.saferTogetherMissionSendTimer) {
    window.clearInterval(window.saferTogetherMissionSendTimer);
    window.saferTogetherMissionSendTimer = null;
  }

  window.saferTogetherMissionRoomAck = null;
}

// payload sent to unity: chosen tasks + exercises + profile
function createUnityMissionPayload(activity, mode) {
  const group = getActiveGroup();
  const payload = activity.payload || {};

  return {
    activityId: activity.id,
    groupId: group?.id || "",
    mode,
    tasks: Array.isArray(payload.tasks) ? payload.tasks : [],
    exercises: Array.isArray(payload.exercises) ? payload.exercises : [],
    profile: {
      avatar: state.user?.avatar || "",
      avatarImage: state.user?.avatarImage || "",
      username: state.user?.username || state.user?.name || "משתמש"
    }
  };
}

// status line around the unity room
function renderUnityMissionStatus(message, tone = "") {
  const status = document.querySelector("[data-unity-mission-status]");

  if (!status) {
    return;
  }

  if (!message) {
    status.className = "notice hidden";
    status.textContent = "";
    return;
  }

  status.className = `notice ${tone}`.trim();
  status.textContent = message;
}

// mark the room done locally + persist the per-task results for the stats page.
// the unity room self-verifies before it reports done.
async function submitMissionCompletion(activity, mode, detail = {}, stageTimings = []) {
  const missionKey = `${activity.id}:room`;
  state.emergency.submittedResults = state.emergency.submittedResults || {};

  if (state.emergency.submittedResults[missionKey]) {
    return state.emergency.submittedResults[missionKey];
  }

  state.emergency.submittedResults[missionKey] = true;
  state.emergency.missionCompletedAt = secondsSince(state.emergency.activityStartedAt);
  saveState();
  const missionTotal = Math.max(1, Array.isArray(activity.payload?.tasks) ? activity.payload.tasks.length : 0);
  reportActivityProgress(activity, "mission", missionTotal, missionTotal);

  await persistMissionResult(activity, mode, detail, stageTimings);
  return true;
}

function missionGameLabel(id) {
  return MISSION_GAME_DEFINITIONS[id]?.label || id;
}

function missionGameDescription(id) {
  return MISSION_GAME_DEFINITIONS[id]?.description || "";
}

function numberOrNull(value) {
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

function normalizeMissionGames(detail = {}) {
  const rawGames = Array.isArray(detail?.games) ? detail.games : [];

  return rawGames
    .map(rawGame => {
      const id = String(rawGame?.game || "").trim().toLowerCase();
      const stages = Array.isArray(rawGame?.stages) ? rawGame.stages : [];

      return {
        description: missionGameDescription(id),
        id,
        label: missionGameLabel(id),
        stages: stages.map((stage, index) => ({
          correct: typeof stage?.correct === "boolean" ? stage.correct : null,
          index: Number.isInteger(Number(stage?.index)) ? Number(stage.index) : index,
          label: String(stage?.label || `${missionGameLabel(id)} ${index + 1}`),
          rotation: numberOrNull(stage?.rotation),
          timeSeconds: numberOrNull(stage?.timeSeconds),
          wrongAttempts: Number.isInteger(Number(stage?.wrongAttempts)) ? Number(stage.wrongAttempts) : 0
        })),
        hits: id === "missile" ? numberOrNull(rawGame?.hits) : null,
        tiltStrength: id === "missile" ? numberOrNull(rawGame?.tiltStrength) : null,
        totalSeconds: numberOrNull(rawGame?.totalSeconds),
        weightedScore: id === "code" ? numberOrNull(rawGame?.weightedScore) : null
      };
    })
    .filter(game => MISSION_GAME_IDS.includes(game.id));
}

function missionMetricItems(activity, games, stageTimings = []) {
  const taskOrder = Array.isArray(activity.payload?.tasks)
    ? activity.payload.tasks.filter(task => MISSION_GAME_IDS.includes(task))
    : [];
  const taskOffset = new Map(taskOrder.map((task, index) => [task, index * 10]));
  const items = [];

  games.forEach(game => {
    const offset = taskOffset.has(game.id) ? taskOffset.get(game.id) : items.length * 10;

    game.stages.forEach((stage, stageIndex) => {
      const label = game.stages.length > 1
        ? `${game.label} - ${stageIndex + 1}`
        : game.label;
      const item = {
        description: game.description,
        game: game.id,
        index: offset + stageIndex,
        label,
        wrongAttempts: stage.wrongAttempts
      };

      if (typeof stage.timeSeconds === "number") item.timeSeconds = round(stage.timeSeconds);
      if (typeof stage.rotation === "number") item.rotation = round(stage.rotation);
      if (typeof stage.correct === "boolean") item.correct = stage.correct;
      if (typeof stage.wrongAttempts === "number") item.mistakes = stage.wrongAttempts;
      if (typeof game.weightedScore === "number") item.weightedScore = round(game.weightedScore);
      if (typeof game.hits === "number") item.hits = game.hits;
      if (typeof game.tiltStrength === "number") item.tiltStrength = round(game.tiltStrength);
      if (game.id === "missile" && typeof game.hits === "number") item.mistakes = game.hits;

      items.push(item);
    });
  });

  if (items.length) {
    return items;
  }

  // Backward-compatible fallback for older builds that only report completed task timings.
  return (stageTimings || [])
    .map(stage => ({
      index: taskOffset.has(stage.target) ? taskOffset.get(stage.target) : -1,
      label: missionGameLabel(stage.target),
      rotation: typeof stage.rotation === "number" ? stage.rotation : null,
      timeSeconds: round(stage.timeSeconds || 0)
    }))
    .filter(item => item.index >= 0);
}

// send the per-stage mission results to the backend (feeds the stats charts).
// Unity sends games[]. Each stage becomes a stable metric item so per-stage time,
// correct/wrong counts, hit counts and phone tilt can be summarized later.
async function persistMissionResult(activity, mode, detail = {}, stageTimings = []) {
  const group = getActiveGroup();
  if (!group || !activity?.id) return;

  const games = normalizeMissionGames(detail);
  const items = missionMetricItems(activity, games, stageTimings);
  const completedTasks = games.length
    ? games.map(game => game.id)
    : (stageTimings || []).map(stage => stage.target);

  try {
    await submitGroupActivityResult(group.id, {
      activityId: activity.id,
      mode,
      payload: { games, items, kind: "mission", tasks: completedTasks }
    });
  } catch (error) {
    console.warn("Failed to submit mission result", error);
  }
}

// draw the stress report for one member
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
      <p class="eyebrow">סיכום מצב</p>
      <h2>${escapeHtml(member.name)}: <span class="stress-level ${stressClass(report.stressLevel)}">${stressLabel(report.stressLevel)}</span></h2>
      <p class="subtitle">דקת לחץ גבוהה ביותר: דקה ${highest.minute}. סיבה מרכזית: ${escapeHtml(report.reason)}.</p>
    </section>

    <section class="card">
      <h2>רמת לחץ לפי דקה</h2>
      <div class="bar-chart">
        ${stressByMinute.map(item => `
          <div class="bar-row">
            <span>דקה ${item.minute}</span>
            <span class="bar-track"><span class="bar-fill" style="--value:${item.stress}%"></span></span>
            <span>${item.stress}</span>
          </div>
        `).join("")}
      </div>
    </section>

    <section class="card">
      <h2>זמן מענה</h2>
      <div class="comparison-grid">
        <div class="comparison-box"><span>בסיס אימון</span><strong>${baseline.averageAnswerTime}s</strong></div>
        <div class="comparison-box"><span>אירוע נוכחי</span><strong>${currentAverage}s</strong></div>
      </div>
    </section>

    <section class="card">
      <h2>נכון מול שגוי</h2>
      <div class="comparison-grid">
        <div class="comparison-box"><span>נכון</span><strong>${report.correct}</strong></div>
        <div class="comparison-box"><span>שגוי</span><strong>${report.mistakes}</strong></div>
      </div>
    </section>

    <section class="card">
      <h2>בסיס מול אירוע נוכחי</h2>
      <div class="comparison-grid">
        <div class="comparison-box"><span>שיעור טעויות בסיס</span><strong>${baseline.mistakeRate}</strong></div>
        <div class="comparison-box"><span>שיעור טעויות נוכחי</span><strong>${currentMistakeRate}</strong></div>
        <div class="comparison-box"><span>תנועה בסיסית</span><strong>${baseline.averageMovementLevel}</strong></div>
        <div class="comparison-box"><span>תנועה נוכחית</span><strong>${movement}</strong></div>
      </div>
    </section>

    <section class="notice warn">
      אינדיקציה אפשרית ללחץ: ${escapeHtml(report.explanation)}
      המלצה: לשוחח ברוגע לאחר האירוע ולשאול איך הרגישו.
    </section>
  `;
}

// build the analytics report data for one member
function buildMemberReport(member, index) {
  const baseline = state.baseline || DEFAULT_BASELINE;
  const telemetry = state.emergency?.telemetry || {};
  const answerTimes = telemetry.answerTimes?.length ? telemetry.answerTimes : [4.8];
  const currentAverage = round(average(answerTimes) + index * 0.35);
  const correct = Math.max((telemetry.correct || 1) - (index === 2 ? 1 : 0), 0);
  const mistakes = (telemetry.mistakes || 0) + (index === 2 ? 2 : index === 1 ? 1 : 0);
  const currentMistakeRate = round(mistakes / Math.max(correct + mistakes, 1));
  const movementLevel = round((telemetry.movementLevel || 0.7) + index * 0.06);
  const checkInTime = state.emergency?.checkIns?.[member.id] || (member.status === "safe" ? "00:08" : "לא דווח");

  let stressLevel = "Low";
  let reason = "התנהגות קרובה לבסיס האימון";

  if (currentAverage > baseline.averageAnswerTime * 1.8 && currentMistakeRate > 0.25 && movementLevel > baseline.averageMovementLevel + 0.3) {
    stressLevel = "High";
    reason = "זמן תגובה איטי יותר, יותר טעויות ותנועה גבוהה מהרגיל";
  } else if (currentAverage > baseline.averageAnswerTime * 1.25 || currentMistakeRate > baseline.mistakeRate + 0.12) {
    stressLevel = "Medium";
    reason = "מענה מעט איטי יותר מבסיס האימון";
  }

  const stressBase = stressLevel === "High" ? 78 : stressLevel === "Medium" ? 56 : 32;
  const stressByMinute = [0, 1, 2, 3, 4, 5].map(offset => ({
    minute: offset + 1,
    stress: Math.max(stressBase - offset * 8, 18)
  }));

  return {
    checkInTime,
    participation: member.id === "1" ? "שאלון ומשימה" : "אישור מוגן בלבד",
    correct,
    mistakes,
    stressLevel,
    reason,
    currentAverage,
    currentMistakeRate,
    movementLevel,
    stressByMinute,
    explanation: `${member.name} הראה/ה ${reason} במהלך האירוע. ייתכן שכדאי לשים לב, אך זה אינו אבחון.`
  };
}

// start the emergency countdown timer
function startEventTimer() {
  if (!document.querySelector("[data-event-timer]")) return;

  updateEventTimer();
  window.setInterval(updateEventTimer, 1000);
}

// update timer text + progress bars
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

// is everyone safe?
function allMembersSafe() {
  return state.familyMembers.every(member => member.status === "safe");
}

// status id -> display text
function statusLabel(status) {
  if (status === "safe") return "מוגן";
  if (status === "at_risk") return "בסיכון";
  return "לא מחובר";
}

// alarm mode -> hebrew
function modeLabel(mode) {
  if (mode === "training") return "תרגול";
  if (mode === "real") return "אזעקת אמת";
  return mode || "";
}

// result review status -> hebrew
function resultStatusLabel(status) {
  if (status === "approved") return "אושר";
  if (status === "rejected") return "נדחה";
  if (status === "pending") return "ממתין";
  return status || "";
}

// set textContent if the element exists
function setText(selector, value) {
  const node = document.querySelector(selector);
  if (node) node.textContent = value;
}

// escape text before dropping it into html
function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

// seconds since a timestamp
function secondsSince(timestamp) {
  return Math.max((Date.now() - timestamp) / 1000, 0);
}

// seconds -> mm:ss
function formatSeconds(seconds) {
  const total = Math.max(Math.floor(seconds), 0);
  const minutes = Math.floor(total / 60).toString().padStart(2, "0");
  const rest = (total % 60).toString().padStart(2, "0");
  return `${minutes}:${rest}`;
}

// average of some numbers
function average(values) {
  if (!values.length) return 0;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

// round to 2 decimals
function round(value) {
  return Math.round(value * 100) / 100;
}

// stress level -> css class
function stressClass(level) {
  if (level === "High") return "stress-high";
  if (level === "Medium") return "stress-medium";
  return "stress-low";
}

// stress level -> display text
function stressLabel(level) {
  if (level === "High") return "גבוה";
  if (level === "Medium") return "בינוני";
  return "נמוך";
}

// right/wrong feedback text
function feedbackText(correct) {
  return correct ? "כל הכבוד! המשיכו כך." : "ניסיון טוב. אתם עושים עבודה טובה.";
}
