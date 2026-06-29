using System;
using System.IO;
using SaferTogether.UnityClient;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaferTogether.UnityClient.Editor
{
    // Rebuilds the whole avatar editor from the Quaternius pack at C:\SaferTogether\Avatars:
    //   * imports the 11 Humanoid-rigged character FBX
    //   * assigns the URP vertex-color material so they render their real colors (not pink)
    //   * builds a shared Idle/Walk Humanoid controller
    //   * writes one prefab + one thumbnail per character under Resources/SaferTogetherAvatars
    //   * registers the avatar shaders as Always Included so the WebGL build never strips them
    //   * rebuilds Assets/Scenes/RuntimeAvatar.unity with the SaferTogetherAvatarEditor controller
    // Run "SaferTogether/Rebuild Avatar Editor (Avatars Pack)" then "Build WebGL Avatar Editor".
    public static class SaferTogetherAvatarPackInstaller
    {
        private const string SourceFbxRoot = "Assets/Avatar3D/UltimateCharacters";
        private const string AvatarResourcesRoot = "Assets/Resources/" + AvatarCatalog.ResourcesRoot;
        private const string ThumbnailsRoot = AvatarResourcesRoot + "/Thumbs";
        private const string ControllerPath = "Assets/Avatar3D/safer-pack-humanoid.controller";
        private const string VertexMaterialPath = "Assets/Avatar3D/vertex-color-lit.mat";
        private const string VertexShaderName = "SaferTogether/VertexColorLit";
        private const string LegacyResourcesRoot = "Assets/Resources/GeneratedAvatarBuilder";
        private const string RuntimeScenePath = "Assets/Scenes/RuntimeAvatar.unity";
        private const string EditorUiResourcesRoot = "Assets/Resources/AvatarEditorUI";

        // pack animation source (preferred), then the already-imported UAL set as a fallback.
        private const string PackAnimationsAsset = "Assets/Avatar3D/PackAnimations.fbx";
        private const string FallbackAnimationsAsset = "Assets/Quaternius/SaferTogether/Animations/UAL1_Standard.fbx";

        private const float TargetBodyHeight = 3.2f;
        private static readonly Vector3 BodyCenter = new Vector3(0f, 1.08f, 0f);
        private const int ThumbnailLayer = 31;
        private const int ThumbWidth = 256;
        private const int ThumbHeight = 360;

        // character id (matches AvatarCatalog.Characters) -> FBX file name inside the pack.
        private static readonly string[][] Characters =
        {
            new[] { "adventurer", "Adventurer.fbx" },
            new[] { "beach", "Beach.fbx" },
            new[] { "casual", "Casual.fbx" },
            new[] { "casual2", "Casual2.fbx" },
            new[] { "farmer", "Farmer.fbx" },
            new[] { "king", "King.fbx" },
            new[] { "punk", "Punk.fbx" },
            new[] { "spacesuit", "Spacesuit.fbx" },
            new[] { "suit", "Suit.fbx" },
            new[] { "swat", "Swat.fbx" },
            new[] { "worker", "Worker.fbx" }
        };

        [MenuItem("SaferTogether/Rebuild Avatar Editor (Avatars Pack)")]
        public static void RebuildMenu()
        {
            Generate(true);
            EditorUtility.DisplayDialog(
                "SaferTogether",
                "Avatar editor rebuilt from the Avatars pack.\n\nNow run SaferTogether > Build WebGL Avatar Editor.",
                "OK");
        }

        // full rebuild. the WebGL builder calls this (rebuildScene = true) before building.
        public static void Generate(bool rebuildScene)
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", AvatarCatalog.ResourcesRoot);
            EnsureFolder(AvatarResourcesRoot, "Thumbs");
            EnsureFolder("Assets", "Avatar3D");

            RemoveLegacyResources();

            CopyMissingFbxFromPack();
            AssetDatabase.Refresh();

            foreach (string[] entry in Characters)
            {
                ConfigureHumanoid(SourceFbxPath(entry[0]));
            }

            AnimatorController controller = EnsureController();

            foreach (string[] entry in Characters)
            {
                BuildPrefab(entry[0], controller);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AnimationClip thumbnailPose = FindIdleClip();

            foreach (string[] entry in Characters)
            {
                GenerateThumbnail(entry[0], thumbnailPose);
            }

            EnsureShadersAlwaysIncluded();
            EnsureEditorUiSprites();

            if (rebuildScene)
            {
                InstallRuntimeScene();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Avatar Pack] Rebuild complete (" + Characters.Length + " characters).");
        }

        // delete the old modular/generated avatar prefabs so nothing references removed scripts.
        private static void RemoveLegacyResources()
        {
            if (AssetDatabase.IsValidFolder(LegacyResourcesRoot))
            {
                AssetDatabase.DeleteAsset(LegacyResourcesRoot);
            }
        }

        // copy any missing character FBX out of the pack (Humanoid Rig version) into the project.
        private static void CopyMissingFbxFromPack()
        {
            EnsureFolder("Assets", "Avatar3D");
            EnsureFolder("Assets/Avatar3D", "UltimateCharacters");

            string packRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Avatars"));
            bool packExists = Directory.Exists(packRoot);

            foreach (string[] entry in Characters)
            {
                string destFull = AssetPathToFullPath(SourceFbxPath(entry[0]));

                if (File.Exists(destFull))
                {
                    continue;
                }

                if (!packExists)
                {
                    Debug.LogWarning("[Avatar Pack] Missing FBX for " + entry[0] + " and pack folder not found: " + packRoot);
                    continue;
                }

                string source = FindHumanoidFbx(packRoot, entry[1]);

                if (source == null)
                {
                    Debug.LogWarning("[Avatar Pack] Source FBX not found in pack: " + entry[1]);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destFull));
                File.Copy(source, destFull, true);
            }
        }

        // prefer the Humanoid Rig / Individual Characters copy inside the timestamped pack folders.
        private static string FindHumanoidFbx(string packRoot, string fbxName)
        {
            string[] matches = Directory.GetFiles(packRoot, fbxName, SearchOption.AllDirectories);
            string fallback = null;

            foreach (string match in matches)
            {
                string normalized = match.Replace("\\", "/");

                if (normalized.Contains("Humanoid Rig") && normalized.Contains("Individual Characters"))
                {
                    return match;
                }

                fallback ??= match;
            }

            return fallback;
        }

        // set the FBX rig to Humanoid (so the shared Idle/Walk clips retarget) AND import its
        // materials through the active pipeline (URP) so every part keeps its baseColor. these
        // characters are coloured per-material (no vertex colors), so we must keep the FBX
        // materials, not override them. forces a reimport so the materials regenerate as URP/Lit
        // now that URP is the active pipeline.
        private static void ConfigureHumanoid(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not ModelImporter importer)
            {
                return;
            }

            bool dirty = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                dirty = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                dirty = true;
            }

            // only reimport when something actually changed, so repeat rebuilds/builds stay fast.
            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        // build the shared Humanoid Idle/Walk controller from the bundled Quaternius animation
        // library (proven to retarget onto these characters), falling back to the pack's own
        // Animations.fbx if the library is absent.
        private static AnimatorController EnsureController()
        {
            if (File.Exists(AssetPathToFullPath(FallbackAnimationsAsset)))
            {
                ConfigureHumanoid(FallbackAnimationsAsset);
            }

            AnimationClip idle = FindIdleClip();
            AnimationClip walk = FindWalkClip();

            if (File.Exists(AssetPathToFullPath(ControllerPath)))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(AvatarCatalog.WalkingParam, AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idle;
            sm.defaultState = idleState;

            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = walk;

            AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.15f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, AvatarCatalog.WalkingParam);

            AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, AvatarCatalog.WalkingParam);

            if (idle == null && walk == null)
            {
                Debug.LogWarning("[Avatar Pack] No idle/walk clips found; characters will be static.");
            }

            return controller;
        }

        // pick a relaxed standing idle (avoid combat/crouch/action variants that look like sitting).
        private static AnimationClip FindIdleClip()
        {
            return ResolveClip(
                new[] { "Idle_Loop", "Idle", "Idle_A", "IdleNeutral", "Breathing_Idle" },
                "idle",
                new[] { "combat", "crouch", "sit", "aim", "gun", "sword", "weapon", "death", "hit", "attack", "block" });
        }

        // pick a forward walk (avoid back/strafe/combat variants).
        private static AnimationClip FindWalkClip()
        {
            return ResolveClip(
                new[] { "Walk_Loop", "Walk", "Walk_A", "WalkForward" },
                "walk",
                new[] { "back", "strafe", "crouch", "combat", "aim", "gun", "sword", "left", "right" });
        }

        // resolve a humanoid clip: prefer an exact known name, else the shortest keyword match that
        // avoids unwanted variants. searches the bundled UAL set first, then the pack animations.
        private static AnimationClip ResolveClip(string[] exactNames, string keyword, string[] avoid)
        {
            foreach (string assetPath in new[] { FallbackAnimationsAsset, EnsurePackAnimations() })
            {
                if (string.IsNullOrEmpty(assetPath) || !File.Exists(AssetPathToFullPath(assetPath)))
                {
                    continue;
                }

                var clips = new System.Collections.Generic.List<AnimationClip>();

                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    {
                        clips.Add(clip);
                    }
                }

                foreach (string exact in exactNames)
                {
                    foreach (AnimationClip clip in clips)
                    {
                        if (string.Equals(clip.name, exact, StringComparison.OrdinalIgnoreCase))
                        {
                            return clip;
                        }
                    }
                }

                AnimationClip best = null;

                foreach (AnimationClip clip in clips)
                {
                    if (clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                        && !ContainsAny(clip.name, avoid)
                        && (best == null || clip.name.Length < best.name.Length))
                    {
                        best = clip;
                    }
                }

                if (best != null)
                {
                    return best;
                }

                foreach (AnimationClip clip in clips)
                {
                    if (clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return clip;
                    }
                }
            }

            return null;
        }

        private static bool ContainsAny(string name, string[] words)
        {
            foreach (string word in words)
            {
                if (name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        // copy + humanoid-import the pack's Animations.fbx (returns null if the pack lacks it).
        private static string EnsurePackAnimations()
        {
            if (!File.Exists(AssetPathToFullPath(PackAnimationsAsset)))
            {
                string packRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Avatars"));

                if (!Directory.Exists(packRoot))
                {
                    return null;
                }

                string[] matches = Directory.GetFiles(packRoot, "Animations.fbx", SearchOption.AllDirectories);

                if (matches.Length == 0)
                {
                    return null;
                }

                File.Copy(matches[0], AssetPathToFullPath(PackAnimationsAsset), true);
                AssetDatabase.Refresh();
            }

            ConfigureHumanoid(PackAnimationsAsset);
            return PackAnimationsAsset;
        }

        // create/load the shared URP vertex-color material (renders the Quaternius vertex colors).
        private static Material EnsureVertexColorMaterial()
        {
            Shader custom = Shader.Find(VertexShaderName);
            Shader shader = (custom != null && !ShaderUtil.ShaderHasError(custom))
                ? custom
                : Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                Debug.LogError("[Avatar Pack] No URP shader found — is URP installed?");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(VertexMaterialPath);

            if (material == null)
            {
                material = new Material(shader) { name = "vertex-color-lit" };
                AssetDatabase.CreateAsset(material, VertexMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        // build Resources/SaferTogetherAvatars/<id>.prefab: root > Model(scaled) > FBX + Animator.
        private static void BuildPrefab(string character, AnimatorController controller)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath(character));

            if (model == null)
            {
                Debug.LogWarning("[Avatar Pack] Imported FBX missing for " + character);
                return;
            }

            GameObject body = UnityEngine.Object.Instantiate(model);
            var root = new GameObject(character);
            var modelHolder = new GameObject("Model");
            modelHolder.transform.SetParent(root.transform, false);
            body.transform.SetParent(modelHolder.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;

            // keep the FBX's own URP materials (each part keeps its baseColor); just make sure the
            // skinned meshes always render even when their bounds are momentarily off-camera.
            foreach (SkinnedMeshRenderer skinned in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinned.updateWhenOffscreen = true;
            }

            Animator animator = body.GetComponent<Animator>();

            if (animator == null)
            {
                animator = body.AddComponent<Animator>();
            }

            animator.avatar = LoadAvatar(SourceFbxPath(character));
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            FitModel(modelHolder.transform, body);

            Directory.CreateDirectory(AssetPathToFullPath(AvatarResourcesRoot));
            PrefabUtility.SaveAsPrefabAsset(root, AvatarResourcesRoot + "/" + character + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        // scale + recenter so the character is TargetBodyHeight tall, centered at BodyCenter.
        private static void FitModel(Transform model, GameObject body)
        {
            if (!TryGetBounds(body, out Bounds bounds) || bounds.size.y <= 0.0001f)
            {
                model.localScale = Vector3.one;
                model.localPosition = BodyCenter;
                return;
            }

            float scale = TargetBodyHeight / bounds.size.y;
            model.localScale = new Vector3(scale, scale, scale);
            model.localPosition = BodyCenter - (bounds.center * scale);
        }

        // set the shared material on every slot of every renderer under root.
        private static void AssignMaterial(GameObject root, Material material)
        {
            if (material == null)
            {
                return;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];

                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;

                if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    skinnedRenderer.updateWhenOffscreen = true;
                }
            }
        }

        // render the prefab to a transparent square PNG and import it as a Sprite for the grid.
        private static void GenerateThumbnail(string character, AnimationClip poseClip)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarResourcesRoot + "/" + character + ".prefab");

            if (prefab == null)
            {
                return;
            }

            GameObject instance = null;
            Camera camera = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                instance.transform.position = new Vector3(5000f, 5000f, 5000f);
                // characters face +Z; rotate 180 so the front faces the thumbnail camera
                instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                SetLayerRecursively(instance, ThumbnailLayer);

                // pose a relaxed idle frame instead of a stiff T-pose
                TrySamplePose(instance, poseClip);

                // skinned-mesh renderer.bounds are unreliable before play mode (they left the
                // thumbnails blank), so frame from the shared mesh bounds instead.
                if (!TryGetEditorMeshBounds(instance, out Bounds bounds))
                {
                    return;
                }

                renderTexture = new RenderTexture(ThumbWidth, ThumbHeight, 24, RenderTextureFormat.ARGB32);
                renderTexture.Create();

                var cameraObject = new GameObject("Thumbnail Camera");
                cameraObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 6f);
                cameraObject.transform.rotation = Quaternion.identity;
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.orthographic = true;
                // portrait frame: fill the height so the character is large, not a small square
                camera.orthographicSize = bounds.size.y * 0.55f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.cullingMask = 1 << ThumbnailLayer;
                camera.targetTexture = renderTexture;

                // URP/Lit materials are dark without a light, which made the thumbnails black
                // silhouettes; add a directional light so they render in colour.
                lightObject = new GameObject("Thumbnail Light", typeof(Light));
                lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                Light thumbLight = lightObject.GetComponent<Light>();
                thumbLight.type = LightType.Directional;
                thumbLight.intensity = 1.4f;

                camera.Render();

                var texture = new Texture2D(ThumbWidth, ThumbHeight, TextureFormat.RGBA32, false);
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, ThumbWidth, ThumbHeight), 0, 0);
                texture.Apply();

                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);

                string path = ThumbnailsRoot + "/" + character + ".png";
                File.WriteAllBytes(AssetPathToFullPath(path), png);
                AssetDatabase.ImportAsset(path);
                ConfigureThumbnailImporter(path);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Avatar Pack] Thumbnail failed for " + character + ": " + error.Message);
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }

                RenderTexture.active = previousActive;

                if (camera != null)
                {
                    camera.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
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
            }
        }

        // sample a relaxed idle frame so thumbnails aren't a stiff T-pose. humanoid sampling needs
        // the editor AnimationMode; on any failure we just leave the bind pose.
        private static void TrySamplePose(GameObject instance, AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(instance, clip, 1.0f);
                AnimationMode.EndSampling();
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Avatar Pack] Pose sample failed: " + error.Message);

                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }
            }
        }

        private static void ConfigureThumbnailImporter(string path)
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

        // copy the avatar-editor skin PNGs from <repo>/AvatarEditor into Resources/AvatarEditorUI
        // and import them as UI Sprites so the runtime editor can load them.
        private static void EnsureEditorUiSprites()
        {
            EnsureFolder("Assets/Resources", "AvatarEditorUI");

            string sourceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "AvatarEditor"));

            if (Directory.Exists(sourceRoot))
            {
                foreach (string source in Directory.GetFiles(sourceRoot, "*.png"))
                {
                    File.Copy(source, AssetPathToFullPath(EditorUiResourcesRoot + "/" + Path.GetFileName(source)), true);
                }

                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("[Avatar Pack] Avatar editor image folder not found: " + sourceRoot);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { EditorUiResourcesRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
        }

        // add the avatar shaders to ProjectSettings Always Included Shaders so WebGL keeps them.
        public static void EnsureShadersAlwaysIncluded()
        {
            string[] names =
            {
                VertexShaderName,
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Unlit"
            };

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");

            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Avatar Pack] Could not open GraphicsSettings to register shaders.");
                return;
            }

            var serialized = new SerializedObject(assets[0]);
            SerializedProperty list = serialized.FindProperty("m_AlwaysIncludedShaders");

            if (list == null)
            {
                return;
            }

            bool changed = false;

            foreach (string name in names)
            {
                Shader shader = Shader.Find(name);

                if (shader == null || ShaderUtil.ShaderHasError(shader))
                {
                    continue;
                }

                bool exists = false;

                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    continue;
                }

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                changed = true;
            }

            if (changed)
            {
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        // rebuild RuntimeAvatar.unity with just the editor controller (it builds its UI at runtime).
        private static void InstallRuntimeScene()
        {
            Directory.CreateDirectory(AssetPathToFullPath("Assets/Scenes"));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var controllerObject = new GameObject("SaferTogether Auth Controller");
            controllerObject.AddComponent<SaferTogetherAvatarEditor>();

            EditorSceneManager.SaveScene(scene, RuntimeScenePath);
        }

        // ---- helpers ----------------------------------------------------------------------

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
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

            return any;
        }

        // edit-mode reliable world bounds from the shared meshes (skinned renderer.bounds are
        // not updated before play mode, which left thumbnails blank).
        private static bool TryGetEditorMeshBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh != null)
                {
                    EncapsulateMeshBounds(smr.transform, smr.sharedMesh.bounds, ref bounds, ref any);
                }
            }

            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null)
                {
                    EncapsulateMeshBounds(mf.transform, mf.sharedMesh.bounds, ref bounds, ref any);
                }
            }

            return any;
        }

        private static void EncapsulateMeshBounds(Transform t, Bounds local, ref Bounds bounds, ref bool any)
        {
            Vector3 min = local.min;
            Vector3 max = local.max;

            for (int corner = 0; corner < 8; corner++)
            {
                var localCorner = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                Vector3 world = t.TransformPoint(localCorner);

                if (!any)
                {
                    bounds = new Bounds(world, Vector3.zero);
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(world);
                }
            }
        }

        private static Avatar LoadAvatar(string assetPath)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;

            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static string SourceFbxPath(string character)
        {
            return SourceFbxRoot + "/" + character + ".fbx";
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
