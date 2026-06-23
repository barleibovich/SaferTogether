using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // mini-game for closing the shelter window
    [Preserve]
    public sealed class WindowCloseMiniGame : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0.02f, 0.05f, 0.07f, 0.82f);
        private static readonly Color PanelColor = new Color32(15, 27, 34, 255);
        private static readonly Color TitleColor = new Color32(243, 251, 255, 255);
        private static readonly Color HintColor = new Color32(159, 179, 187, 255);
        private static readonly Color FrameColor = new Color32(46, 52, 58, 255);
        private static readonly Color ShutterColor = new Color32(150, 156, 162, 255);
        private static readonly Color ShutterShutColor = new Color32(96, 102, 108, 255);
        private static readonly Color DialColor = new Color32(58, 64, 70, 255);
        private static readonly Color ChalkColor = new Color32(244, 246, 240, 255);
        private static readonly Color TextDark = new Color32(7, 16, 21, 255);

        private const int TotalShutters = 2;

        private static readonly Color WrongColor = new Color32(225, 64, 78, 255);

        private GameObject canvasObject;
        private Action onComplete;
        private int shutCount;
        private RectTransform windowRect;
        private RectTransform panelRect;
        private Text hintText;
        private Text dialNumberText;
        private Text dialIndicator;
        private RotaryDial dial;
        private bool dialShown;
        private int target;
        private int currentNumber;

        public bool IsOpen => canvasObject != null;

        // open the window game and save the finish callback
        public void Open(Action completeCallback)
        {
            if (canvasObject != null)
            {
                return;
            }

            onComplete = completeCallback;
            shutCount = 0;
            dialShown = false;
            Build();
        }

        // close and destroy the panel
        public void Close()
        {
            if (canvasObject != null)
            {
                Destroy(canvasObject);
            }

            canvasObject = null;
        }

        // build the panel: window opening with two shutters + a close button
        private void Build()
        {
            EnsureEventSystem();

            canvasObject = new GameObject("Window Close Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 720);
            scaler.matchWidthOrHeight = 0.5f;

            Image backdrop = canvasObject.AddComponent<Image>();
            backdrop.color = BackdropColor;

            RectTransform root = canvasObject.GetComponent<RectTransform>();

            panelRect = CreatePanel(root, "WindowMiniGamePanel", new Vector2(0.07f, 0.18f), new Vector2(0.93f, 0.82f), PanelColor);
            RectTransform panel = panelRect;

            CreateText(panel, "Close the window", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), 24, FontStyle.Bold, TitleColor);
            hintText = CreateText(panel, "Step 1: slide both steel shutters to the centre", new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.88f), 15, FontStyle.Normal, HintColor);

            windowRect = CreatePanel(panel, "WindowOpening", new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.76f), FrameColor);
            Image windowImage = windowRect.GetComponent<Image>();
            Sprite openWindow = LoadRoomSprite("open_window");
            if (openWindow != null)
            {
                windowImage.sprite = openWindow;
                windowImage.color = Color.white;
                windowImage.preserveAspect = true;
            }
            windowImage.raycastTarget = false;

            CreateShutter(windowRect, "LeftShutter", 0.04f, 0.34f);
            CreateShutter(windowRect, "RightShutter", 0.96f, 0.66f);

            CreateButton(panel, "CloseButton", "Close", new Vector2(0.70f, 0.04f), new Vector2(0.97f, 0.14f), Close);

            // step 1 (the shutters) is the first step: start the idle nudge timer
            MissionResultBridge.NotifyStageProgress();
        }

        // make one draggable steel shutter you slide to the centre
        private void CreateShutter(RectTransform window, string name, float startT, float targetT)
        {
            var shutterObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(DraggableBolt));
            shutterObject.transform.SetParent(window, false);

            RectTransform rect = shutterObject.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(startT, 0.5f);
            rect.anchorMax = new Vector2(startT, 0.5f);
            rect.sizeDelta = new Vector2(130, 240);

            Image image = shutterObject.GetComponent<Image>();
            image.color = ShutterColor;

            DraggableBolt shutter = shutterObject.GetComponent<DraggableBolt>();
            shutter.Configure(window, image, startT, targetT, 0.18f, 0.5f, ShutterColor, ShutterShutColor, null, OnShutterShut);
        }

        // a shutter closed: once both are shut, move on to the dial
        private void OnShutterShut()
        {
            shutCount += 1;

            if (shutCount >= TotalShutters && !dialShown)
            {
                ShowDialPhase();
            }
        }

        // step 2: show the combination dial with a random number you have to turn it to
        private void ShowDialPhase()
        {
            dialShown = true;
            target = UnityEngine.Random.Range(0, 100);

            // step 2 (the dial) is a new step: restart the idle nudge timer
            MissionResultBridge.NotifyStageProgress();

            if (hintText != null)
            {
                hintText.text = "Step 2: turn the lock to " + target.ToString("00");
            }

            RectTransform area = CreatePanel(windowRect, "DialArea", new Vector2(0.27f, 0.16f), new Vector2(0.73f, 0.84f), new Color(0f, 0f, 0f, 0f));
            area.GetComponent<Image>().raycastTarget = false;

            var dialObject = new GameObject("Dial", typeof(RectTransform), typeof(Image), typeof(RotaryDial));
            dialObject.transform.SetParent(area, false);
            RectTransform dialRect = dialObject.GetComponent<RectTransform>();
            dialRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialRect.pivot = new Vector2(0.5f, 0.5f);
            dialRect.anchoredPosition = Vector2.zero;
            dialRect.sizeDelta = new Vector2(160, 160);

            Image dialImage = dialObject.GetComponent<Image>();
            dialImage.sprite = GetDialSprite();
            dialImage.color = Color.white;

            // little marker near the top so you can see the dial spinning
            Image marker = CreateImage(dialRect, "Marker", new Vector2(0.45f, 0.78f), new Vector2(0.55f, 0.96f), ChalkColor);
            marker.raycastTarget = false;

            // the current number, kept upright in the middle of the dial.
            // box must be tall enough for the 34px line, else Truncate drops it.
            dialNumberText = CreateText(area, "00", new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.72f), 34, FontStyle.Bold, ChalkColor);
            dialNumberText.verticalOverflow = VerticalWrapMode.Overflow;

            dial = dialObject.GetComponent<RotaryDial>();
            dial.Configure(dialRect, OnDialNumber);

            // show feedback and the lock button
            dialIndicator = CreateText(panelRect, "", new Vector2(0.05f, 0.155f), new Vector2(0.95f, 0.205f), 14, FontStyle.Bold, HintColor);
            CreateButton(panelRect, "LockButton", "Lock window", new Vector2(0.06f, 0.04f), new Vector2(0.66f, 0.14f), CheckDial);
        }

        // dial moved: remember the number and update the on-screen readout
        private void OnDialNumber(int number)
        {
            currentNumber = number;

            if (dialNumberText != null)
            {
                dialNumberText.text = number.ToString("00");
            }

            if (dialIndicator != null)
            {
                dialIndicator.text = "";
            }
        }

        // only locks the window if the dial is exactly on the target when you press lock
        private void CheckDial()
        {
            if (!dialShown)
            {
                return;
            }

            if (currentNumber != target)
            {
                if (dialIndicator != null)
                {
                    dialIndicator.text = "Not on " + target.ToString("00") + " - keep turning";
                    dialIndicator.color = WrongColor;
                }

                return;
            }

            Close();
            onComplete?.Invoke();
        }

        private static Sprite dialSprite;

        // make the round dial face once (just a filled circle with a rim)
        private static Sprite GetDialSprite()
        {
            if (dialSprite != null)
            {
                return dialSprite;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - radius;
                    float dy = (y + 0.5f) - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius - 6f)
                    {
                        pixels[y * size + x] = DialColor;
                    }
                    else if (distance <= radius - 1f)
                    {
                        pixels[y * size + x] = new Color32(120, 128, 136, 255); // rim
                    }
                    else
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            dialSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return dialSprite;
        }

        // make a colored rectangle panel that fills the given anchors
        private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        // make a colored Image at the given anchors
        private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        // make a centered Arial text label
        private static Text CreateText(Transform parent, string value, Vector2 anchorMin, Vector2 anchorMax, int size, FontStyle style, Color color)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        // make a button with a text label that runs onClick when tapped
        private static void CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Image>().color = new Color32(122, 157, 147, 235);
            buttonObject.GetComponent<Button>().onClick.AddListener(onClick);
            CreateText(buttonObject.transform, label, Vector2.zero, Vector2.one, 16, FontStyle.Bold, TextDark);
        }

        // load a room image from Resources and turn it into a sprite (null if missing)
        private static Sprite LoadRoomSprite(string id)
        {
            Texture2D texture = Resources.Load<Texture2D>("Room/" + id);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // make sure there's an EventSystem so UI clicks work
        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
