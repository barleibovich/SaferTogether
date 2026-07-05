using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // Door code sequence memory game. The keypad flashes a random sequence (orange); the player
    // repeats it (green = right key, red = wrong). Stages: 3 -> 4 -> 5 -> 5 digits (stage 4 flashes
    // faster). Records per-stage time + wrong attempts and a weighted score (early mistakes cost
    // more), then reports a MissionGameResult.
    public sealed class DoorCodeMiniGame : MonoBehaviour
    {
        private static readonly int[] StageLengths = { 3, 4, 5, 5 };
        private static readonly float[] FlashSeconds = { 0.55f, 0.5f, 0.5f, 0.32f };
        private static readonly float[] StageWeights = { 4f, 3f, 2f, 1f };

        private Canvas canvas;
        private Text headerText;
        private Text statusText;
        private readonly Dictionary<int, Image> keyImages = new Dictionary<int, Image>();
        private readonly Dictionary<int, Button> keyButtons = new Dictionary<int, Button>();
        private Action<MissionGameResult> onDone;

        private readonly List<MissionStageResult> results = new List<MissionStageResult>();
        private readonly List<int> sequence = new List<int>();
        private int stageIndex;
        private int expectedPos;
        private int wrongThisStage;
        private float repeatStartTime;
        private bool acceptingInput;

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
            StartCoroutine(RunStage());
        }

        public void Close()
        {
            StopAllCoroutines();

            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }
        }

        private void BuildUi()
        {
            canvas = MissionGameUi.CreateOverlay(transform, "Door Code Game");
            RectTransform card = MissionGameUi.Panel3(canvas.transform, "Card", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), MissionGameUi.Panel);

            headerText = MissionGameUi.Label(card, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.97f), "", 16, TextAnchor.MiddleCenter, MissionGameUi.Accent);
            MissionGameUi.Label(card, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.9f), MissionText.Rtl("בכתום הספורת אחרי חזרו"), 20, TextAnchor.MiddleCenter, MissionGameUi.Text);
            statusText = MissionGameUi.Label(card, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.82f), "", 16, TextAnchor.MiddleCenter, MissionGameUi.Text);

            RectTransform pad = MissionGameUi.Stretch(card, "Keypad", new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.72f));
            Image keypadBackdrop = pad.gameObject.AddComponent<Image>();
            keypadBackdrop.color = MissionGameUi.Card;
            keypadBackdrop.sprite = Resources.Load<Sprite>("MissionGames/keypad");
            keypadBackdrop.preserveAspect = true;
            keypadBackdrop.raycastTarget = false;

            // digits 1-9 in a 3x3 grid, 0 centred underneath
            int[,] grid = { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float x0 = 0.02f + col * 0.33f;
                    float y0 = 0.76f - row * 0.25f;
                    CreateKey(pad, grid[row, col], new Vector2(x0, y0), new Vector2(x0 + 0.31f, y0 + 0.22f));
                }
            }

            CreateKey(pad, 0, new Vector2(0.35f, 0.01f), new Vector2(0.66f, 0.23f));
        }

        private void CreateKey(Transform parent, int digit, Vector2 anchorMin, Vector2 anchorMax)
        {
            Button button = MissionGameUi.TextButton(parent, anchorMin, anchorMax, digit.ToString(), MissionGameUi.Card, 26, () => OnKey(digit), out Text _);
            keyButtons[digit] = button;
            keyImages[digit] = button.GetComponent<Image>();
        }

        private IEnumerator RunStage()
        {
            acceptingInput = false;
            wrongThisStage = 0;
            expectedPos = 0;
            headerText.text = MissionText.Rtl("4 מתוך " + (stageIndex + 1) + " שלב");
            ResetKeyColors();

            // build the random sequence
            sequence.Clear();
            int length = StageLengths[stageIndex];
            for (int i = 0; i < length; i++)
            {
                sequence.Add(UnityEngine.Random.Range(0, 10));
            }

            yield return FlashSequence();

            statusText.text = MissionText.Rtl("תורכם עכשיו");
            statusText.color = MissionGameUi.Text;
            repeatStartTime = Time.unscaledTime;
            acceptingInput = true;
        }

        private IEnumerator FlashSequence()
        {
            statusText.text = MissionText.Rtl("לרצף לב שימו");
            statusText.color = MissionGameUi.Orange;
            float flash = FlashSeconds[stageIndex];

            yield return new WaitForSecondsRealtime(0.6f);

            foreach (int digit in sequence)
            {
                if (keyImages.TryGetValue(digit, out Image image))
                {
                    image.color = MissionGameUi.Orange;
                }

                yield return new WaitForSecondsRealtime(flash);

                if (keyImages.TryGetValue(digit, out image))
                {
                    image.color = MissionGameUi.Card;
                }

                yield return new WaitForSecondsRealtime(flash * 0.45f);
            }
        }

        private void OnKey(int digit)
        {
            if (!acceptingInput)
            {
                return;
            }

            if (digit == sequence[expectedPos])
            {
                FlashKey(digit, MissionGameUi.Good);
                expectedPos++;

                if (expectedPos >= sequence.Count)
                {
                    CompleteStage();
                }

                return;
            }

            // wrong key: flash red, count it, replay the sequence to try again
            wrongThisStage++;
            FlashKey(digit, MissionGameUi.Bad);
            statusText.text = MissionText.Rtl("שוב לב שימו ,טעות");
            statusText.color = MissionGameUi.Bad;
            acceptingInput = false;
            StartCoroutine(ReplayAfterMistake());
        }

        private IEnumerator ReplayAfterMistake()
        {
            yield return new WaitForSecondsRealtime(0.7f);
            expectedPos = 0;
            ResetKeyColors();
            yield return FlashSequence();
            statusText.text = MissionText.Rtl("תורכם עכשיו");
            statusText.color = MissionGameUi.Text;
            acceptingInput = true;
        }

        private void CompleteStage()
        {
            acceptingInput = false;
            statusText.text = MissionText.Rtl("הכבוד כל");
            statusText.color = MissionGameUi.Good;

            results.Add(new MissionStageResult
            {
                index = stageIndex,
                label = "קוד הדלת - שלב " + (stageIndex + 1),
                timeSeconds = Mathf.Round((Time.unscaledTime - repeatStartTime) * 10f) / 10f,
                correct = wrongThisStage == 0,
                wrongAttempts = wrongThisStage,
                rotation = MissionTilt.Take()
            });

            MissionResultBridge.NotifyStageProgress();
            stageIndex++;

            if (stageIndex >= StageLengths.Length)
            {
                StartCoroutine(FinishAfter(0.6f));
            }
            else
            {
                StartCoroutine(NextStageAfter(0.8f));
            }
        }

        private IEnumerator NextStageAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            yield return RunStage();
        }

        private IEnumerator FinishAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Finish();
        }

        private void Finish()
        {
            float total = 0f;
            float weighted = 0f;
            float weightTotal = 0f;

            foreach (MissionStageResult stage in results)
            {
                total += stage.timeSeconds;
                float weight = stage.index < StageWeights.Length ? StageWeights[stage.index] : 1f;
                weightTotal += weight;
                if (stage.correct)
                {
                    weighted += weight;
                }
            }

            var result = new MissionGameResult
            {
                game = "code",
                stages = results.ToArray(),
                weightedScore = weightTotal > 0f ? Mathf.Round(weighted / weightTotal * 100f) : 0f,
                totalSeconds = total
            };

            Action<MissionGameResult> callback = onDone;
            Close();
            callback?.Invoke(result);
        }

        private void FlashKey(int digit, Color color)
        {
            if (keyImages.TryGetValue(digit, out Image image))
            {
                image.color = color;
                StartCoroutine(ResetKeyAfter(digit, 0.35f));
            }
        }

        private IEnumerator ResetKeyAfter(int digit, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);

            if (keyImages.TryGetValue(digit, out Image image) && image != null)
            {
                image.color = MissionGameUi.Card;
            }
        }

        private void ResetKeyColors()
        {
            foreach (Image image in keyImages.Values)
            {
                if (image != null)
                {
                    image.color = MissionGameUi.Card;
                }
            }
        }
    }
}
