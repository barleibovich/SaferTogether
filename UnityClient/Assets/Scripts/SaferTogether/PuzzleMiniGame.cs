using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // "What should you bring to the safe room?" quiz. 4 stages, each with 4 item thumbnails
    // (Resources/MissionGames/Puzzle/stage<N>/*) — pick the correct one. Records per-stage time and
    // wrong attempts, then reports a MissionGameResult.
    public sealed class PuzzleMiniGame : MonoBehaviour
    {
        // correct item per stage (matches the rendered sprite names / folder keys)
        private static readonly string[] CorrectKeys = { "flashlight", "iphone", "radio", "water" };
        private const string Question = "? המוגן במרחב צריך מה";

        private Canvas canvas;
        private RectTransform optionsRoot;
        private Text headerText;
        private Text questionText;
        private Text feedbackText;
        private Action<MissionGameResult> onDone;

        private readonly List<MissionStageResult> results = new List<MissionStageResult>();
        private int stageIndex;
        private float stageStartTime;
        private int wrongThisStage;
        private bool locked;

        public bool IsOpen => canvas != null;

        public void Open(Action<MissionGameResult> done)
        {
            if (IsOpen)
            {
                return;
            }

            onDone = done;
            results.Clear();
            stageIndex = 0;
            BuildUi();
            ShowStage();
        }

        public void Close()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }
        }

        private void BuildUi()
        {
            canvas = MissionGameUi.CreateOverlay(transform, "Puzzle Game");
            RectTransform card = MissionGameUi.Panel3(canvas.transform, "Card", new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), MissionGameUi.Panel);

            headerText = MissionGameUi.Label(card, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.97f), "", 16, TextAnchor.MiddleCenter, MissionGameUi.Accent);
            questionText = MissionGameUi.Label(card, new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.9f), MissionText.Rtl(Question), 22, TextAnchor.MiddleCenter, MissionGameUi.Text);
            feedbackText = MissionGameUi.Label(card, new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.07f), "", 16, TextAnchor.MiddleCenter, MissionGameUi.Text);

            optionsRoot = MissionGameUi.Stretch(card, "Options", new Vector2(0.04f, 0.09f), new Vector2(0.96f, 0.78f));
        }

        private void ShowStage()
        {
            locked = false;
            wrongThisStage = 0;
            stageStartTime = Time.unscaledTime;
            feedbackText.text = "";
            headerText.text = MissionText.Rtl("4 מתוך " + (stageIndex + 1) + " שלב");

            for (int i = optionsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(optionsRoot.GetChild(i).gameObject);
            }

            string correctKey = stageIndex < CorrectKeys.Length ? CorrectKeys[stageIndex] : "";
            List<Sprite> options = LoadStageSprites(stageIndex + 1);
            Shuffle(options);

            // 2x2 grid
            Vector2[] mins = { new Vector2(0.02f, 0.52f), new Vector2(0.52f, 0.52f), new Vector2(0.02f, 0.02f), new Vector2(0.52f, 0.02f) };
            Vector2[] maxs = { new Vector2(0.48f, 0.98f), new Vector2(0.98f, 0.98f), new Vector2(0.48f, 0.48f), new Vector2(0.98f, 0.48f) };

            int count = Mathf.Min(4, options.Count);
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = options[i];
                bool isCorrect = sprite != null && string.Equals(sprite.name, correctKey, StringComparison.OrdinalIgnoreCase);
                CreateOption(mins[i], maxs[i], sprite, isCorrect);
            }
        }

        private void CreateOption(Vector2 anchorMin, Vector2 anchorMax, Sprite sprite, bool isCorrect)
        {
            RectTransform cell = MissionGameUi.Panel3(optionsRoot, "Option", anchorMin, anchorMax, MissionGameUi.Card);
            Image cellImage = cell.GetComponent<Image>();
            Button button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = cellImage;
            button.onClick.AddListener(() => OnPick(cellImage, isCorrect));

            RectTransform thumbRect = MissionGameUi.Stretch(cell, "Thumb", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));

            Image thumb = thumbRect.gameObject.AddComponent<Image>();
            thumb.raycastTarget = false;
            thumb.preserveAspect = true;

            if (sprite != null)
            {
                thumb.sprite = sprite;
            }
            else
            {
                thumb.color = new Color(1f, 1f, 1f, 0.15f);
            }
        }

        private void OnPick(Image cellImage, bool isCorrect)
        {
            if (locked)
            {
                return;
            }

            if (!isCorrect)
            {
                wrongThisStage++;
                cellImage.color = MissionGameUi.Bad;
                feedbackText.text = MissionText.Rtl("לא נכון, נסו שוב");
                feedbackText.color = MissionGameUi.Bad;
                return;
            }

            locked = true;
            cellImage.color = MissionGameUi.Good;
            feedbackText.text = MissionText.Rtl("! הכבוד כל");
            feedbackText.color = MissionGameUi.Good;

            results.Add(new MissionStageResult
            {
                index = stageIndex,
                label = "ערכת חירום - שלב " + (stageIndex + 1),
                timeSeconds = Mathf.Round((Time.unscaledTime - stageStartTime) * 10f) / 10f,
                correct = wrongThisStage == 0,
                wrongAttempts = wrongThisStage,
                rotation = 0f
            });

            MissionResultBridge.NotifyStageProgress();
            StartCoroutine(NextStageAfter(0.6f));
        }

        private IEnumerator NextStageAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            stageIndex++;

            if (stageIndex >= 4)
            {
                Finish();
                yield break;
            }

            ShowStage();
        }

        private void Finish()
        {
            float total = 0f;
            foreach (MissionStageResult stage in results)
            {
                total += stage.timeSeconds;
            }

            var result = new MissionGameResult
            {
                game = "puzzle",
                stages = results.ToArray(),
                totalSeconds = total
            };

            Action<MissionGameResult> callback = onDone;
            Close();
            callback?.Invoke(result);
        }

        private static void Shuffle(List<Sprite> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static List<Sprite> LoadStageSprites(int stageNumber)
        {
            string path = "MissionGames/Puzzle/stage" + stageNumber;
            var options = new List<Sprite>(Resources.LoadAll<Sprite>(path));

            if (options.Count > 0)
            {
                return options;
            }

            Texture2D[] textures = Resources.LoadAll<Texture2D>(path);
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                if (texture == null)
                {
                    continue;
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = texture.name;
                options.Add(sprite);
            }

            return options;
        }
    }
}
