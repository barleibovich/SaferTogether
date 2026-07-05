using UnityEngine;

namespace SaferTogether.UnityClient
{
    // Tracks how much the phone was rotated (from the browser deviceorientation bridge),
    // so EVERY mission mini-game/stage can report a rotation value - not just the missile
    // game. It measures the child's hand movement even when tilt doesn't steer anything.
    // MissionRoomController.Update() calls Sample() each frame; each stage calls Take().
    public static class MissionTilt
    {
        private static float accumulated;
        private static bool hasLast;
        private static float lastGamma;
        private static float lastBeta;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern float SaferTogetherGetTiltGamma();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern float SaferTogetherGetTiltBeta();
#endif

        // call once per frame while the mission room is open
        public static void Sample()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            float gamma = SaferTogetherGetTiltGamma();
            float beta = SaferTogetherGetTiltBeta();

            // 999 = no reading yet (or no permission) -> don't count it
            if (Mathf.Abs(gamma) < 900f && Mathf.Abs(beta) < 900f)
            {
                if (hasLast)
                {
                    accumulated += Mathf.Abs(Mathf.DeltaAngle(lastGamma, gamma))
                                 + Mathf.Abs(Mathf.DeltaAngle(lastBeta, beta));
                }

                lastGamma = gamma;
                lastBeta = beta;
                hasLast = true;
            }
#endif
        }

        // rotation (degrees) accumulated since the last take, then reset for the next stage
        public static float Take()
        {
            float value = Mathf.Round(accumulated * 100f) / 100f;
            accumulated = 0f;
            return value;
        }

        // start fresh when a new mission room opens
        public static void Reset()
        {
            accumulated = 0f;
            hasLast = false;
        }
    }
}
