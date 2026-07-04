mergeInto(LibraryManager.library, {
  // just send the browser to a new url
  SaferTogetherNavigate: function (urlPointer) {
    window.location.href = UTF8ToString(urlPointer);
  },

  // parse the mission json and tell the page we finished
  SaferTogetherSubmitMissionResult: function (jsonPointer) {
    var payload = {};

    try {
      payload = JSON.parse(UTF8ToString(jsonPointer));
    } catch (error) {
      payload = {
        error: "Invalid mission result JSON"
      };
    }

    if (typeof window.saferTogetherMissionCompleted === "function") {
      window.saferTogetherMissionCompleted(payload);
    }
  },

  // ping the page that the mission room is ready
  SaferTogetherMissionRoomAck: function () {
    if (typeof window.saferTogetherMissionRoomAck === "function") {
      window.saferTogetherMissionRoomAck();
    }
  },

  // tell the page that one task inside the room was completed
  SaferTogetherMissionStageCompleted: function (targetPointer) {
    if (typeof window.saferTogetherMissionStageCompleted === "function") {
      window.saferTogetherMissionStageCompleted({
        target: UTF8ToString(targetPointer)
      });
    }
  },

  // tell the page the player advanced a step inside a task (resets the idle nudge timer)
  SaferTogetherMissionStageProgress: function () {
    if (typeof window.saferTogetherMissionStageProgress === "function") {
      window.saferTogetherMissionStageProgress();
    }
  },

  // current phone tilt (deviceorientation); returns 999 when there's no reading yet,
  // so the game knows not to move instead of treating "no data" as a tilt
  SaferTogetherGetTiltGamma: function () {
    return (typeof window.__saferTiltGamma === "number") ? window.__saferTiltGamma : 999;
  },

  SaferTogetherGetTiltBeta: function () {
    return (typeof window.__saferTiltBeta === "number") ? window.__saferTiltBeta : 999;
  }
});
