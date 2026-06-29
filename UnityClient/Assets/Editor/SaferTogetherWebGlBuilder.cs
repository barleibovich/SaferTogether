using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaferTogether.UnityClient.Editor
{
    // builds the avatar editor as a webgl module for the web app
    public static class SaferTogetherWebGlBuilder
    {
        private const string AvatarEditorScenePath = "Assets/Scenes/RuntimeAvatar.unity";
        private const string MissionRoomScenePath = "Assets/Scenes/MissionRoom.unity";
        private const string ProductName = "avatar-editor";
        private const string MissionRoomProductName = "mission-room";

        // build the webgl avatar editor into the frontend folder
        [MenuItem("SaferTogether/Build WebGL Avatar Editor")]
        public static void BuildAvatarEditor()
        {
            string outputPath = GetFrontendBuildPath();

            string previousProductName = PlayerSettings.productName;
            PlayerSettings.productName = ProductName;

            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                // rebuild every avatar prefab/thumbnail from the pack, register the avatar
                // shaders as Always Included (so they aren't stripped → pink), and rewrite the
                // RuntimeAvatar scene with the new editor controller.
                SaferTogetherAvatarPackInstaller.Generate(true);
                Directory.CreateDirectory(outputPath);

                BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    locationPathName = outputPath,
                    options = BuildOptions.None,
                    scenes = new[] { AvatarEditorScenePath },
                    target = BuildTarget.WebGL
                });
            }
            finally
            {
                PlayerSettings.productName = previousProductName;
            }
        }

        // build the mission room into the frontend folder
        [MenuItem("SaferTogether/Build WebGL Mission Room")]
        public static void BuildMissionRoom()
        {
            string outputPath = GetFrontendMissionRoomBuildPath();

            string previousProductName = PlayerSettings.productName;
            PlayerSettings.productName = MissionRoomProductName;

            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                // mission room spawns the same pack characters; rebuild the prefabs/shaders but
                // leave the avatar-editor scene alone (the mission room uses its own scene).
                SaferTogetherAvatarPackInstaller.Generate(false);
                SaferTogetherMissionGamesInstaller.Generate();
                EnsureMissionRoomScene();
                Directory.CreateDirectory(outputPath);

                BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    locationPathName = outputPath,
                    options = BuildOptions.None,
                    scenes = new[] { MissionRoomScenePath },
                    target = BuildTarget.WebGL
                });
            }
            finally
            {
                PlayerSettings.productName = previousProductName;
            }
        }

        // where the avatar editor build should go in the web app
        private static string GetFrontendBuildPath()
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "SaferTogetherUI",
                "unity",
                "avatar-editor"
            ));
        }

        // where the mission room build should go in the web app
        private static string GetFrontendMissionRoomBuildPath()
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "SaferTogetherUI",
                "unity",
                "mission-room"
            ));
        }

        // set up the mission room scene the build uses
        private static void EnsureMissionRoomScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MissionRoomScenePath));
            Scene scene = File.Exists(MissionRoomScenePath)
                ? EditorSceneManager.OpenScene(MissionRoomScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MissionRoomController controller = Object.FindAnyObjectByType<MissionRoomController>();

            if (controller == null)
            {
                var controllerObject = new GameObject("SaferTogether Mission Room Controller");
                controllerObject.AddComponent<MissionRoomController>();
            }

            EditorSceneManager.SaveScene(scene, MissionRoomScenePath);
            AssetDatabase.Refresh();
        }
    }
}
