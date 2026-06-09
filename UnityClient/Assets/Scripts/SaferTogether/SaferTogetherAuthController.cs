using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // runs the avatar editor UI at runtime
    public sealed class SaferTogetherAuthController : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // web function that changes the browser page
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void SaferTogetherNavigate(string url);
#endif

        [SerializeField] private string gatewayBaseUrl = "http://localhost:5173";
        [SerializeField] private bool buildRuntimeUi = true;
        private const float AvatarEditorTabContentHeight = 190f;

        private SaferTogetherApiClient apiClient;
        private Dropdown speciesDropdown;
        private Dropdown topDropdown;
        private Dropdown topColorDropdown;
        private Dropdown bottomDropdown;
        private Dropdown bottomColorDropdown;
        private Dropdown shoesDropdown;
        private Dropdown shoeColorDropdown;
        private Dropdown accessoryDropdown;
        private AvatarView avatarView;
        private AvatarBuilder avatarBuilder;
        private Camera avatarPreviewCamera;
        private RawImage avatarPreviewImage;
        private RenderTexture avatarPreviewTexture;
        private Coroutine avatarPreviewRenderCoroutine;
        private Text statusText;
        private Button saveAvatarButton;
        private Button backButton;
        private Button avatarTabButton;
        private Button shirtsTabButton;
        private Button pantsTabButton;
        private Button accessoriesTabButton;
        private GameObject avatarTabPanel;
        private GameObject shirtsTabPanel;
        private GameObject pantsTabPanel;
        private GameObject accessoriesTabPanel;
        private UserProfile currentProfile;
        private string returnUrl = "signup.html";
        private string activeTabId = "avatar";

        // set up the api client and build the UI on startup
        private void Awake()
        {
            apiClient = new SaferTogetherApiClient(gatewayBaseUrl);

            if (buildRuntimeUi && avatarView == null)
            {
                BuildRuntimeUi();
            }

            WireButtons();
        }

        // clean up the preview render texture + coroutine on teardown
        private void OnDestroy()
        {
            if (avatarPreviewRenderCoroutine != null)
            {
                StopCoroutine(avatarPreviewRenderCoroutine);
                avatarPreviewRenderCoroutine = null;
            }

            if (avatarPreviewTexture == null)
            {
                return;
            }

            avatarPreviewTexture.Release();
            Destroy(avatarPreviewTexture);
        }

        // take the web session json and load it into Unity
        public void ApplyWebSessionJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            WebSessionMessage session = UnityEngine.JsonUtility.FromJson<WebSessionMessage>(json);

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
                SetStatus("Connected as " + session.profile.username);
                return;
            }

            if (!string.IsNullOrEmpty(session.draftAvatar))
            {
                ApplyAvatar(session.draftAvatar);
            }

            SetStatus("Choose an avatar, then save");
        }

        // connect the buttons and dropdowns to their actions
        private void WireButtons()
        {
            saveAvatarButton?.onClick.RemoveAllListeners();
            backButton?.onClick.RemoveAllListeners();

            foreach (Dropdown dropdown in AvatarDropdowns())
            {
                dropdown?.onValueChanged.RemoveAllListeners();
            }

            saveAvatarButton?.onClick.AddListener(OnSaveAvatarClicked);
            backButton?.onClick.AddListener(OnBackClicked);

            foreach (Dropdown dropdown in AvatarDropdowns())
            {
                if (dropdown == null)
                {
                    continue;
                }

                if (dropdown == speciesDropdown)
                {
                    dropdown.onValueChanged.AddListener(_ => OnSpeciesChanged());
                    continue;
                }

                dropdown.onValueChanged.AddListener(_ => PreviewAvatar());
            }
        }

        // return all the avatar dropdowns in one list
        private Dropdown[] AvatarDropdowns()
        {
            return new[]
            {
                speciesDropdown,
                topDropdown,
                topColorDropdown,
                bottomDropdown,
                bottomColorDropdown,
                shoesDropdown,
                shoeColorDropdown,
                accessoryDropdown
            };
        }

        // build the avatar editor when the scene has no UI
        private void BuildRuntimeUi()
        {
            EnsureEventSystem();
            Canvas canvas = CreateCanvas();
            RectTransform panel = CreateScrollablePanel(canvas.transform);

            CreateTitle(panel, "SaferTogether Unity");
            Text subtitle = CreateText(panel, "Avatar Builder", 20, TextAnchor.MiddleCenter);
            subtitle.color = new Color32(190, 205, 220, 255);
            avatarBuilder = CreateAvatarBuilderPreview(panel);
            CreateAvatarEditorControls(panel);

            RectTransform profileRow = CreateRow(panel);
            saveAvatarButton = CreateButton(profileRow, "Save Avatar");
            backButton = CreateButton(profileRow, "Back");

            statusText = CreateText(panel, "Choose an avatar, then save", 16, TextAnchor.MiddleCenter);
            statusText.color = new Color32(190, 205, 220, 255);
            ApplyDefaultSelections();
            UpdateTabAvailability();
            PreviewAvatar();
        }

        // build the tab controls for the avatar editor
        private void CreateAvatarEditorControls(Transform parent)
        {
            CreateTabBar(parent);

            RectTransform content = CreateTabContent(parent);
            avatarTabPanel = CreateTabPanel(content, "Avatar Tab");
            shirtsTabPanel = CreateTabPanel(content, "Shirts Tab");
            pantsTabPanel = CreateTabPanel(content, "Pants Tab");
            accessoriesTabPanel = CreateTabPanel(content, "Accessories Tab");

            CreateAvatarTab(avatarTabPanel.transform);
            CreateShirtsTab(shirtsTabPanel.transform);
            CreatePantsTab(pantsTabPanel.transform);
            CreateAccessoriesTab(accessoriesTabPanel.transform);

            SelectTab("avatar");
        }

        // make the row of tab buttons
        private void CreateTabBar(Transform parent)
        {
            RectTransform row = CreateRow(parent);
            row.name = "Avatar Tabs";
            row.GetComponent<HorizontalLayoutGroup>().spacing = 6;
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;

            avatarTabButton = CreateTabButton(row, "Avatar", () => SelectTab("avatar"));
            shirtsTabButton = CreateTabButton(row, "Shirts", () => SelectTab("shirts"));
            pantsTabButton = CreateTabButton(row, "Pants", () => SelectTab("pants"));
            accessoriesTabButton = CreateTabButton(row, "Accessories", () => SelectTab("accessories"));
        }

        // make the area where tab panels go
        private RectTransform CreateTabContent(Transform parent)
        {
            var contentObject = new GameObject("Tab Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            contentObject.transform.SetParent(parent, false);

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = contentObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = AvatarEditorTabContentHeight;
            layoutElement.flexibleWidth = 1;

            return contentObject.GetComponent<RectTransform>();
        }

        // make one hidden tab panel
        private GameObject CreateTabPanel(Transform parent, string name)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            panelObject.transform.SetParent(parent, false);

            VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = panelObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = AvatarEditorTabContentHeight;
            layoutElement.flexibleWidth = 1;

            return panelObject;
        }

        // make one tab button
        private Button CreateTabButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(label + " Tab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(42, 61, 86, 255);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            Text text = CreateText(buttonObject.transform, label, 17, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            text.color = new Color32(238, 244, 250, 255);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 38;
            layoutElement.flexibleWidth = 1;

            return button;
        }

        // make the avatar tab controls
        private void CreateAvatarTab(Transform parent)
        {
            CreateSectionLabel(parent, "Avatar");
            RectTransform row = CreateOptionRow(parent);
            speciesDropdown = CreateCompactDropdownField(row, "Avatar", new List<string>(CharacterAvatarOptions.Species));
        }

        // make the shirt tab controls
        private void CreateShirtsTab(Transform parent)
        {
            CreateSectionLabel(parent, "Shirts");
            RectTransform row = CreateOptionRow(parent);
            topDropdown = CreateCompactDropdownField(row, "Shirt", new List<string>(CharacterAvatarOptions.Tops));
            topColorDropdown = CreateCompactDropdownField(row, "Color", new List<string>(CharacterAvatarOptions.ClothingColors));
        }

        // make the pants and shoes tab controls
        private void CreatePantsTab(Transform parent)
        {
            CreateSectionLabel(parent, "Pants");
            RectTransform pantsRow = CreateOptionRow(parent);
            bottomDropdown = CreateCompactDropdownField(pantsRow, "Pants", new List<string>(CharacterAvatarOptions.Bottoms));
            bottomColorDropdown = CreateCompactDropdownField(pantsRow, "Color", new List<string>(CharacterAvatarOptions.ClothingColors));

            CreateSectionLabel(parent, "Shoes");
            RectTransform shoesRow = CreateOptionRow(parent);
            shoesDropdown = CreateCompactDropdownField(shoesRow, "Shoes", new List<string>(CharacterAvatarOptions.Shoes));
            shoeColorDropdown = CreateCompactDropdownField(shoesRow, "Color", new List<string>(CharacterAvatarOptions.ClothingColors));
        }

        // make the accessory tab controls
        private void CreateAccessoriesTab(Transform parent)
        {
            CreateSectionLabel(parent, "Accessories");
            RectTransform row = CreateOptionRow(parent);
            accessoryDropdown = CreateCompactDropdownField(row, "Accessory", new List<string>(CharacterAvatarOptions.Accessories));
        }

        // update the editor after the species changes
        private void OnSpeciesChanged()
        {
            UpdateTabAvailability();
            PreviewAvatar();
        }

        // switch which tab is showing
        private void SelectTab(string tabId)
        {
            bool dragon = IsDragonSelected();

            if (dragon && (tabId == "shirts" || tabId == "pants"))
            {
                tabId = "avatar";
            }

            activeTabId = tabId;
            UpdateTabAvailability();
        }

        // lock or unlock tabs depending on the avatar
        private void UpdateTabAvailability()
        {
            bool dragon = IsDragonSelected();

            SetTabVisible(avatarTabButton, avatarTabPanel, true, activeTabId == "avatar");
            SetTabVisible(accessoriesTabButton, accessoriesTabPanel, true, activeTabId == "accessories");
            SetTabVisible(shirtsTabButton, shirtsTabPanel, !dragon, activeTabId == "shirts" && !dragon);
            SetTabVisible(pantsTabButton, pantsTabPanel, !dragon, activeTabId == "pants" && !dragon);

            if (dragon && (activeTabId == "shirts" || activeTabId == "pants"))
            {
                activeTabId = "avatar";
                SetTabVisible(avatarTabButton, avatarTabPanel, true, true);
            }

            SetControlInteractivity();
        }

        // show one tab and set its button color
        private void SetTabVisible(Button button, GameObject panel, bool visible, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = active
                        ? new Color32(18, 154, 228, 255)
                        : new Color32(42, 61, 86, 255);
                }
            }

            if (panel != null)
            {
                panel.SetActive(visible && active);
            }
        }

        // turn clothing controls on or off
        private void SetControlInteractivity()
        {
            bool dragon = IsDragonSelected();
            bool enabled = !dragon;

            if (topDropdown != null) topDropdown.interactable = enabled;
            if (topColorDropdown != null) topColorDropdown.interactable = enabled;
            if (bottomDropdown != null) bottomDropdown.interactable = enabled;
            if (bottomColorDropdown != null) bottomColorDropdown.interactable = enabled;
            if (shoesDropdown != null) shoesDropdown.interactable = enabled;
            if (shoeColorDropdown != null) shoeColorDropdown.interactable = enabled;
        }

        // check if the dragon avatar is selected
        private bool IsDragonSelected()
        {
            return SelectedDropdownValue(speciesDropdown, CharacterAvatarOptions.Male) == CharacterAvatarOptions.Dragon;
        }

        // set the default outfit values
        private void ApplyDefaultSelections()
        {
            SetDropdownValue(speciesDropdown, CharacterAvatarOptions.Male);
            SetDropdownValue(topDropdown, CharacterAvatarOptions.Tee);
            SetDropdownValue(topColorDropdown, CharacterAvatarOptions.Black);
            SetDropdownValue(bottomDropdown, CharacterAvatarOptions.Jeans);
            SetDropdownValue(bottomColorDropdown, CharacterAvatarOptions.Denim);
            SetDropdownValue(shoesDropdown, CharacterAvatarOptions.Sneakers);
            SetDropdownValue(shoeColorDropdown, CharacterAvatarOptions.Black);
            SetDropdownValue(accessoryDropdown, CharacterAvatarOptions.NoAccessory);
        }

        // make a small label for a section
        private void CreateSectionLabel(Transform parent, string label)
        {
            Text text = CreateText(parent, label, 15, TextAnchor.MiddleLeft);
            text.fontStyle = FontStyle.Bold;
            text.color = new Color32(111, 196, 242, 255);
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 20);

            LayoutElement layoutElement = text.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 20;
        }

        // make one row for avatar options
        private RectTransform CreateOptionRow(Transform parent)
        {
            var rowObject = new GameObject("Option Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 66;

            return rowObject.GetComponent<RectTransform>();
        }

        // make a small dropdown with a label
        private Dropdown CreateCompactDropdownField(Transform parent, string label, List<string> options)
        {
            var fieldObject = new GameObject(label + " Field", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            fieldObject.transform.SetParent(parent, false);

            VerticalLayoutGroup layout = fieldObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = fieldObject.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1;
            layoutElement.preferredHeight = 66;

            CreateCompactFieldLabel(fieldObject.transform, label);
            return CreateDropdown(fieldObject.transform, label, options, 15, 15, 38, 8, 24);
        }

        // make the little label above a compact dropdown
        private void CreateCompactFieldLabel(Transform parent, string label)
        {
            Text text = CreateText(parent, label, 13, TextAnchor.MiddleLeft);
            text.color = new Color32(190, 205, 220, 255);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = 13;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 18);

            LayoutElement layoutElement = text.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 18;
        }

        // make an EventSystem if the scene needs one
        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        // make the main UI canvas
        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("SaferTogether Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(430, 760);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            return canvas;
        }

        // make the scroll panel for the editor
        private RectTransform CreateScrollablePanel(Transform parent)
        {
            var scrollObject = new GameObject("Avatar Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);

            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(10, 10);
            scrollRectTransform.offsetMax = new Vector2(-10, -10);

            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color32(7, 16, 21, 24);

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = Color.white;
            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var panelObject = new GameObject("Auth Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelObject.transform.SetParent(viewportObject.transform, false);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(396, 0);

            Image image = panelObject.GetComponent<Image>();
            image.color = new Color32(8, 17, 30, 255);

            VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = rect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 34f;

            return rect;
        }

        // make a horizontal button row
        private RectTransform CreateRow(Transform parent)
        {
            var rowObject = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 50;

            return rowObject.GetComponent<RectTransform>();
        }

        // make the page title text
        private void CreateTitle(Transform parent, string value)
        {
            Text title = CreateText(parent, value, 34, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color32(245, 241, 226, 255);
        }

        // make the simple layered avatar preview
        private AvatarView CreateAvatarView(Transform parent)
        {
            var avatarObject = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(AvatarView));
            avatarObject.transform.SetParent(parent, false);
            const float previewScale = 1.1f;

            RectTransform rect = avatarObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 350);

            LayoutElement layoutElement = avatarObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 300;
            layoutElement.preferredHeight = 350;

            Image backgroundImage = avatarObject.GetComponent<Image>();
            backgroundImage.color = new Color32(184, 199, 217, 255);

            Image artImage = CreateAvatarImage(avatarObject.transform, "Illustrated Avatar", new Vector2(0, -8) * previewScale, new Vector2(286, 326) * previewScale);
            Image wingsImage = CreateAvatarImage(avatarObject.transform, "Wings", new Vector2(0, -32) * previewScale, new Vector2(188, 86) * previewScale);
            Image tailImage = CreateAvatarImage(avatarObject.transform, "Tail", new Vector2(62, -86) * previewScale, new Vector2(64, 28) * previewScale);
            Image leftLegImage = CreateAvatarImage(avatarObject.transform, "Left Leg", new Vector2(-24, -128) * previewScale, new Vector2(30, 76) * previewScale);
            Image rightLegImage = CreateAvatarImage(avatarObject.transform, "Right Leg", new Vector2(24, -128) * previewScale, new Vector2(30, 76) * previewScale);
            Image bottomImage = CreateAvatarImage(avatarObject.transform, "Pants", new Vector2(0, -92) * previewScale, new Vector2(86, 34) * previewScale);
            Image leftShoeImage = CreateAvatarImage(avatarObject.transform, "Left Shoe", new Vector2(-24, -170) * previewScale, new Vector2(38, 18) * previewScale);
            Image rightShoeImage = CreateAvatarImage(avatarObject.transform, "Right Shoe", new Vector2(24, -170) * previewScale, new Vector2(38, 18) * previewScale);
            Image leftArmImage = CreateAvatarImage(avatarObject.transform, "Left Arm", new Vector2(-70, -40) * previewScale, new Vector2(26, 86) * previewScale);
            Image rightArmImage = CreateAvatarImage(avatarObject.transform, "Right Arm", new Vector2(70, -40) * previewScale, new Vector2(26, 86) * previewScale);
            Image leftHandImage = CreateAvatarImage(avatarObject.transform, "Left Hand", new Vector2(-86, -88) * previewScale, new Vector2(26, 26) * previewScale);
            Image rightHandImage = CreateAvatarImage(avatarObject.transform, "Right Hand", new Vector2(86, -88) * previewScale, new Vector2(26, 26) * previewScale);
            Image bodyImage = CreateAvatarImage(avatarObject.transform, "Top", new Vector2(0, -38) * previewScale, new Vector2(106, 92) * previewScale);
            Image topCenterDetailImage = CreateAvatarImage(avatarObject.transform, "Top Center Detail", new Vector2(0, -34) * previewScale, new Vector2(5, 70) * previewScale);
            Image leftTopDetailImage = CreateAvatarImage(avatarObject.transform, "Left Top Detail", new Vector2(-18, -8) * previewScale, new Vector2(28, 44) * previewScale);
            Image rightTopDetailImage = CreateAvatarImage(avatarObject.transform, "Right Top Detail", new Vector2(18, -8) * previewScale, new Vector2(28, 44) * previewScale);
            Image neckImage = CreateAvatarImage(avatarObject.transform, "Neck", new Vector2(0, 18) * previewScale, new Vector2(34, 44) * previewScale);
            Image waistDetailImage = CreateAvatarImage(avatarObject.transform, "Waist Detail", new Vector2(0, -75) * previewScale, new Vector2(90, 10) * previewScale);
            Image leftBottomDetailImage = CreateAvatarImage(avatarObject.transform, "Left Bottom Detail", new Vector2(-26, -101) * previewScale, new Vector2(18, 18) * previewScale);
            Image rightBottomDetailImage = CreateAvatarImage(avatarObject.transform, "Right Bottom Detail", new Vector2(26, -101) * previewScale, new Vector2(18, 18) * previewScale);
            Image leftShoeDetailImage = CreateAvatarImage(avatarObject.transform, "Left Shoe Detail", new Vector2(-24, -169) * previewScale, new Vector2(22, 4) * previewScale);
            Image rightShoeDetailImage = CreateAvatarImage(avatarObject.transform, "Right Shoe Detail", new Vector2(24, -169) * previewScale, new Vector2(22, 4) * previewScale);
            Image leftEarImage = CreateAvatarImage(avatarObject.transform, "Left Ear", new Vector2(-58, 76) * previewScale, new Vector2(34, 42) * previewScale);
            Image rightEarImage = CreateAvatarImage(avatarObject.transform, "Right Ear", new Vector2(58, 76) * previewScale, new Vector2(34, 42) * previewScale);
            Image backHairImage = CreateAvatarImage(avatarObject.transform, "Back Hair", new Vector2(0, 82) * previewScale, new Vector2(112, 116) * previewScale);
            Image headImage = CreateAvatarImage(avatarObject.transform, "Head", new Vector2(0, 78) * previewScale, new Vector2(92, 108) * previewScale);
            Image jawShadowImage = CreateAvatarImage(avatarObject.transform, "Jaw Shadow", new Vector2(0, 42) * previewScale, new Vector2(42, 8) * previewScale);
            Image leftCheekImage = CreateAvatarImage(avatarObject.transform, "Left Cheek", new Vector2(-34, 70) * previewScale, new Vector2(16, 10) * previewScale);
            Image rightCheekImage = CreateAvatarImage(avatarObject.transform, "Right Cheek", new Vector2(34, 70) * previewScale, new Vector2(16, 10) * previewScale);
            Image hairImage = CreateAvatarImage(avatarObject.transform, "Hair", new Vector2(0, 128) * previewScale, new Vector2(104, 42) * previewScale);
            Image leftHairDetailImage = CreateAvatarImage(avatarObject.transform, "Left Hair Detail", new Vector2(-42, 89) * previewScale, new Vector2(15, 42) * previewScale);
            Image rightHairDetailImage = CreateAvatarImage(avatarObject.transform, "Right Hair Detail", new Vector2(42, 89) * previewScale, new Vector2(15, 42) * previewScale);
            Image trunkImage = CreateAvatarImage(avatarObject.transform, "Nose", new Vector2(0, 76) * previewScale, new Vector2(12, 22) * previewScale);

            Image leftEyeWhiteImage = CreateAvatarImage(avatarObject.transform, "Left Eye White", new Vector2(-22, 88) * previewScale, new Vector2(25, 14) * previewScale);
            Image rightEyeWhiteImage = CreateAvatarImage(avatarObject.transform, "Right Eye White", new Vector2(22, 88) * previewScale, new Vector2(25, 14) * previewScale);
            Image leftEyeImage = CreateAvatarImage(avatarObject.transform, "Left Eye", new Vector2(-22, 88) * previewScale, new Vector2(10, 10) * previewScale);
            Image rightEyeImage = CreateAvatarImage(avatarObject.transform, "Right Eye", new Vector2(22, 88) * previewScale, new Vector2(10, 10) * previewScale);
            Image leftBrowImage = CreateAvatarImage(avatarObject.transform, "Left Brow", new Vector2(-22, 103) * previewScale, new Vector2(23, 5) * previewScale);
            Image rightBrowImage = CreateAvatarImage(avatarObject.transform, "Right Brow", new Vector2(22, 103) * previewScale, new Vector2(23, 5) * previewScale);
            Image mouthImage = CreateAvatarImage(avatarObject.transform, "Mouth", new Vector2(0, 58) * previewScale, new Vector2(30, 5) * previewScale);
            Image accessoryImage = CreateAvatarImage(avatarObject.transform, "Accessory", new Vector2(0, 90) * previewScale, new Vector2(86, 10) * previewScale);
            Image accessoryLeftDetailImage = CreateAvatarImage(avatarObject.transform, "Accessory Left Detail", new Vector2(-25, 88) * previewScale, new Vector2(28, 22) * previewScale);
            Image accessoryRightDetailImage = CreateAvatarImage(avatarObject.transform, "Accessory Right Detail", new Vector2(25, 88) * previewScale, new Vector2(28, 22) * previewScale);
            Text badgeLabel = CreateAvatarText(avatarObject.transform, "Badge", "", 16, new Vector2(0, -42) * previewScale, new Vector2(28, 20) * previewScale);

            AvatarView view = avatarObject.GetComponent<AvatarView>();
            view.Bind(
                backgroundImage,
                artImage,
                wingsImage,
                tailImage,
                leftArmImage,
                rightArmImage,
                leftHandImage,
                rightHandImage,
                bodyImage,
                topCenterDetailImage,
                leftTopDetailImage,
                rightTopDetailImage,
                neckImage,
                bottomImage,
                waistDetailImage,
                leftBottomDetailImage,
                rightBottomDetailImage,
                leftLegImage,
                rightLegImage,
                leftShoeImage,
                rightShoeImage,
                leftShoeDetailImage,
                rightShoeDetailImage,
                leftEarImage,
                rightEarImage,
                headImage,
                backHairImage,
                hairImage,
                leftHairDetailImage,
                rightHairDetailImage,
                jawShadowImage,
                leftCheekImage,
                rightCheekImage,
                trunkImage,
                leftEyeWhiteImage,
                rightEyeWhiteImage,
                leftEyeImage,
                rightEyeImage,
                leftBrowImage,
                rightBrowImage,
                mouthImage,
                accessoryImage,
                accessoryLeftDetailImage,
                accessoryRightDetailImage,
                badgeLabel
            );
            return view;
        }

        // make the prefab avatar preview scene
        private AvatarBuilder CreateAvatarBuilderPreview(Transform parent)
        {
            var previewObject = new GameObject("3D Avatar Preview", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement));
            previewObject.transform.SetParent(parent, false);

            RectTransform rect = previewObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 350);

            LayoutElement layoutElement = previewObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 300;
            layoutElement.preferredHeight = 350;

            avatarPreviewTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32)
            {
                name = "Avatar Builder Preview Texture",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            avatarPreviewTexture.Create();

            RawImage image = previewObject.GetComponent<RawImage>();
            image.texture = avatarPreviewTexture;
            image.color = Color.white;
            image.raycastTarget = false;
            avatarPreviewImage = image;

            AvatarBuilder builder = Object.FindAnyObjectByType<AvatarBuilder>();

            if (builder == null)
            {
                var builderObject = new GameObject("AvatarBuilder", typeof(AvatarBuilder));
                builder = builderObject.GetComponent<AvatarBuilder>();
            }

            builder.resourcesRoot = "GeneratedAvatarBuilder";
            builder.instantiateAvatarPrefabs = true;

            Transform root = builder.avatarRoot != null ? builder.avatarRoot : builder.transform.Find("AvatarPreviewRoot");
            if (root == null)
            {
                root = new GameObject("AvatarPreviewRoot").transform;
            }

            root.SetParent(builder.transform, false);
            root.localPosition = new Vector3(0, -1.1f, 0);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * 1.18f;
            builder.avatarRoot = root;
            builder.EnsureResourcesLoaded();

            var cameraObject = new GameObject("AvatarPreviewCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 18, 31, 255);
            camera.orthographic = true;
            camera.orthographicSize = 2.45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.targetTexture = avatarPreviewTexture;
            camera.enabled = true;
            camera.transform.position = new Vector3(0, 0.55f, -6.2f);
            camera.transform.rotation = Quaternion.Euler(3f, 0, 0);
            avatarPreviewCamera = camera;

            var lightObject = new GameObject("AvatarPreviewKeyLight", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color32(255, 243, 220, 255);
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0);

            var fillLightObject = new GameObject("AvatarPreviewFillLight", typeof(Light));
            Light fillLight = fillLightObject.GetComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 7f;
            fillLight.intensity = 0.8f;
            fillLight.color = new Color32(120, 190, 255, 255);
            fillLightObject.transform.position = new Vector3(-2.2f, 1.6f, -2.2f);

            return builder;
        }

        // make one image layer for the preview
        private Image CreateAvatarImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return imageObject.GetComponent<Image>();
        }

        // make one text layer for the preview
        private Text CreateAvatarText(Transform parent, string name, string value, int size, Vector2 position, Vector2 layerSize)
        {
            Text text = CreateText(parent, value, size, TextAnchor.MiddleCenter);
            text.gameObject.name = name;
            text.fontStyle = FontStyle.Bold;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = layerSize;

            return text;
        }

        // make a dropdown control
        private Dropdown CreateDropdown(
            Transform parent,
            string name,
            List<string> options,
            int captionFontSize = 22,
            int optionFontSize = 20,
            float preferredHeight = 48,
            float horizontalPadding = 18,
            float arrowWidth = 36)
        {
            var dropdownObject = new GameObject(name + " Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown), typeof(LayoutElement));
            dropdownObject.transform.SetParent(parent, false);

            Image image = dropdownObject.GetComponent<Image>();
            image.color = new Color32(23, 39, 60, 255);

            Text label = CreateText(dropdownObject.transform, "", captionFontSize, TextAnchor.MiddleLeft);
            label.color = new Color32(245, 248, 252, 255);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = captionFontSize;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(horizontalPadding, 0);
            labelRect.offsetMax = new Vector2(-(arrowWidth + horizontalPadding), 0);

            Text arrow = CreateText(dropdownObject.transform, "v", captionFontSize, TextAnchor.MiddleCenter);
            arrow.color = new Color32(130, 190, 230, 255);
            RectTransform arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(arrowWidth, preferredHeight);
            arrowRect.anchoredPosition = new Vector2(-4, 0);

            Text itemText;
            RectTransform template = CreateDropdownTemplate(dropdownObject.transform, out itemText, preferredHeight, optionFontSize);

            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.captionText = label;
            dropdown.template = template;
            dropdown.itemText = itemText;
            dropdown.options = options.ConvertAll(option => new Dropdown.OptionData(option));
            dropdown.RefreshShownValue();

            LayoutElement layoutElement = dropdownObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;

            return dropdown;
        }

        // make Unity's dropdown template
        private RectTransform CreateDropdownTemplate(Transform parent, out Text itemText, float controlHeight = 48, int optionFontSize = 20)
        {
            var templateObject = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateObject.transform.SetParent(parent, false);
            templateObject.SetActive(false);

            RectTransform templateRect = templateObject.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, -controlHeight);
            templateRect.sizeDelta = new Vector2(0, 210);

            Image templateImage = templateObject.GetComponent<Image>();
            templateImage.color = new Color32(240, 252, 255, 255);

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(templateObject.transform, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text optionText = CreateDropdownItem(contentObject.transform, optionFontSize);
            itemText = optionText;

            ScrollRect scrollRect = templateObject.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;

            return templateRect;
        }

        // make one item inside the dropdown template
        private Text CreateDropdownItem(Transform parent, int optionFontSize = 20)
        {
            var itemObject = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image), typeof(LayoutElement));
            itemObject.transform.SetParent(parent, false);

            Image image = itemObject.GetComponent<Image>();
            image.color = Color.white;

            Toggle toggle = itemObject.GetComponent<Toggle>();
            toggle.targetGraphic = image;

            LayoutElement layoutElement = itemObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = Mathf.Max(34, optionFontSize + 24);

            Text itemText = CreateText(itemObject.transform, "Option", optionFontSize, TextAnchor.MiddleLeft);
            itemText.color = Color.black;
            itemText.resizeTextForBestFit = true;
            itemText.resizeTextMinSize = 10;
            itemText.resizeTextMaxSize = optionFontSize;
            RectTransform textRect = itemText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18, 0);
            textRect.offsetMax = new Vector2(-18, 0);

            return itemText;
        }

        // make a UI button
        private Button CreateButton(Transform parent, string label)
        {
            var buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(8, 181, 101, 255);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText(buttonObject.transform, label, 20, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 50;

            return button;
        }

        // make a UI text label
        private Text CreateText(Transform parent, string value, int size, TextAnchor alignment)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (runtimeFont == null)
            {
                runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.font = runtimeFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color32(7, 16, 21, 255);

            return text;
        }

        // save the avatar or return a signup draft
        private void OnSaveAvatarClicked()
        {
            string avatar = SelectedAvatarId();

            if (currentProfile == null)
            {
                NavigateToReturnUrl(true, avatar);
                return;
            }

            SetBusy(true);
            SetStatus("Saving avatar...");
            StartCoroutine(SaveAvatarWithPreviewImage(avatar));
        }

        // save the avatar id with its preview image
        private IEnumerator SaveAvatarWithPreviewImage(string avatar)
        {
            yield return null;

            string avatarImage = CaptureAvatarPreviewImage();
            yield return apiClient.UpdateAvatar(avatar, avatarImage, profile =>
            {
                ApplyProfile(profile);
                SetStatus("Avatar saved");
                SetBusy(false);
                NavigateToReturnUrl(false, "");
            }, message =>
            {
                SetStatus(message);
                SetBusy(false);
            });
        }

        // leave without saving changes
        private void OnBackClicked()
        {
            NavigateToReturnUrl(false, "");
        }

        // put the loaded profile into the editor
        private void ApplyProfile(UserProfile profile)
        {
            currentProfile = profile;
            ApplyAvatar(profile.avatar);
        }

        // load an avatar id into the dropdowns
        private void ApplyAvatar(string avatar)
        {
            CharacterAvatarSpec spec = CharacterAvatarId.ToSpec(avatar);
            SetDropdownValue(speciesDropdown, spec.species);
            SetDropdownValue(topDropdown, spec.top);
            SetDropdownValue(topColorDropdown, spec.topColor);
            SetDropdownValue(bottomDropdown, spec.bottom);
            SetDropdownValue(bottomColorDropdown, spec.bottomColor);
            SetDropdownValue(shoesDropdown, spec.shoes);
            SetDropdownValue(shoeColorDropdown, spec.shoeColor);
            SetDropdownValue(accessoryDropdown, spec.accessory);
            UpdateTabAvailability();
            PreviewAvatar();
        }

        // refresh the current avatar preview
        private void PreviewAvatar()
        {
            string avatar = SelectedAvatarId();
            CharacterAvatarSpec spec = CharacterAvatarId.ToSpec(avatar);

            if (avatarBuilder != null)
            {
                avatarBuilder.SelectAvatar(spec.species);
                avatarBuilder.SelectAccessory(spec.accessory);
                avatarBuilder.SelectShirt(spec.top);
                avatarBuilder.SelectPants(spec.bottom);
                avatarBuilder.SelectShoes(spec.shoes);
                avatarBuilder.SelectShirtColor(spec.topColor);
                avatarBuilder.SelectPantsColor(spec.bottomColor);
                avatarBuilder.SelectShoeColor(spec.shoeColor);
            }

            avatarView?.SetAvatar(currentProfile != null ? currentProfile.username : "", avatar);
            RequestAvatarPreviewRender();
        }

        // ask for the preview render on the next frame
        private void RequestAvatarPreviewRender()
        {
            if (!isActiveAndEnabled || avatarPreviewCamera == null || avatarPreviewTexture == null)
            {
                return;
            }

            if (avatarPreviewRenderCoroutine != null)
            {
                StopCoroutine(avatarPreviewRenderCoroutine);
            }

            avatarPreviewRenderCoroutine = StartCoroutine(RenderAvatarPreviewNextFrame());
        }

        // render the preview after the UI updates
        private IEnumerator RenderAvatarPreviewNextFrame()
        {
            yield return null;

            if (avatarPreviewCamera == null || avatarPreviewTexture == null)
            {
                avatarPreviewRenderCoroutine = null;
                yield break;
            }

            if (!avatarPreviewTexture.IsCreated())
            {
                avatarPreviewTexture.Create();
            }

            if (avatarPreviewImage != null && avatarPreviewImage.texture != avatarPreviewTexture)
            {
                avatarPreviewImage.texture = avatarPreviewTexture;
            }

            RenderAvatarPreviewNow();
            avatarPreviewRenderCoroutine = null;
        }

        // force the preview camera to render now
        private void RenderAvatarPreviewNow()
        {
            if (avatarPreviewCamera == null || avatarPreviewTexture == null)
            {
                return;
            }

            RenderTexture previousTexture = RenderTexture.active;
            try
            {
                avatarPreviewCamera.targetTexture = avatarPreviewTexture;
                avatarPreviewCamera.Render();
            }
            finally
            {
                RenderTexture.active = previousTexture;
            }
        }

        // turn the preview texture into a png data url
        private string CaptureAvatarPreviewImage()
        {
            if (avatarPreviewCamera == null || avatarPreviewTexture == null)
            {
                return "";
            }

            if (!avatarPreviewTexture.IsCreated())
            {
                avatarPreviewTexture.Create();
            }

            RenderAvatarPreviewNow();

            var texture = new Texture2D(avatarPreviewTexture.width, avatarPreviewTexture.height, TextureFormat.RGBA32, false);
            RenderTexture previousTexture = RenderTexture.active;
            byte[] pngBytes;

            try
            {
                RenderTexture.active = avatarPreviewTexture;
                texture.ReadPixels(new Rect(0, 0, avatarPreviewTexture.width, avatarPreviewTexture.height), 0, 0);
                texture.Apply();
                pngBytes = texture.EncodeToPNG();
            }
            finally
            {
                Destroy(texture);
                RenderTexture.active = previousTexture;
            }

            return pngBytes == null || pngBytes.Length == 0
                ? ""
                : "data:image/png;base64," + System.Convert.ToBase64String(pngBytes);
        }

        // set a dropdown to a saved value
        private void SetDropdownValue(Dropdown dropdown, string value)
        {
            if (dropdown == null)
            {
                return;
            }

            int index = dropdown.options.FindIndex(option => option.text == value);
            dropdown.value = Mathf.Max(index, 0);
            dropdown.RefreshShownValue();
        }

        // build the selected avatar id
        private string SelectedAvatarId()
        {
            string species = SelectedDropdownValue(speciesDropdown, CharacterAvatarOptions.Male);
            string sex = species == CharacterAvatarOptions.Female
                ? CharacterAvatarOptions.Female
                : CharacterAvatarOptions.Male;
            string top = SelectedDropdownValue(topDropdown, CharacterAvatarOptions.Tee);
            string topColor = SelectedDropdownValue(topColorDropdown, CharacterAvatarOptions.Black);
            string bottom = SelectedDropdownValue(bottomDropdown, CharacterAvatarOptions.Jeans);
            string bottomColor = bottom == CharacterAvatarOptions.Jeans
                ? CharacterAvatarOptions.Denim
                : SelectedDropdownValue(bottomColorDropdown, CharacterAvatarOptions.Black);
            string shoes = SelectedDropdownValue(shoesDropdown, CharacterAvatarOptions.Sneakers);
            string shoeColor = SelectedDropdownValue(shoeColorDropdown, CharacterAvatarOptions.Black);
            string accessory = SelectedDropdownValue(accessoryDropdown, CharacterAvatarOptions.NoAccessory);
            return CharacterAvatarId.Build(
                species,
                sex,
                CharacterAvatarOptions.Tan,
                CharacterAvatarOptions.Soft,
                CharacterAvatarOptions.Almond,
                CharacterAvatarOptions.EyeBrown,
                CharacterAvatarOptions.Short,
                CharacterAvatarOptions.HairBrown,
                top,
                topColor,
                bottom,
                bottomColor,
                shoes,
                shoeColor,
                accessory,
                CharacterAvatarOptions.Sky
            );
        }

        // read one dropdown with a fallback
        private string SelectedDropdownValue(Dropdown dropdown, string fallback)
        {
            if (dropdown == null || dropdown.options.Count == 0)
            {
                return fallback;
            }

            int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
            return dropdown.options[index].text;
        }

        // go back to the page that opened Unity
        private void NavigateToReturnUrl(bool includeAvatar, string avatar)
        {
            string target = string.IsNullOrEmpty(returnUrl) ? "signup.html" : returnUrl;

            if (includeAvatar && !string.IsNullOrEmpty(avatar))
            {
                target = AppendQueryParameter(target, "avatar", avatar);
            }

            OpenUrlInCurrentPage(target);
        }

        // add one query value to a url
        private string AppendQueryParameter(string url, string key, string value)
        {
            int hashIndex = url.IndexOf('#');
            string baseUrl = hashIndex >= 0 ? url.Substring(0, hashIndex) : url;
            string hash = hashIndex >= 0 ? url.Substring(hashIndex) : "";
            string separator = baseUrl.Contains("?") ? "&" : "?";
            return baseUrl + separator + key + "=" + System.Uri.EscapeDataString(value) + hash;
        }

        // open a url in the same browser page
        private void OpenUrlInCurrentPage(string target)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaferTogetherNavigate(target);
#else
            Application.OpenURL(target);
#endif
        }

        // show a status message
        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        // enable or disable buttons while saving
        private void SetBusy(bool busy)
        {
            if (saveAvatarButton != null) saveAvatarButton.interactable = !busy;
            if (backButton != null) backButton.interactable = !busy;
        }
    }
}
