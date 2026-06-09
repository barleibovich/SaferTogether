using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // door lock game where bolts must close in order
    [Preserve]
    public sealed class DoorLockMiniGame : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0.02f, 0.05f, 0.07f, 0.82f);
        private static readonly Color PanelColor = new Color32(15, 27, 34, 255);
        private static readonly Color TitleColor = new Color32(243, 251, 255, 255);
        private static readonly Color HintColor = new Color32(159, 179, 187, 255);
        private static readonly Color DoorColor = new Color32(58, 64, 70, 255);
        private static readonly Color BoltColor = new Color32(154, 160, 166, 255);
        private static readonly Color SlotColor = new Color32(30, 36, 42, 255);
        private static readonly Color LockedColor = new Color32(41, 179, 106, 255);
        private static readonly Color TextDark = new Color32(7, 16, 21, 255);

        private const int TotalBolts = 4;

        private GameObject canvasObject;
        private Action onComplete;
        private int nextIndex;
        private readonly List<DraggableBolt> bolts = new List<DraggableBolt>();

        public bool IsOpen => canvasObject != null;

        // open the lock panel. completeCallback runs once every bolt is locked.
        public void Open(Action completeCallback)
        {
            if (canvasObject != null)
            {
                return;
            }

            onComplete = completeCallback;
            nextIndex = 0;
            bolts.Clear();
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

        // build the lock panel with door, bolts and buttons
        private void Build()
        {
            EnsureEventSystem();

            canvasObject = new GameObject("Door Lock Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the room canvas

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 720);
            scaler.matchWidthOrHeight = 0.5f;

            // full-screen backdrop so you can't tap the room behind the panel
            Image backdrop = canvasObject.AddComponent<Image>();
            backdrop.color = BackdropColor;

            RectTransform root = canvasObject.GetComponent<RectTransform>();

            RectTransform panel = CreatePanel(root, "DoorLockMiniGamePanel", new Vector2(0.07f, 0.18f), new Vector2(0.93f, 0.82f), PanelColor);

            CreateText(panel, "Shelter door lock", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), 24, FontStyle.Bold, TitleColor);
            CreateText(panel, "Slide the bolts in order, from top to bottom", new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.88f), 15, FontStyle.Normal, HintColor);

            // the door background is also the track the bolts slide on
            RectTransform door = CreatePanel(panel, "DoorBackground", new Vector2(0.07f, 0.16f), new Vector2(0.93f, 0.78f), DoorColor);

            float startT = 0.16f;
            float targetT = 0.84f;
            float snapThreshold = 0.13f; // pretty tight, you have to drag almost all the way in
            float[] rows = { 0.84f, 0.61f, 0.39f, 0.16f };
            string[] boltNames = { "TopBolt", "UpperMiddleBolt", "LowerMiddleBolt", "BottomBolt" };
            string[] targetNames = { "TopTarget", "UpperMiddleTarget", "LowerMiddleTarget", "BottomTarget" };

            for (int i = 0; i < TotalBolts; i += 1)
            {
                CreateTargetSlot(door, targetNames[i], targetT, rows[i]);
                CreateBolt(door, boltNames[i], i, startT, targetT, snapThreshold, rows[i]);
            }

            UpdateHighlights();

            CreateButton(panel, "CloseButton", "Close", new Vector2(0.31f, 0.03f), new Vector2(0.69f, 0.13f), Close);
        }

        // little dark slot that shows where a bolt is supposed to lock in
        private void CreateTargetSlot(RectTransform parent, string name, float targetT, float rowY)
        {
            Image slot = CreateImage(parent, name, SlotColor);
            RectTransform rect = slot.rectTransform;
            rect.anchorMin = new Vector2(targetT - 0.105f, rowY - 0.11f);
            rect.anchorMax = new Vector2(targetT + 0.105f, rowY + 0.11f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            slot.raycastTarget = false;
        }

        // spawn a draggable bolt and wire it so it only locks when it's its turn
        private void CreateBolt(RectTransform parent, string name, int index, float startT, float targetT, float snapThreshold, float rowY)
        {
            var boltObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(DraggableBolt));
            boltObject.transform.SetParent(parent, false);

            RectTransform rect = boltObject.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(startT, rowY);
            rect.anchorMax = new Vector2(startT, rowY);
            rect.sizeDelta = new Vector2(70, 26);

            Image image = boltObject.GetComponent<Image>();
            image.color = BoltColor;

            DraggableBolt bolt = boltObject.GetComponent<DraggableBolt>();
            int boltIndex = index; // grab it for the lambda below
            bolt.Configure(parent, image, startT, targetT, snapThreshold, rowY, BoltColor, LockedColor,
                () => boltIndex == nextIndex, OnBoltLocked);
            bolts.Add(bolt);
        }

        // a bolt locked: move to the next one, finish if all done
        private void OnBoltLocked()
        {
            nextIndex += 1;
            UpdateHighlights();

            if (nextIndex >= TotalBolts)
            {
                Close();
                onComplete?.Invoke();
            }
        }

        // light up the next bolt to lock and dim the others, so you know the order
        private void UpdateHighlights()
        {
            for (int i = 0; i < bolts.Count; i += 1)
            {
                bolts[i].SetActiveHighlight(i == nextIndex);
            }
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

        // quick helper to make a colored Image
        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
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
