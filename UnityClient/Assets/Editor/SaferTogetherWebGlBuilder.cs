using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaferTogether.UnityClient.Editor
{
    /// <summary>
    /// Builds the Unity avatar editor as a WebGL module for the existing web app.
    /// </summary>
    public static class SaferTogetherWebGlBuilder
    {
        private const string ScenePath = "Assets/Scenes/RuntimeAvatar.unity";
        private const string ProductName = "avatar-editor";

        /// <summary>
        /// This function builds the WebGL avatar editor into the frontend Unity folder.
        /// </summary>
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

        /// <summary>
        /// This function returns the web app folder where the Unity build should live.
        /// </summary>
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

        /// <summary>
        /// This function creates a minimal saved scene for WebGL builds when needed.
        /// </summary>
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
    }
}
