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
        private const string ScenePath = "Assets/Scenes/RuntimeAvatar.unity";
        private const string MissionRoomScenePath = "Assets/Scenes/MissionRoom.unity";
        private const string ProductName = "avatar-editor";
        private const string MissionRoomProductName = "mission-room";

        // build the webgl avatar editor into the frontend folder
        [MenuItem("SaferTogether/Build WebGL Avatar Editor")]
        public static void BuildAvatarEditor()
        {
            string outputPath = GetFrontendBuildPath();
            Directory.CreateDirectory(outputPath);

            string previousProductName = PlayerSettings.productName;
            PlayerSettings.productName = ProductName;

            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                EnsureRuntimeScene();
                SaferTogetherGeneratedAvatarInstaller.GenerateAssets(true);
                SaferTogetherGeneratedAvatarInstaller.InstallBuilderInRuntimeScene();

                BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    locationPathName = outputPath,
                    options = BuildOptions.None,
                    scenes = new[] { ScenePath },
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
            Directory.CreateDirectory(outputPath);

            string previousProductName = PlayerSettings.productName;
            PlayerSettings.productName = MissionRoomProductName;

            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                EnsureMissionRoomScene();

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

        // make a basic saved scene for the build if we don't have one
        private static void EnsureRuntimeScene()
        {
            if (File.Exists(ScenePath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
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
