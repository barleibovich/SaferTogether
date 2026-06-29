using UnityEngine;
namespace SaferTogether.UnityClient
{
    // auto-spawns the avatar editor when we hit play mode (or load the WebGL build)
    public static class RuntimeAuthScene
    {
        // make the editor controller if the scene doesn't have one yet. the GameObject name must
        // stay "SaferTogether Auth Controller" so the website's SendMessage call keeps working.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateController()
        {
            if (Application.productName == "mission-room" || Object.FindAnyObjectByType<MissionRoomController>() != null)
            {
                return;
            }

            if (Object.FindAnyObjectByType<SaferTogetherAvatarEditor>() != null)
            {
                return;
            }

            var controllerObject = new GameObject("SaferTogether Auth Controller");
            controllerObject.AddComponent<SaferTogetherAvatarEditor>();
        }
    }
}
