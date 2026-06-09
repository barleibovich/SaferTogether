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

  // open the radio wire thing on the page
  SaferTogetherOpenRadioWire: function () {
    if (typeof window.saferTogetherOpenRadioWire === "function") {
      window.saferTogetherOpenRadioWire();
    }
  }
});
