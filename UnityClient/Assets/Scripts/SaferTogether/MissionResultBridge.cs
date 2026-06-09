using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SaferTogether.UnityClient
{
    // sends the room's mission stuff back to the web page it's embedded in
    public static class MissionResultBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // web function that receives mission result json
        [DllImport("__Internal")]
        private static extern void SaferTogetherSubmitMissionResult(string json);

        // web function that says the mission room loaded
        [DllImport("__Internal")]
        private static extern void SaferTogetherMissionRoomAck();

        // web function that opens the radio wire puzzle
        [DllImport("__Internal")]
        private static extern void SaferTogetherOpenRadioWire();
#endif

        // ask the web page to open the radio wire puzzle
        public static void OpenRadioWire()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaferTogetherOpenRadioWire();
#else
            Debug.Log("Open radio wire puzzle");
#endif
        }

        // tell the web page we got the mission and we're ready, so it stops resending it
        public static void NotifyLoaded()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaferTogetherMissionRoomAck();
#else
            Debug.Log("Mission room loaded");
#endif
        }

        // send one finished task back to the web page
        public static void Submit(string missionId, string target, string action, bool completed, string selectedChannel = "", string answer = "")
        {
            var result = new MissionResultMessage
            {
                action = action,
                answer = answer,
                completed = completed,
                missionId = missionId,
                selectedChannel = selectedChannel,
                source = "unity-room",
                target = target
            };

            string json = JsonUtility.ToJson(result);

#if UNITY_WEBGL && !UNITY_EDITOR
            SaferTogetherSubmitMissionResult(json);
#else
            Debug.Log("Mission result: " + json);
#endif
        }
    }

    // the message shape we serialize to json and send to the web page
    [Serializable]
    public class MissionResultMessage
    {
        public string action;
        public string answer;
        public bool completed;
        public string missionId;
        public string selectedChannel;
        public string source;
        public string target;
    }
}
