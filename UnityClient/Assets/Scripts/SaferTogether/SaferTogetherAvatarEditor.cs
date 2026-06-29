using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // the avatar editor that runs inside the embedded WebGL build. layout (skinned with the PNGs
    // under Resources/AvatarEditorUI):
    //   background  - background.png
    //   left        - the chosen character, front-facing, drag to rotate
    //   right        - field_background panel: name + password (field.png) with a show/hide eye
    //                  (closed_eye.png / opened_eye.png), then Save (save.png) and Back (back.png)
    //   bottom       - character thumbnails (Hebrew names) over preview_background
    // the website pushes the session in with SendMessage("SaferTogether Auth Controller",
    // "ApplyWebSessionJson", json), so the GameObject must keep that name.
    public sealed class SaferTogetherAvatarEditor : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // web function that changes the browser page (Plugins/WebGL/SaferTogetherWebBridge.jslib)
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void SaferTogetherNavigate(string url);
#endif

        private static readonly Color TextColor = new Color32(245, 245, 245, 255);
        private static readonly Color FieldTextColor = new Color32(0, 0, 0, 255);
        private static readonly Color MutedColor = new Color32(92, 92, 98, 255);
        private static readonly Color LabelTextColor = new Color32(0, 0, 0, 255);
        private static readonly Color GridSelected = Color.white;
        private static readonly Color SelectedCellColor = new Color32(40, 122, 226, 230);
        private static readonly Color NormalCellColor = new Color(1f, 1f, 1f, 0.08f);

        [SerializeField] private string gatewayBaseUrl = "http://localhost:5173";

        private SaferTogetherApiClient apiClient;
        private UserProfile currentProfile;
        private string returnUrl = "signup.html";
        private string selectedCharacter = AvatarCatalog.DefaultCharacter;
        private bool showPassword;
        private float yaw;

        private Font runtimeFont;
        private CharacterSpawner spawner;
        private Transform rotator;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private RawImage previewImage;
        private InputField nameField;
        private InputField passwordField;
        private Image eyeImage;
        private Text statusText;
        private Button saveButton;
        private Button backButton;
        private readonly Image[] thumbImages = new Image[AvatarCatalog.Characters.Length];
        private readonly Image[] cellBackgrounds = new Image[AvatarCatalog.Characters.Length];

        // skin sprites
        private Sprite spriteBackground;
        private Sprite spriteFieldBackground;
        private Sprite spriteField;
        private Sprite spriteEyeClosed;
        private Sprite spriteEyeOpen;
        private Sprite spriteSave;
        private Sprite spriteBack;
        private Sprite spritePreviewBackground;
        private Sprite spriteUsernameLabel;
        private Sprite spritePasswordLabel;
        private readonly Sprite[] characterLabelSprites = new Sprite[AvatarCatalog.Characters.Length];

        private void Awake()
        {
            runtimeFont = MissionFonts.UiFont;
            apiClient = new SaferTogetherApiClient(gatewayBaseUrl);
            LoadSkin();
            BuildUi();
            SelectCharacter(selectedCharacter, false);
        }

        private void OnDestroy()
        {
            if (previewTexture != null)
            {
                previewTexture.Release();
                Destroy(previewTexture);
                previewTexture = null;
            }
        }

        private void LoadSkin()
        {
            spriteBackground = LoadUiSprite("background");
            spriteFieldBackground = LoadUiSprite("field_background");
            spriteField = LoadUiSprite("field");
            spriteEyeClosed = LoadUiSprite("closed_eye");
            spriteEyeOpen = LoadUiSprite("opened_eye");
            spriteSave = LoadUiSprite("save");
            spriteBack = LoadUiSprite("back");
            spritePreviewBackground = LoadUiSprite("preview_background") ?? spriteFieldBackground;
            spriteUsernameLabel = LoadUiSprite("label_username");
            spritePasswordLabel = LoadUiSprite("label_password");

            for (int i = 0; i < AvatarCatalog.Characters.Length; i++)
            {
                characterLabelSprites[i] = LoadUiSprite("label_" + AvatarCatalog.Characters[i]);
            }
        }

        private static Sprite LoadUiSprite(string name)
        {
            string path = "AvatarEditorUI/" + name;
            Sprite sprite = Resources.Load<Sprite>(path);

            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(path);

            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---- web bridge -------------------------------------------------------------------

        // the website calls this once the build has loaded
        public void ApplyWebSessionJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            WebSessionMessage session = JsonUtility.FromJson<WebSessionMessage>(json);

            if (session == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(session.gatewayBaseUrl))
            {
                gatewayBaseUrl = session.gatewayBaseUrl;
                apiClient = new SaferTogetherApiClient(gatewayBaseUrl);
            }

            if (!string.IsNullOrEmpty(session.returnUrl))
            {
                returnUrl = session.returnUrl;
            }

            if (session.profile != null)
            {
                ApplyProfile(session.profile);
                SetStatus("");
                return;
            }

            if (!string.IsNullOrEmpty(session.draftAvatar))
            {
                SelectCharacter(session.draftAvatar, false);
            }

            ApplyLoggedOutState();
        }

        private void ApplyProfile(UserProfile profile)
        {
            currentProfile = profile;

            if (nameField != null)
            {
                nameField.interactable = true;
                nameField.text = profile.username ?? "";
            }

            if (passwordField != null)
            {
                passwordField.interactable = true;
                passwordField.text = "";
            }

            SelectCharacter(profile.avatar, false);
        }

        private void ApplyLoggedOutState()
        {
            currentProfile = null;

            if (nameField != null)
            {
                nameField.text = "";
                nameField.interactable = false;
            }

            if (passwordField != null)
            {
                passwordField.text = "";
                passwordField.interactable = false;
            }
        }

        // ---- selection + preview ----------------------------------------------------------

        private void SelectCharacter(string avatarOrCharacterId, bool render)
        {
            selectedCharacter = AvatarCatalog.ResolveCharacter(avatarOrCharacterId);
            yaw = 0f;

            if (spawner != null)
            {
                spawner.Show(selectedCharacter);
                ApplyRotation();
            }

            for (int i = 0; i < AvatarCatalog.Characters.Length; i++)
            {
                bool isSelected = AvatarCatalog.Characters[i] == selectedCharacter;

                // keep every thumbnail at full brightness; show selection via a blue cell instead
                if (thumbImages[i] != null)
                {
                    thumbImages[i].color = GridSelected;
                }

                if (cellBackgrounds[i] != null)
                {
                    cellBackgrounds[i].color = isSelected ? SelectedCellColor : NormalCellColor;
                }
            }

            if (render && previewCamera != null)
            {
                previewCamera.Render();
            }
        }

        // these characters face +Z, so rotate 180 to face the -Z preview camera (plus drag yaw).
        private void ApplyRotation()
        {
            if (rotator != null)
            {
                rotator.localRotation = Quaternion.Euler(0f, 180f + yaw, 0f);
            }
        }

        // drag the preview to spin the character (mouse + touch)
        private void OnPreviewDrag(PointerEventData eventData)
        {
            yaw -= eventData.delta.x * 0.6f;
            ApplyRotation();
        }

        // ---- save / back ------------------------------------------------------------------

        private void OnSaveClicked()
        {
            string avatar = AvatarCatalog.ToAvatarId(selectedCharacter);

            if (currentProfile == null)
            {
                NavigateToReturnUrl(true, avatar);
                return;
            }

            SetBusy(true);
            SetStatus(Rtl("שומר..."));
            StartCoroutine(SaveRoutine(avatar));
        }

        private IEnumerator SaveRoutine(string avatar)
        {
            yield return null;
            string avatarImage = CapturePreviewImage();

            bool avatarOk = false;
            yield return apiClient.UpdateAvatar(avatar, avatarImage, profile =>
            {
                currentProfile = profile;
                avatarOk = true;
            }, message =>
            {
                SetStatus(message);
                SetBusy(false);
            });

            if (!avatarOk)
            {
                yield break;
            }

            string newName = nameField != null ? (nameField.text ?? "").Trim() : "";
            string newPassword = passwordField != null ? (passwordField.text ?? "") : "";
            bool wantsName = !string.IsNullOrEmpty(newName) && newName != currentProfile.username;
            bool wantsPassword = !string.IsNullOrEmpty(newPassword);

            if (!wantsName && !wantsPassword)
            {
                NavigateToReturnUrl(false, "");
                yield break;
            }

            yield return apiClient.UpdateCredentials(wantsName ? newName : currentProfile.username, "", newPassword, profile =>
            {
                currentProfile = profile;
                NavigateToReturnUrl(false, "");
            }, message =>
            {
                SetStatus(message);
                SetBusy(false);
            });
        }

        private void OnBackClicked()
        {
            NavigateToReturnUrl(false, "");
        }

        // render the front-facing preview to a PNG data url (ignores any drag rotation)
        private string CapturePreviewImage()
        {
            if (previewCamera == null || previewTexture == null)
            {
                return "";
            }

            if (!previewTexture.IsCreated())
            {
                previewTexture.Create();
            }

            if (rotator != null)
            {
                rotator.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            previewCamera.Render();

            var texture = new Texture2D(previewTexture.width, previewTexture.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            byte[] png;

            try
            {
                RenderTexture.active = previewTexture;
                texture.ReadPixels(new Rect(0, 0, previewTexture.width, previewTexture.height), 0, 0);
                texture.Apply();
                png = texture.EncodeToPNG();
            }
            finally
            {
                Destroy(texture);
                RenderTexture.active = previous;
                ApplyRotation();
            }

            return png == null || png.Length == 0
                ? ""
                : "data:image/png;base64," + System.Convert.ToBase64String(png);
        }

        // ---- ui construction --------------------------------------------------------------

        private void BuildUi()
        {
            EnsureEventSystem();
            EnsureScreenCamera();
            Canvas canvas = CreateCanvas();

            RectTransform root = CreateStretch(canvas.transform, "Root");
            Image rootImage = root.gameObject.AddComponent<Image>();
            rootImage.raycastTarget = false;

            if (spriteBackground != null)
            {
                rootImage.sprite = spriteBackground;
                rootImage.color = Color.white;
                rootImage.type = Image.Type.Simple;
            }
            else
            {
                rootImage.color = new Color32(8, 17, 30, 255);
            }

            BuildPreviewArea(root);
            BuildFormArea(root);
            BuildThumbnailGrid(root);
        }

        // left: live render of the selected character (front-facing, drag to rotate)
        private void BuildPreviewArea(RectTransform parent)
        {
            RectTransform panel = CreateAnchored(parent, "Preview", new Vector2(0.04f, 0.45f), new Vector2(0.39f, 0.83f));

            previewTexture = new RenderTexture(512, 700, 24, RenderTextureFormat.ARGB32)
            {
                name = "Avatar Editor Preview",
                useMipMap = false,
                autoGenerateMips = false
            };
            previewTexture.Create();

            previewImage = panel.gameObject.AddComponent<RawImage>();
            previewImage.texture = previewTexture;
            previewImage.color = Color.white;
            previewImage.raycastTarget = true;

            // drag to rotate
            EventTrigger trigger = panel.gameObject.AddComponent<EventTrigger>();
            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener(data => OnPreviewDrag((PointerEventData)data));
            trigger.triggers.Add(drag);

            // world rig: stage > rotator > mount > character
            var stage = new GameObject("Avatar Preview Stage");
            stage.transform.SetParent(transform, false);

            rotator = new GameObject("Rotator").transform;
            rotator.SetParent(stage.transform, false);

            var mount = new GameObject("Mount").transform;
            mount.SetParent(rotator, false);

            spawner = stage.AddComponent<CharacterSpawner>();
            spawner.mountPoint = mount;

            var cameraObject = new GameObject("Avatar Preview Camera", typeof(Camera));
            cameraObject.transform.SetParent(stage.transform, false);
            previewCamera = cameraObject.GetComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            // transparent so only the avatar shows (composited over the editor background)
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 1.95f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 40f;
            previewCamera.targetTexture = previewTexture;
            // FitModel centres each character ~3.2 tall at y=1.08, so a fixed front framing works.
            previewCamera.transform.position = new Vector3(0f, 1.05f, -6f);
            previewCamera.transform.rotation = Quaternion.identity;

            var lightObject = new GameObject("Avatar Preview Light", typeof(Light));
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }

        // right: field_background panel with name + password (eye toggle) + Save + Back
        private void BuildFormArea(RectTransform parent)
        {
            RectTransform panel = CreateAnchored(parent, "Form", new Vector2(0.39f, 0.45f), new Vector2(0.99f, 0.83f));
            Image panelImage = panel.gameObject.AddComponent<Image>();

            if (spriteFieldBackground != null)
            {
                panelImage.sprite = spriteFieldBackground;
                panelImage.color = Color.white;
            }
            else
            {
                panelImage.color = new Color32(18, 32, 52, 220);
            }

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 14, 14);
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            // RTL rows: the Hebrew label sits on the right, the field to its left.
            // The password eye is anchored inside the left edge of the field background.
            RectTransform nameRow = CreateRow(panel, 58, 8);
            nameField = CreateSpriteInputField(nameRow, "", false, 58);
            CreateRowLabel(nameRow, spriteUsernameLabel, Rtl("שם משתמש"), 76);

            RectTransform passwordRow = CreateRow(panel, 58, 8);
            passwordField = CreateSpriteInputField(passwordRow, "", true, 58, 54f, 14f);
            CreateEyeButton(passwordField.transform);
            CreateRowLabel(passwordRow, null, Rtl("סיסמא חדשה"), 100);

            saveButton = CreateImageButton(panel, spriteSave, "שמירה", 52, OnSaveClicked);
            backButton = CreateImageButton(panel, spriteBack, "חזרה", 48, OnBackClicked);

            statusText = CreateText(panel, "", 13, TextAnchor.MiddleCenter, new Color32(210, 60, 60, 255));
            LayoutElement statusLayout = statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredHeight = 18;
            statusLayout.flexibleWidth = 1;
        }

        private void CreateEyeButton(Transform parent)
        {
            var go = new GameObject("Eye", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(5f, 7f);
            rect.offsetMax = new Vector2(47f, -7f);

            // transparent button (no white box) — show only the eye icon, still tappable
            Image background = go.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(OnTogglePassword);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(go.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            eyeImage = iconObject.GetComponent<Image>();
            eyeImage.raycastTarget = false;
            eyeImage.preserveAspect = true;
            eyeImage.sprite = spriteEyeClosed;

            if (spriteEyeClosed == null)
            {
                eyeImage.color = new Color32(40, 60, 90, 255);
            }

        }

        private void OnTogglePassword()
        {
            showPassword = !showPassword;

            if (passwordField != null)
            {
                passwordField.contentType = showPassword
                    ? InputField.ContentType.Standard
                    : InputField.ContentType.Password;
                passwordField.ForceLabelUpdate();
            }

            if (eyeImage != null)
            {
                Sprite next = showPassword ? spriteEyeOpen : spriteEyeClosed;
                if (next != null)
                {
                    eyeImage.sprite = next;
                }
            }
        }

        // bottom: scrollable grid of character thumbnails over preview_background
        private void BuildThumbnailGrid(RectTransform parent)
        {
            RectTransform panel = CreateAnchored(parent, "Characters", new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.42f));
            Image panelImage = panel.gameObject.AddComponent<Image>();

            if (spritePreviewBackground != null)
            {
                panelImage.sprite = spritePreviewBackground;
                panelImage.color = Color.white;
            }
            else
            {
                panelImage.color = new Color32(7, 16, 21, 140);
            }

            var viewport = CreateStretch(panel, "Viewport");
            viewport.offsetMin = new Vector2(8, 8);
            viewport.offsetMax = new Vector2(-8, -8);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(60, 100);
            grid.spacing = new Vector2(4, 8);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 28f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            for (int i = 0; i < AvatarCatalog.Characters.Length; i++)
            {
                CreateThumbCell(contentRect, i);
            }
        }

        private void CreateThumbCell(RectTransform parent, int index)
        {
            string character = AvatarCatalog.Characters[index];

            var cellObject = new GameObject(character + " Cell", typeof(RectTransform), typeof(Image), typeof(Button));
            cellObject.transform.SetParent(parent, false);
            Image cellBackground = cellObject.GetComponent<Image>();
            cellBackground.color = NormalCellColor;
            cellBackgrounds[index] = cellBackground;

            Button button = cellObject.GetComponent<Button>();
            button.targetGraphic = cellBackground;
            button.onClick.AddListener(() => SelectCharacter(character, true));

            var thumbObject = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumbObject.transform.SetParent(cellObject.transform, false);
            RectTransform thumbRect = thumbObject.GetComponent<RectTransform>();
            thumbRect.anchorMin = new Vector2(0.04f, 0.24f);
            thumbRect.anchorMax = new Vector2(0.96f, 0.98f);
            thumbRect.offsetMin = Vector2.zero;
            thumbRect.offsetMax = Vector2.zero;

            Image thumbImage = thumbObject.GetComponent<Image>();
            thumbImage.raycastTarget = false;
            thumbImage.preserveAspect = true;
            Sprite sprite = AvatarCatalog.LoadThumbnail(character);

            if (sprite != null)
            {
                thumbImage.sprite = sprite;
            }
            else
            {
                thumbImage.color = new Color32(90, 120, 150, 255);
            }

            thumbImages[index] = thumbImage;

            Sprite labelSprite = characterLabelSprites[index];

            if (labelSprite != null)
            {
                Image labelImage = CreateImage(cellObject.transform, "Label", new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.245f), Color.white);
                labelImage.sprite = labelSprite;
                labelImage.preserveAspect = false;
                labelImage.raycastTarget = false;
            }
            else
            {
                Text label = CreateText(cellObject.transform, AvatarCatalog.DisplayNameFor(character), 12, TextAnchor.MiddleCenter, LabelTextColor);
                label.fontStyle = FontStyle.Bold;
                label.raycastTarget = false;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0.02f, 0.02f);
                labelRect.anchorMax = new Vector2(0.98f, 0.22f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
        }

        // ---- small ui helpers -------------------------------------------------------------

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        // the preview camera renders to a texture, so add a screen camera that just clears the
        // display (otherwise the Game view shows "No cameras rendering"). renders nothing itself.
        private void EnsureScreenCamera()
        {
            var cameraObject = new GameObject("Screen Camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            Camera screenCamera = cameraObject.GetComponent<Camera>();
            screenCamera.clearFlags = CameraClearFlags.SolidColor;
            screenCamera.backgroundColor = new Color32(8, 17, 30, 255);
            screenCamera.cullingMask = 0;
            screenCamera.orthographic = true;
            screenCamera.depth = -10;
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Avatar Editor Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(430, 760);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private RectTransform CreateStretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private RectTransform CreateAnchored(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private RectTransform CreateRow(Transform parent, float height, float spacing)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleWidth = 1;
            return go.GetComponent<RectTransform>();
        }

        // dark Hebrew label that sits on the right of an RTL field row (fixed width)
        private void CreateRowLabel(Transform parent, Sprite labelSprite, string fallbackLabel, float width)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();

            if (labelSprite != null)
            {
                Image image = go.AddComponent<Image>();
                image.sprite = labelSprite;
                image.color = Color.white;
                image.preserveAspect = false;
                image.raycastTarget = false;
            }
            else
            {
                Text text = CreateText(go.transform, fallbackLabel, 16, TextAnchor.MiddleRight, LabelTextColor);
                text.fontStyle = FontStyle.Bold;
                FillWithPadding(text.rectTransform, 0);
            }

            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = 58;
            element.flexibleWidth = 0;
        }

        private InputField CreateSpriteInputField(Transform parent, string placeholder, bool password, float height, float leftPadding = 14f, float rightPadding = 14f)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image background = go.GetComponent<Image>();

            if (spriteField != null)
            {
                background.sprite = spriteField;
                background.color = Color.white;
                background.type = Image.Type.Simple;
            }
            else
            {
                background.color = new Color32(235, 238, 242, 255);
            }

            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleWidth = 1;

            Text placeholderText = CreateText(go.transform, placeholder, 16, TextAnchor.MiddleRight, MutedColor);
            FillWithPadding(placeholderText.rectTransform, leftPadding, rightPadding);

            Text inputText = CreateText(go.transform, "", 16, TextAnchor.MiddleRight, FieldTextColor);
            inputText.supportRichText = false;
            FillWithPadding(inputText.rectTransform, leftPadding, rightPadding);

            InputField input = go.GetComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = inputText;
            input.placeholder = placeholderText;
            input.lineType = InputField.LineType.SingleLine;

            if (password)
            {
                input.contentType = InputField.ContentType.Password;
            }

            return input;
        }

        private void FillWithPadding(RectTransform rect, float leftPadding, float rightPadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(leftPadding, 0);
            rect.offsetMax = new Vector2(-rightPadding, 0);
        }

        private void FillWithPadding(RectTransform rect, float padding)
        {
            FillWithPadding(rect, padding, padding);
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        // image button (save/back). falls back to a coloured button with a Hebrew label when the
        // sprite is missing.
        private Button CreateImageButton(Transform parent, Sprite sprite, string fallbackLabel, float height, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
            }
            else
            {
                image.color = new Color32(8, 181, 101, 255);
                Text text = CreateText(go.transform, Rtl(fallbackLabel), 18, TextAnchor.MiddleCenter, TextColor);
                text.fontStyle = FontStyle.Bold;
                FillWithPadding(text.rectTransform, 2);
            }

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleWidth = 1;
            return button;
        }

        private Text CreateText(Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = runtimeFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        // legacy UI Text renders left-to-right, so reverse Hebrew strings to read correctly.
        private static string Rtl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            char[] chars = value.ToCharArray();
            System.Array.Reverse(chars);
            return new string(chars);
        }

        // ---- status + navigation ----------------------------------------------------------

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void SetBusy(bool busy)
        {
            if (saveButton != null) saveButton.interactable = !busy;
            if (backButton != null) backButton.interactable = !busy;
        }

        private void NavigateToReturnUrl(bool includeAvatar, string avatar)
        {
            string target = string.IsNullOrEmpty(returnUrl) ? "signup.html" : returnUrl;

            if (includeAvatar && !string.IsNullOrEmpty(avatar))
            {
                target = AppendQueryParameter(target, "avatar", avatar);
            }

            OpenUrlInCurrentPage(target);
        }

        private string AppendQueryParameter(string url, string key, string value)
        {
            int hashIndex = url.IndexOf('#');
            string baseUrl = hashIndex >= 0 ? url.Substring(0, hashIndex) : url;
            string hash = hashIndex >= 0 ? url.Substring(hashIndex) : "";
            string separator = baseUrl.Contains("?") ? "&" : "?";
            return baseUrl + separator + key + "=" + System.Uri.EscapeDataString(value) + hash;
        }

        private void OpenUrlInCurrentPage(string target)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaferTogetherNavigate(target);
#else
            Application.OpenURL(target);
#endif
        }
    }
}
