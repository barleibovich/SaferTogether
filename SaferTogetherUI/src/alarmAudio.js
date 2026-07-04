const ALARM_AUDIO_STORAGE_KEY = "saferTogetherAlarmAudio.v1";

const ALARM_FILES = {
  real: "/audio/real_alarm.mp3",
  training: "/audio/training_alarm.mp3"
};

function normalizeMode(mode) {
  return mode === "training" ? "training" : "real";
}

function readPlaybackState() {
  try {
    return JSON.parse(sessionStorage.getItem(ALARM_AUDIO_STORAGE_KEY) || "null");
  } catch {
    return null;
  }
}

function writePlaybackState(value) {
  if (!value) {
    sessionStorage.removeItem(ALARM_AUDIO_STORAGE_KEY);
    return;
  }

  sessionStorage.setItem(ALARM_AUDIO_STORAGE_KEY, JSON.stringify(value));
}

class AlarmAudioController {
  constructor() {
    this.audio = new Audio();
    this.audio.preload = "auto";
    this.audio.volume = 1;
    this.activeAlarmId = "";
    this.activeMode = "";
    this.playStartedAt = 0;

    // tell the page when iOS is blocking the siren until a tap, so it can prompt
    this.audio.addEventListener("playing", () => this.emitState());
    this.audio.addEventListener("pause", () => this.emitState());

    this.audio.addEventListener("ended", () => {
      const saved = readPlaybackState();
      if (saved?.alarmId === this.activeAlarmId) {
        writePlaybackState({
          ...saved,
          completed: true
        });
      }
      this.resetLocalState();
    });

    const retryBlockedPlayback = () => {
      if (this.activeAlarmId && this.audio.paused) {
        this.audio.play().catch(() => {});
      }
    };

    document.addEventListener("pointerdown", retryBlockedPlayback, { capture: true });
    document.addEventListener("keydown", retryBlockedPlayback, { capture: true });

    const saved = readPlaybackState();
    if (saved?.alarmId && saved?.mode && !saved.completed) {
      Promise.resolve().then(() => this.start(saved.mode, saved.alarmId));
    }
  }

  start(mode, alarmId) {
    const cleanMode = normalizeMode(mode);
    const cleanAlarmId = String(alarmId || `${cleanMode}:${Date.now()}`);
    const saved = readPlaybackState();

    if (saved?.alarmId === cleanAlarmId && saved?.mode === cleanMode && saved?.completed) {
      return Promise.resolve();
    }

    if (
      this.activeAlarmId &&
      this.activeMode === cleanMode &&
      (
        this.activeAlarmId === cleanAlarmId ||
        this.activeAlarmId.startsWith("pending:")
      )
    ) {
      this.activeAlarmId = cleanAlarmId;
      writePlaybackState({
        alarmId: cleanAlarmId,
        completed: false,
        mode: cleanMode,
        playStartedAt: this.playStartedAt
      });
      return this.audio.play().catch(() => this.emitState());
    }

    const resumeSavedAlarm = saved?.alarmId === cleanAlarmId && saved?.mode === cleanMode;
    const playStartedAt = resumeSavedAlarm
      ? Number(saved.playStartedAt) || Date.now()
      : Date.now();
    const elapsedSeconds = Math.max(0, (Date.now() - playStartedAt) / 1000);

    this.activeAlarmId = cleanAlarmId;
    this.activeMode = cleanMode;
    this.playStartedAt = playStartedAt;
    writePlaybackState({
      alarmId: cleanAlarmId,
      completed: false,
      mode: cleanMode,
      playStartedAt
    });

    this.audio.pause();
    this.audio.src = ALARM_FILES[cleanMode];
    this.audio.currentTime = 0;

    const seekToElapsedTime = () => {
      if (
        elapsedSeconds > 0 &&
        Number.isFinite(this.audio.duration) &&
        elapsedSeconds < this.audio.duration
      ) {
        this.audio.currentTime = elapsedSeconds;
      }
    };

    if (this.audio.readyState >= 1) {
      seekToElapsedTime();
    } else {
      this.audio.addEventListener("loadedmetadata", seekToElapsedTime, { once: true });
    }

    return this.audio.play().catch(() => {
      // Browsers may wait for the user's next tap before allowing audio.
      this.emitState();
    });
  }

  stop() {
    writePlaybackState(null);
    this.audio.pause();
    this.audio.currentTime = 0;
    this.audio.removeAttribute("src");
    this.audio.load();
    this.resetLocalState();
  }

  dismiss(alarmId = "") {
    const saved = readPlaybackState();
    const cleanAlarmId = String(alarmId || this.activeAlarmId || saved?.alarmId || "");
    const mode = this.activeMode || saved?.mode || "real";

    if (cleanAlarmId) {
      writePlaybackState({
        alarmId: cleanAlarmId,
        completed: true,
        mode,
        playStartedAt: this.playStartedAt || saved?.playStartedAt || Date.now()
      });
    }

    this.audio.pause();
    this.audio.currentTime = 0;
    this.audio.removeAttribute("src");
    this.audio.load();
    this.resetLocalState();
  }

  resetLocalState() {
    this.activeAlarmId = "";
    this.activeMode = "";
    this.playStartedAt = 0;
  }

  // true when there's an active alarm whose audio is held back (waiting for a tap).
  isBlocked() {
    return Boolean(this.activeAlarmId) && this.audio.paused;
  }

  // tell the page whether the siren is sounding or blocked, so it can prompt a tap.
  emitState() {
    if (typeof document === "undefined") {
      return;
    }
    document.dispatchEvent(new CustomEvent("saferAlarmAudioState", {
      detail: {
        blocked: this.isBlocked(),
        playing: Boolean(this.activeAlarmId) && !this.audio.paused
      }
    }));
  }
}

export const alarmAudio = new AlarmAudioController();
