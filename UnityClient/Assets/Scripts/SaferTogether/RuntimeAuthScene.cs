using UnityEngine;

namespace SaferTogether.UnityClient
{
    /// <summary>
    /// Creates a minimal auth UI automatically when the Unity project enters Play Mode.
    /// </summary>
    public static class RuntimeAuthScene
    {
        /// <summary>
        /// This function creates the auth controller when none exists in the scene.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateController()
        {
            if (Object.FindAnyObjectByType<SaferTogetherAuthController>() != null)
            {
                return;
            }

            var controllerObject = new GameObject("SaferTogether Auth Controller");
            controllerObject.AddComponent<SaferTogetherAuthController>();
        }
    }
}
