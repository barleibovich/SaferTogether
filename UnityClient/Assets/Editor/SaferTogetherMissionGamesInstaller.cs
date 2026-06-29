using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SaferTogether.UnityClient.Editor
{
    // Imports the mission mini-game art from C:\SaferTogether\MissionRoomGames3D into runtime assets:
    //   * puzzle items  -> Resources/MissionGames/Puzzle/stage<N>/<item>.png
    //   * missile       -> Resources/MissionGames/missile.png
    //   * keypad        -> Resources/MissionGames/keypad.png  (panel backdrop)
    // The missile FBX is rendered to a flat thumbnail (camera + light + RenderTexture).
    // Run "SaferTogether/Import Mission Games", then "Build WebGL Mission Room".
    public static class SaferTogetherMissionGamesInstaller
    {
        private const string SourceFolderName = "MissionRoomGames3D";
        private const string ImportRoot = "Assets/MissionGames/Source";
        private const string ResourcesRoot = "Assets/Resources/MissionGames";
        private const int ThumbWidth = 256;
        private const int ThumbHeight = 256;
        private const int ThumbnailLayer = 31;

        [MenuItem("SaferTogether/Import Mission Games")]
        public static void ImportMenu()
        {
            Generate();
            EditorUtility.DisplayDialog(
                "SaferTogether",
                "Mission game art imported.\n\nNow run SaferTogether > Build WebGL Mission Room.",
                "OK");
        }

        // the mission-room WebGL builder calls this so a build always has fresh art.
        public static void Generate()
        {
            string sourceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", SourceFolderName));

            if (!Directory.Exists(sourceRoot))
            {
                Debug.LogWarning("[Mission Games] Source folder not found: " + sourceRoot);
                return;
            }

            EnsureFolder("Assets", "MissionGames");
            EnsureFolder("Assets/MissionGames", "Source");
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "MissionGames");
            EnsureFolder(ResourcesRoot, "Puzzle");

            ImportKeypad(sourceRoot);
            ImportMissile(sourceRoot);
            ImportPuzzle(sourceRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mission Games] Import complete.");
        }

        // keypad backdrop: just import the diffuse texture as a sprite
        private static void ImportKeypad(string sourceRoot)
        {
            string source = Path.Combine(sourceRoot, "Door Code Sequence Game", "keypad", "texture_diffuse.png");

            if (!File.Exists(source))
            {
                Debug.LogWarning("[Mission Games] Keypad texture not found: " + source);
                return;
            }

            string dest = ResourcesRoot + "/keypad.png";
            File.Copy(source, AssetPathToFullPath(dest), true);
            AssetDatabase.ImportAsset(dest);
            ConfigureSpriteImporter(dest);
        }

        // missile: import the FBX, render it to a flat thumbnail sprite
        private static void ImportMissile(string sourceRoot)
        {
            string sourceDir = Path.Combine(sourceRoot, "missle game", "missle");

            if (!Directory.Exists(sourceDir))
            {
                Debug.LogWarning("[Mission Games] Missile folder not found: " + sourceDir);
                return;
            }

            string fbx = CopyModel(sourceDir, "Missile");
            RenderThumbnail(fbx, ResourcesRoot + "/missile.png");
        }

        // puzzle: every image under puzzle game / 2D / Stage N becomes
        // Resources/MissionGames/Puzzle/stageN/<item>.png.
        private static void ImportPuzzle(string sourceRoot)
        {
            string puzzleRoot = Path.Combine(sourceRoot, "puzzle game", "2D");

            if (!Directory.Exists(puzzleRoot))
            {
                Debug.LogWarning("[Mission Games] Puzzle 2D folder not found: " + puzzleRoot);
                return;
            }

            for (int stage = 1; stage <= 4; stage++)
            {
                string stageDir = Path.Combine(puzzleRoot, "Stage " + stage);

                if (!Directory.Exists(stageDir))
                {
                    Debug.LogWarning("[Mission Games] Missing puzzle stage folder: " + stageDir);
                    continue;
                }

                EnsureFolder(ResourcesRoot + "/Puzzle", "stage" + stage);
                string stageAssetDir = ResourcesRoot + "/Puzzle/stage" + stage;
                ClearGeneratedSprites(stageAssetDir);

                foreach (string file in Directory.GetFiles(stageDir))
                {
                    string extension = Path.GetExtension(file).ToLowerInvariant();

                    if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                    {
                        continue;
                    }

                    string key = NormalizeKey(Path.GetFileNameWithoutExtension(file));
                    string dest = stageAssetDir + "/" + key + ".png";
                    File.Copy(file, AssetPathToFullPath(dest), true);
                    AssetDatabase.ImportAsset(dest);
                    ConfigureSpriteImporter(dest);
                }
            }
        }

        // copy base.fbx + its textures into Assets/MissionGames/Source/<destName> and import the FBX.
        private static string CopyModel(string sourceDir, string destName)
        {
            return CopyModelInto(sourceDir, ImportRoot + "/" + destName);
        }

        // copy base.fbx + its colour/normal maps into destAssetDir and import the FBX (URP materials).
        // returns the imported FBX asset path. destAssetDir may live under Resources so the model
        // can be loaded at runtime.
        private static string CopyModelInto(string sourceDir, string destAssetDir)
        {
            Directory.CreateDirectory(AssetPathToFullPath(destAssetDir));

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string lower = Path.GetFileName(file).ToLowerInvariant();

                // bring the mesh + its colour/normal maps; skip the metallic/roughness/pbr extras
                bool wanted = lower == "base.fbx"
                    || lower == "shaded.png"
                    || lower == "texture_diffuse.png"
                    || lower == "texture_normal.png";

                if (wanted)
                {
                    File.Copy(file, AssetPathToFullPath(destAssetDir + "/" + Path.GetFileName(file)), true);
                }
            }

            AssetDatabase.Refresh();

            string fbxPath = destAssetDir + "/base.fbx";

            if (AssetImporter.GetAtPath(fbxPath) is ModelImporter importer
                && importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.SaveAndReimport();
            }

            return fbxPath;
        }

        // render an FBX to a transparent square sprite
        private static void RenderThumbnail(string fbxPath, string outResourcePath, bool flip180 = false)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

            if (model == null)
            {
                Debug.LogWarning("[Mission Games] FBX missing: " + fbxPath);
                return;
            }

            GameObject instance = null;
            Camera camera = null;
            GameObject lightObject = null;
            GameObject fillLightObject = null;
            Material thumbnailMaterial = null;
            RenderTexture renderTexture = null;
            RenderTexture previousActive = RenderTexture.active;

            // a single hard light with no ambient leaves every shadowed face black, so the items
            // bake out dark and muddy and you can't read their real colours. Flood the bake with
            // flat ambient + a fill light, then restore the scene's lighting afterwards.
            UnityEngine.Rendering.AmbientMode previousAmbientMode = RenderSettings.ambientMode;
            Color previousAmbientLight = RenderSettings.ambientLight;
            float previousAmbientIntensity = RenderSettings.ambientIntensity;

            try
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.78f, 0.78f, 0.78f, 1f);
                RenderSettings.ambientIntensity = 1f;

                instance = UnityEngine.Object.Instantiate(model);
                instance.transform.position = new Vector3(6000f, 6000f, 6000f);
                instance.transform.rotation = Quaternion.Euler(15f, 25f, flip180 ? 180f : 0f);
                thumbnailMaterial = ApplyThumbnailMaterial(instance, Path.GetDirectoryName(fbxPath));
                SetLayerRecursively(instance, ThumbnailLayer);

                if (!TryGetBounds(instance, out Bounds bounds))
                {
                    return;
                }

                renderTexture = new RenderTexture(ThumbWidth, ThumbHeight, 24, RenderTextureFormat.ARGB32);
                renderTexture.Create();

                var cameraObject = new GameObject("Thumb Camera");
                cameraObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - Mathf.Max(bounds.size.magnitude, 1f));
                cameraObject.transform.LookAt(bounds.center);
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.25f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.cullingMask = 1 << ThumbnailLayer;
                camera.targetTexture = renderTexture;

                // key light gives the item its form; toned down because flat ambient now carries the base
                lightObject = new GameObject("Thumb Light", typeof(Light));
                lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 0.8f;

                // fill light from the opposite side so the far face keeps its real colour instead of going black
                fillLightObject = new GameObject("Thumb Fill Light", typeof(Light));
                fillLightObject.transform.rotation = Quaternion.Euler(-15f, 150f, 0f);
                Light fillLight = fillLightObject.GetComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.intensity = 0.45f;

                camera.Render();

                var texture = new Texture2D(ThumbWidth, ThumbHeight, TextureFormat.RGBA32, false);
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, ThumbWidth, ThumbHeight), 0, 0);
                texture.Apply();
                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);

                File.WriteAllBytes(AssetPathToFullPath(outResourcePath), png);
                AssetDatabase.ImportAsset(outResourcePath);
                ConfigureSpriteImporter(outResourcePath);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Mission Games] Thumbnail failed for " + fbxPath + ": " + error.Message);
            }
            finally
            {
                RenderTexture.active = previousActive;

                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.ambientIntensity = previousAmbientIntensity;

                if (camera != null)
                {
                    camera.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }

                if (fillLightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(fillLightObject);
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                if (thumbnailMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(thumbnailMaterial);
                }
            }
        }

        private static Material ApplyThumbnailMaterial(GameObject instance, string assetDir)
        {
            assetDir = assetDir.Replace("\\", "/");
            Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(assetDir + "/texture_diffuse.png")
                ?? AssetDatabase.LoadAssetAtPath<Texture2D>(assetDir + "/shaded.png");

            if (diffuse == null)
            {
                return null;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Unlit/Texture");

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "Mission Thumbnail Material"
            };

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", diffuse);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", diffuse);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;

                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (int i = 0; i < materials.Length; i += 1)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }

            return material;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    if (!any)
                    {
                        bounds = renderer.bounds;
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return any;
        }

        private static void ConfigureSpriteImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void ClearGeneratedSprites(string assetDir)
        {
            string fullDir = AssetPathToFullPath(assetDir);

            if (!Directory.Exists(fullDir))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(fullDir))
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();

                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    continue;
                }

                string assetPath = (assetDir + "/" + Path.GetFileName(file)).Replace("\\", "/");
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static string NormalizeKey(string name)
        {
            return name.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;

            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }
}
