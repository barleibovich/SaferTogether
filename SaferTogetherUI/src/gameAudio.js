const AUDIO_ROOT = "/audio";
const INACTIVITY_DELAY_MS = 8000;

const AUDIO_FILES = {
  background: `${AUDIO_ROOT}/background_music.mp3`,
  stageSuccess: `${AUDIO_ROOT}/success_mission.mp3`,
  activitySuccess: `${AUDIO_ROOT}/success_mission_2.mp3`,
  encouragement: [
    `${AUDIO_ROOT}/every_step_closer_final.mp3`,
    `${AUDIO_ROOT}/fun_to_see_process.mp3`,
    `${AUDIO_ROOT}/great_effort.mp3`,
    `${AUDIO_ROOT}/great_we're_on_track.mp3`,
    `${AUDIO_ROOT}/keep_trying.mp3`
  ]
};

function makeAudio(src, { loop = false, volume = 1 } = {}) {
  const audio = new Audio(src);
  audio.loop = loop;
  audio.preload = "auto";
  audio.volume = volume;
  return audio;
}

class GameAudioController {
  constructor() {
    this.background = makeAudio(AUDIO_FILES.background, { loop: true, volume: 0.24 });
    this.cue = makeAudio("", { volume: 1 });
    this.encouragement = makeAudio("", { volume: 0.92 });
    this.activeActivityKey = "";
    this.inactivityTimer = null;
    this.inactivityToken = 0;
    this.stageDelayMs = INACTIVITY_DELAY_MS;
    this.lastEncouragementIndex = -1;
    this.pendingCueCount = 0;
    this.cueQueue = Promise.resolve();

    // when an encouragement clip finishes and the user is still stuck on the
    // same stage/question, re-arm the idle timer so we keep nudging them.
    this.handleEncouragementEnded = () => {
      if (this.activeActivityKey) {
        // re-arm with the same delay this stage was started with
        this.watchStage(this.stageDelayMs);
      }
    };

    const unlock = () => {
      if (this.activeActivityKey) {
        this.playBackground();
      }
    };

    document.addEventListener("pointerdown", unlock, { capture: true });
    document.addEventListener("keydown", unlock, { capture: true });
  }

  startActivity(activityKey) {
    const cleanKey = String(activityKey || "");
    if (!cleanKey) return;

    if (this.activeActivityKey !== cleanKey) {
      this.stopInactivity();
      this.stopEncouragement();
      this.background.currentTime = 0;
      this.activeActivityKey = cleanKey;
    }

    this.playBackground();
  }

  playBackground() {
    if (!this.activeActivityKey || !this.background.paused) return;
    this.background.play().catch(() => {
      // Browsers may wait for the first click/key before allowing audio.
    });
  }

  watchStage(delayMs = INACTIVITY_DELAY_MS) {
    if (!this.activeActivityKey) return;

    this.stageDelayMs = delayMs;
    this.stopInactivity();
    const token = ++this.inactivityToken;
    const activityKey = this.activeActivityKey;

    this.inactivityTimer = window.setTimeout(() => {
      if (
        token !== this.inactivityToken ||
        activityKey !== this.activeActivityKey
      ) {
        return;
      }

      this.playRandomEncouragement();
    }, delayMs);
  }

  stopInactivity() {
    if (this.inactivityTimer) {
      window.clearTimeout(this.inactivityTimer);
      this.inactivityTimer = null;
    }
    this.inactivityToken += 1;
  }

  stageSucceeded() {
    this.stopInactivity();
    this.stopEncouragement();
    return this.enqueueCue(AUDIO_FILES.stageSuccess);
  }

  completeActivity(activityKey) {
    if (activityKey && this.activeActivityKey && activityKey !== this.activeActivityKey) {
      return Promise.resolve();
    }

    this.stopActivity();
    return this.enqueueCue(AUDIO_FILES.activitySuccess);
  }

  stopActivity() {
    this.stopInactivity();
    this.stopEncouragement();
    this.activeActivityKey = "";
    this.background.pause();
    this.background.currentTime = 0;
  }

  shutdown() {
    this.stopActivity();
    this.cue.pause();
    this.cue.removeAttribute("src");
    this.cue.load();
  }

  playRandomEncouragement() {
    if (!this.activeActivityKey || this.pendingCueCount > 0) return;

    const files = AUDIO_FILES.encouragement;
    let nextIndex = Math.floor(Math.random() * files.length);

    if (files.length > 1 && nextIndex === this.lastEncouragementIndex) {
      nextIndex = (nextIndex + 1) % files.length;
    }

    this.lastEncouragementIndex = nextIndex;
    this.stopEncouragement();
    this.encouragement.src = files[nextIndex];
    this.encouragement.currentTime = 0;
    this.encouragement.addEventListener("ended", this.handleEncouragementEnded, { once: true });
    this.encouragement.play().catch(() => {});
  }

  stopEncouragement() {
    this.encouragement.removeEventListener("ended", this.handleEncouragementEnded);
    this.encouragement.pause();
    this.encouragement.currentTime = 0;
  }

  enqueueCue(src) {
    this.pendingCueCount += 1;
    const queued = this.cueQueue
      .catch(() => {})
      .then(() => this.playCueToEnd(src))
      .finally(() => {
        this.pendingCueCount = Math.max(0, this.pendingCueCount - 1);
      });

    this.cueQueue = queued;
    return queued;
  }

  playCueToEnd(src) {
    return new Promise(resolve => {
      let settled = false;
      const finish = () => {
        if (settled) return;
        settled = true;
        window.clearTimeout(fallbackTimer);
        this.cue.removeEventListener("ended", finish);
        this.cue.removeEventListener("error", finish);
        resolve();
      };
      const fallbackTimer = window.setTimeout(finish, 10000);

      this.cue.addEventListener("ended", finish, { once: true });
      this.cue.addEventListener("error", finish, { once: true });
      this.cue.src = src;
      this.cue.currentTime = 0;

      const playPromise = this.cue.play();
      if (playPromise?.catch) {
        playPromise.catch(finish);
      }
    });
  }
}

export const gameAudio = new GameAudioController();
