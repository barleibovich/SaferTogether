using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // shared uGUI builders for the mission mini-games (puzzle, code, missile). each game spins up
    // its own full-screen overlay canvas above the room and tears it down when it closes.
    public static class MissionGameUi
    {
        public static readonly Color Panel = new Color32(8, 17, 30, 245);
        public static readonly Color Card = new Color32(18, 32, 52, 255);
        public static readonly Color Accent = new Color32(18, 154, 228, 255);
        public static readonly Color Good = new Color32(40, 200, 120, 255);
        public static readonly Color Bad = new Color32(225, 70, 78, 255);
        public static readonly Color Orange = new Color32(255, 160, 40, 255);
        public static readonly Color Text = new Color32(238, 244, 250, 255);

        public static Font Font => MissionFonts.UiFont;

        // a screen-space overlay canvas (above the room) with a dimmed, click-blocking backdrop.
        public static Canvas CreateOverlay(Transform parent, string name)
        {
            EnsureEventSystem();

            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 720);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backdrop = Stretch(canvasObject.transform, "Backdrop", Vector2.zero, Vector2.one);
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.55f);
            backdropImage.raycastTarget = true;

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        public static RectTransform Stretch(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
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

        public static RectTransform Panel3(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = Stretch(parent, name, anchorMin, anchorMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        public static Text Label(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string value, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(6, 4);
            rect.offsetMax = new Vector2(-6, -4);

            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(8, size - 8);
            text.resizeTextMaxSize = size;
            return text;
        }

        public static Button ImageButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite, UnityAction onClick, out Image image)
        {
            var go = new GameObject("ImageButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(4, 4);
            rect.offsetMax = new Vector2(-4, -4);

            image = go.GetComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            return button;
        }

        public static Button TextButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string label, Color background, int size, UnityAction onClick, out Text text)
        {
            var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(4, 4);
            rect.offsetMax = new Vector2(-4, -4);

            Image image = go.GetComponent<Image>();
            image.color = background;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            text = Label(go.transform, Vector2.zero, Vector2.one, label, size, TextAnchor.MiddleCenter, Text);
            return button;
        }
    }
}
