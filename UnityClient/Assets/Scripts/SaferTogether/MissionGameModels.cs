using System;
using UnityEngine;

namespace SaferTogether.UnityClient
{
    // one stage of a mission mini-game (a puzzle stage, a code stage, etc.)
    [Serializable]
    public class MissionStageResult
    {
        public int index;
        public string label;
        public float timeSeconds;
        public bool correct;
        public int wrongAttempts;
        public float rotation;   // phone tilt strength during the stage (0 on desktop)
    }

    // the full result of one mini-game, reported back when it finishes
    [Serializable]
    public class MissionGameResult
    {
        public string game;                 // "puzzle" | "code" | "missile"
        public MissionStageResult[] stages;
        public float weightedScore;         // code game: early-stage mistakes weigh more
        public int hits;                    // missile game: times the player was hit
        public float tiltStrength;          // missile game: accumulated phone tilt
        public float totalSeconds;
    }

    // shared text helpers for the mission games
    public static class MissionText
    {
        // Legacy uGUI Text lays glyphs left-to-right. Reverse Hebrew letter runs so Hebrew
        // words read correctly, while keeping numbers like 45 and 1/4 in normal order.
        public static string Rtl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var builder = new System.Text.StringBuilder(value.Length);

            for (int i = 0; i < value.Length;)
            {
                if (!IsHebrew(value[i]))
                {
                    builder.Append(value[i]);
                    i += 1;
                    continue;
                }

                int start = i;
                while (i < value.Length && IsHebrew(value[i]))
                {
                    i += 1;
                }

                for (int j = i - 1; j >= start; j -= 1)
                {
                    builder.Append(value[j]);
                }
            }

            return builder.ToString();
        }

        // Full right-to-left reordering for a single short Hebrew line. Unlike Rtl (which only
        // flips letters inside each run and so leaves multi-word phrases and list markers in the
        // wrong order), this reverses the whole line so words, spaces and trailing punctuation all
        // read correctly under the LTR legacy Text engine. Single-digit markers like "1)" stay
        // readable; avoid it for lines with multi-digit numbers (they would be reversed too).
        public static string RtlLine(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            char[] chars = value.ToCharArray();
            System.Array.Reverse(chars);
            return new string(chars);
        }

        private static bool IsHebrew(char value)
        {
            return value >= '\u0590' && value <= '\u05FF';
        }
    }

    public static class MissionFonts
    {
        private static Font uiFont;

        public static Font UiFont
        {
            get
            {
                if (uiFont == null)
                {
                    uiFont = Resources.Load<Font>("Fonts/LiberationSans")
                        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return uiFont;
            }
        }
    }
}
