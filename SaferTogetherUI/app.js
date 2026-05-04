import {
  getCurrentUserProfile,
  loginWithUsername,
  logout,
  signUpWithUsername
} from "./src/api/authGateway.js";
import {
  createGroupForCurrentUser,
  deleteOwnedGroup,
  getCurrentUserGroups,
  renameGroup,
  requestJoinByCode,
  reviewJoinRequest
} from "./src/api/groupGateway.js";

const STORAGE_KEY = "saferTogetherState.v5";
const EVENT_DURATION_SECONDS = 600;

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

document.addEventListener("DOMContentLoaded", () => {
  ensureDefaults();
  routePage();
  startEventTimer();
});

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
    emergency: null
  };
}

function copy(value) {
  return JSON.parse(JSON.stringify(value));
}

function loadState() {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    return saved ? { ...initialState(), ...JSON.parse(saved) } : initialState();
  } catch {
    return initialState();
  }
}

function saveState() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

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
  saveState();
}

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
  if (page === "summary") initSummary();
  if (page === "report") initAdminPage(initReport);
}

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

function initSignup() {
  const form = document.querySelector("[data-signup-form]");

  form?.addEventListener("submit", async event => {
    event.preventDefault();
    clearFormMessage(form);

    const formData = new FormData(form);
    const username = formData.get("username")?.toString() || "";
    const password = formData.get("password")?.toString() || "";
    const role = formData.get("role")?.toString() === "admin" ? "admin" : "user";

    try {
      setFormBusy(form, true);
      await signUpWithUsername({ username, password, role });
      await loadSessionIntoState();
      saveState();
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

function setSessionUser(username, role, userId) {
  const cleanRole = role === "admin" ? "admin" : "user";

  state.user = {
    userId,
    username,
    name: username,
    role: cleanRole,
    familyRoomId: state.activeGroupId || ""
  };

  state.groups = state.groups.map(group => ({ ...group, userRole: cleanRole }));
}

async function loadSessionIntoState() {
  const profile = await getCurrentUserProfile();

  if (!profile) {
    return null;
  }

  setSessionUser(profile.username, profile.role, profile.id);
  state.groups = [];
  await refreshCurrentUserGroups(profile.role);
  return profile;
}

async function refreshCurrentUserGroups(role = state.user?.role || "user") {
  const groups = await getCurrentUserGroups();

  state.groups = groups
    .filter(Boolean)
    .map(group => ({
      id: group.id,
      joinCode: group.joinCode || "",
      members: Array.isArray(group.members) ? group.members : [],
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

function showFormError(form, message) {
  showFormMessage(form, message, "auth-error");
}

function showFormSuccess(form, message) {
  showFormMessage(form, message, "auth-success");
}

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

function clearFormMessage(form) {
  form.querySelector("[data-form-message]")?.remove();
}

function readableAuthError(error) {
  return error?.message || "Authentication failed. Please try again.";
}

async function initAdminPage(initializer) {
  const allowed = await requireAdminAccess();
  if (allowed && initializer) initializer();
}

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
    if (!state.user) {
      state.groups = [];
      renderGroupsList();
      createButton?.classList.add("hidden");
      return;
    }
  }

  const user = state.user;
  if (!user) {
    window.location.href = "index.html";
    return;
  }

  if (currentUser) {
    currentUser.textContent = `Logged in as ${user.username || user.name} (${user.role})`;
  }

  renderGroupsList();
  createButton?.classList.toggle("hidden", user.role !== "admin");
  joinForm?.classList.toggle("hidden", user.role === "admin");

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
    localStorage.removeItem(STORAGE_KEY);
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
          ${renderPendingRequests(group)}
        </div>
      ` : ""}
    </article>
  `).join("");

  container.querySelectorAll("[data-open-group]").forEach(button => {
    button.addEventListener("click", () => openGroup(button.dataset.openGroup));
  });

  container.querySelectorAll("[data-review-request]").forEach(button => {
    button.addEventListener("click", async () => {
      try {
        button.disabled = true;
        await reviewJoinRequest({
          groupId: button.dataset.groupId,
          requestId: button.dataset.requestId,
          status: button.dataset.reviewRequest
        });
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

// This function shows the pending join requests for one group.
function renderPendingRequests(group) {
  if (!group.pendingRequests.length) {
    return `<p class="notice">No pending requests.</p>`;
  }

  return `
    <div class="join-request-list">
      ${group.pendingRequests.map(request => `
        <div class="join-request-card">
          <p>${escapeHtml(request.username || "User")} wants to join the group.</p>
          <div class="join-request-actions">
            <button class="btn btn-primary" type="button" data-group-id="${group.id}" data-request-id="${request.id}" data-review-request="approved">Accept</button>
            <button class="btn btn-secondary" type="button" data-group-id="${group.id}" data-request-id="${request.id}" data-review-request="declined">Decline</button>
          </div>
        </div>
      `).join("")}
    </div>
  `;
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

  document.querySelectorAll("[data-admin-only]").forEach(node => {
    node.classList.toggle("hidden", !isCurrentUserAdminForActiveGroup());
  });

  document.querySelector("[data-trigger-emergency]")?.addEventListener("click", () => {
    startEmergency();
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

function getActiveGroup() {
  return state.groups.find(group => group.id === state.activeGroupId) || state.groups[0] || null;
}

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

  container.innerHTML = group.members.map(member => `
    <article class="member-card">
      <div class="member-main">
        <p class="member-name">${escapeHtml(member.username)}</p>
        <p class="member-role">${member.role === "admin" ? "Admin" : "User"}</p>
      </div>
    </article>
  `).join("");
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

// This function polls the server every 15 seconds to pick up new join requests.
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
    } catch {
      // silent — next tick will retry
    }
  }, INTERVAL_MS);

  window.addEventListener("beforeunload", () => clearInterval(intervalId));
}

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

function initPractice() {
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

  document.querySelector("[data-practice-safe]")?.addEventListener("click", event => {
    if (!state.practiceSession) return;
    state.practiceSession.safeAt = Date.now();
    state.practiceSession.taps += 1;
    saveState();
    event.currentTarget.textContent = "××™×©×•×¨ ×ž×•×’×Ÿ × ×©×ž×¨";
    event.currentTarget.disabled = true;
  });

  document.querySelector("[data-complete-practice]")?.addEventListener("click", () => {
    completePractice();
  });
}

function initEmergency() {
  if (!state.emergency?.active) {
    startEmergency();
  }

  renderEmergency();

  document.querySelector("[data-emergency-safe]")?.addEventListener("click", () => {
    if (allMembersSafe()) {
      window.location.href = "game.html";
      return;
    }

    markMemberSafe("1");
    state.emergency.telemetry.safeClickTime = secondsSince(state.emergency.startedAt);
    state.emergency.telemetry.tapCount += 1;
    saveState();
    renderEmergency();
    simulateFamilyCheckIns();
  });
}

function initGame() {
  renderGame();
}

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

function initReport() {
  const select = document.querySelector("[data-report-member]");
  if (!select) return;

  select.innerHTML = state.familyMembers.map(member => (
    `<option value="${member.id}">${escapeHtml(member.name)}</option>`
  )).join("");

  select.addEventListener("change", () => renderReport(select.value));
  renderReport(select.value || state.familyMembers[0]?.id);
}

function renderFamilyList(container) {
  if (!container) return;

  container.innerHTML = state.familyMembers.map(member => `
    <article class="member-card">
      <div class="avatar-badge ${member.status}">
        <span>${member.avatar || "ðŸ‘¤"}</span>
      </div>
      <div class="member-main">
        <p class="member-name">${escapeHtml(member.name)}</p>
        <p class="member-role">${escapeHtml(member.role)}</p>
      </div>
      <span class="status-pill ${member.status}">${statusLabel(member.status)}</span>
    </article>
  `).join("");
}

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

function startEmergency() {
  state.familyMembers = state.familyMembers.map(member => ({ ...member, status: "at_risk" }));
  state.emergency = {
    active: true,
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

function renderEmergency() {
  renderFamilyList(document.querySelector("[data-emergency-family]"));
  const button = document.querySelector("[data-emergency-safe]");
  const message = document.querySelector("[data-emergency-message]");
  const current = state.familyMembers.find(member => member.id === "1");

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

function markMemberSafe(id) {
  const member = state.familyMembers.find(item => item.id === id);
  if (!member || member.status === "safe") return;
  member.status = "safe";
  state.emergency.checkIns[id] = formatSeconds(secondsSince(state.emergency.startedAt));
}

function simulateFamilyCheckIns() {
  const pending = state.familyMembers.filter(member => member.status !== "safe" && member.id !== "1");
  pending.forEach((member, index) => {
    window.setTimeout(() => {
      markMemberSafe(member.id);
      saveState();
      renderEmergency();
    }, 900 + index * 900);
  });
}

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

function startEventTimer() {
  if (!document.querySelector("[data-event-timer]")) return;

  updateEventTimer();
  window.setInterval(updateEventTimer, 1000);
}

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

function countStatus(status) {
  return state.familyMembers.filter(member => member.status === status).length;
}

function allMembersSafe() {
  return state.familyMembers.every(member => member.status === "safe");
}

function statusLabel(status) {
  if (status === "safe") return "×ž×•×’×Ÿ";
  if (status === "at_risk") return "×‘×¡×™×›×•×Ÿ";
  return "OFFLINE";
}

function setText(selector, value) {
  const node = document.querySelector(selector);
  if (node) node.textContent = value;
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function secondsSince(timestamp) {
  return Math.max((Date.now() - timestamp) / 1000, 0);
}

function formatSeconds(seconds) {
  const total = Math.max(Math.floor(seconds), 0);
  const minutes = Math.floor(total / 60).toString().padStart(2, "0");
  const rest = (total % 60).toString().padStart(2, "0");
  return `${minutes}:${rest}`;
}

function average(values) {
  if (!values.length) return 0;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function round(value) {
  return Math.round(value * 100) / 100;
}

function stressClass(level) {
  if (level === "High") return "stress-high";
  if (level === "Medium") return "stress-medium";
  return "stress-low";
}

function stressLabel(level) {
  if (level === "High") return "×’×‘×•×”";
  if (level === "Medium") return "×‘×™× ×•× ×™";
  return "× ×ž×•×š";
}

function feedbackText(correct) {
  return correct ? "×›×œ ×”×›×‘×•×“! ×”×ž×©×™×›×• ×›×š." : "× ×™×¡×™×•×Ÿ ×˜×•×‘. ××ª× ×¢×•×©×™× ×¢×‘×•×“×” ×˜×•×‘×”.";
}
