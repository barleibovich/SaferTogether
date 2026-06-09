using UnityEngine;

namespace SaferTogether.UnityClient
{
    // auto-spawns the auth UI when we hit play mode
    public static class RuntimeAuthScene
    {
        // make the auth controller if the scene doesn't have one yet
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateController()
        {
            if (Application.productName == "mission-room" || Object.FindAnyObjectByType<MissionRoomController>() != null)
            {
                return;
            }

            if (Object.FindAnyObjectByType<SaferTogetherAuthController>() != null)
            {
                return;
            }

            var controllerObject = new GameObject("SaferTogether Auth Controller");
            controllerObject.AddComponent<SaferTogetherAuthController>();
        }
    }
}
