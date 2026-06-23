using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // whiteboard game for answering the admin exercises
    [Preserve]
    public sealed class BoardExerciseMiniGame : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0.02f, 0.05f, 0.07f, 0.82f);
        private static readonly Color PanelColor = new Color32(15, 27, 34, 255);
        private static readonly Color TitleColor = new Color32(243, 251, 255, 255);
        private static readonly Color HintColor = new Color32(159, 179, 187, 255);
        private static readonly Color BoardFallback = new Color32(28, 64, 48, 255);
        private static readonly Color ChalkColor = new Color32(244, 246, 240, 255);
        private static readonly Color TextDark = new Color32(7, 16, 21, 255);
        private static readonly Color CorrectColor = new Color32(41, 179, 106, 255);
        private static readonly Color WrongColor = new Color32(225, 64, 78, 255);

        private GameObject canvasObject;
        private string[] questions;
        private string[] answers;
        private Action onComplete;
        private int index;

        private Text exerciseText;
        private Text progressText;
        private Text indicatorText;
        private InputField answerField;

        public bool IsOpen => canvasObject != null;

        // open the board game and save the finish callback
        public void Open(string[] exerciseQuestions, string[] exerciseAnswers, Action completeCallback)
        {
            if (canvasObject != null)
            {
                return;
            }

            questions = exerciseQuestions ?? new string[0];
            answers = exerciseAnswers ?? new string[0];
            onComplete = completeCallback;
            index = 0;

            if (questions.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Build();
            ShowExercise();
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

        // build the board panel: title, board with the exercise, input field + buttons
        private void Build()
        {
            EnsureEventSystem();

            canvasObject = new GameObject("Board Exercise Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            RectTransform panel = CreatePanel(root, "BoardExercisePanel", new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.84f), PanelColor);

            CreateText(panel, "Answer the exercise on the board", new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f), 20, FontStyle.Bold, TitleColor);
            progressText = CreateText(panel, "", new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.90f), 14, FontStyle.Normal, HintColor);

            // big board image with the exercise text
            RectTransform board = CreatePanel(panel, "BigBoard", new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.82f), BoardFallback);
            Image boardImage = board.GetComponent<Image>();
            Sprite boardSprite = LoadBoardSprite();
            if (boardSprite != null)
            {
                boardImage.sprite = boardSprite;
                boardImage.color = Color.white;
                boardImage.preserveAspect = true;
            }

            exerciseText = CreateText(board, "", new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.85f), 26, FontStyle.Bold, ChalkColor);

            indicatorText = CreateText(panel, "", new Vector2(0.06f, 0.33f), new Vector2(0.94f, 0.39f), 16, FontStyle.Bold, HintColor);

            answerField = CreateInputField(panel, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.32f), "Type your answer");

            CreateButton(panel, "SubmitButton", "Submit answer", new Vector2(0.30f, 0.08f), new Vector2(0.70f, 0.18f), SubmitAnswer);
            CreateButton(panel, "CloseButton", "Close", new Vector2(0.74f, 0.08f), new Vector2(0.96f, 0.18f), Close);
        }

        // put the current exercise on the board, reset the input + progress text
        private void ShowExercise()
        {
            if (exerciseText != null)
            {
                exerciseText.text = questions[index];
            }

            if (progressText != null)
            {
                progressText.text = $"Exercise {index + 1} of {questions.Length}";
            }

            if (indicatorText != null)
            {
                indicatorText.text = "";
            }

            if (answerField != null)
            {
                answerField.text = "";
            }

            // a new exercise (the first one, or the next after a correct answer)
            // is a new step: restart the idle nudge timer for it
            MissionResultBridge.NotifyStageProgress();
        }

        // check the typed answer and show right or wrong
        private void SubmitAnswer()
        {
            string given = answerField != null ? answerField.text : "";

            if (string.IsNullOrEmpty(given.Trim()))
            {
                return; // need to type something first
            }

            string expected = index < answers.Length ? answers[index] : "";

            if (!AnswersMatch(given, expected))
            {
                if (indicatorText != null)
                {
                    indicatorText.text = "Wrong - try again";
                    indicatorText.color = WrongColor;
                }

                return;
            }

            if (indicatorText != null)
            {
                indicatorText.text = "Correct!";
                indicatorText.color = CorrectColor;
            }

            // show "Correct!" for a moment, then move on
            CancelInvoke(nameof(Advance));
            Invoke(nameof(Advance), 0.6f);
        }

        // go to the next exercise, or finish if that was the last one
        private void Advance()
        {
            if (canvasObject == null)
            {
                return; // panel got closed
            }

            index += 1;

            if (index >= questions.Length)
            {
                Close();
                onComplete?.Invoke();
                return;
            }

            ShowExercise();
        }

        // if the admin didn't set an answer, any non-empty answer counts as right
        private static bool AnswersMatch(string given, string expected)
        {
            string g = (given ?? "").Trim().Replace(" ", "").ToLowerInvariant();
            string e = (expected ?? "").Trim().Replace(" ", "").ToLowerInvariant();
            return e.Length == 0 ? g.Length > 0 : g == e;
        }

        // load the board image from Resources as a sprite (null if missing)
        private static Sprite LoadBoardSprite()
        {
            Texture2D texture = Resources.Load<Texture2D>("Room/board");
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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
            CreateText(buttonObject.transform, label, Vector2.zero, Vector2.one, 15, FontStyle.Bold, TextDark);
        }

        // make a single-line text box with placeholder text for typing the answer
        private static InputField CreateInputField(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string placeholderText)
        {
            var inputObject = new GameObject("Answer Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = inputObject.GetComponent<Image>();
            background.color = new Color32(255, 255, 255, 235);

            Text textComponent = CreateText(inputObject.transform, "", Vector2.zero, Vector2.one, 18, FontStyle.Normal, TextDark);
            textComponent.alignment = TextAnchor.MiddleCenter;

            Text placeholder = CreateText(inputObject.transform, placeholderText, Vector2.zero, Vector2.one, 16, FontStyle.Italic, new Color32(120, 130, 136, 255));
            placeholder.alignment = TextAnchor.MiddleCenter;

            InputField field = inputObject.GetComponent<InputField>();
            field.textComponent = textComponent;
            field.placeholder = placeholder;
            field.targetGraphic = background;
            field.lineType = InputField.LineType.SingleLine;
            return field;
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
