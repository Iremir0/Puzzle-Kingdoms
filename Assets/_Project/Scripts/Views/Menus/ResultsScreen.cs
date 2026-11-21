using System;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>Victory/defeat, stars and the gold breakdown of one attempt.</summary>
    public sealed class ResultsScreen : MonoBehaviour
    {
        /// <summary>Callbacks for the buttons; a null action hides its button.</summary>
        public sealed class Actions
        {
            public Action Retry;
            public Action Next;
            public Action StageSelect;
        }

        public static ResultsScreen Create(Transform parent, string stageName, BattleResult result, float battleSeconds,
            StageReward reward, int totalGold, Actions actions, bool stageWon = true, string goalMissed = null,
            string commandersUnlocked = "", string classesUnlocked = "")
        {
            RectTransform root = Ui.Stretch(parent, "ResultsScreen");
            var screen = root.gameObject.AddComponent<ResultsScreen>();
            screen.Build(stageName, result, battleSeconds, reward, totalGold, actions, stageWon && result.Victory, goalMissed, commandersUnlocked, classesUnlocked);
            return screen;
        }

        /// <summary>The hint for a lost battle (PB-35, D-069).</summary>
        public static string DefeatHint(DefeatReason reason)
        {
            switch (reason)
            {
                case DefeatReason.TimeUp:
                    return Loc.T("Time ran out. Merge units for a stronger, faster army.");
                case DefeatReason.RallyUnused:
                    return Loc.T("Tip: tap Rally during the battle to power up your army.");
                case DefeatReason.Outnumbered:
                    return Loc.T("Outnumbered. Make more matches to recruit more soldiers.");
                default:
                    return Loc.T("Their army was stronger. Merge three of a kind in the War Camp.");
            }
        }

        private void Build(string stageName, BattleResult result, float battleSeconds, StageReward reward, int totalGold,
            Actions actions, bool won, string goalMissed, string commandersUnlocked, string classesUnlocked)
        {
            Transform root = transform;
            Ui.Backdrop(root);
            Ui.Label(root, Loc.T(stageName), 48, TextAnchor.MiddleCenter, Palette.Muted, 0f, 0.84f, 1f, 0.9f);
            Text title = Ui.Label(root, won ? Loc.T("Victory!") : Loc.T("Defeat"), 110, TextAnchor.MiddleCenter,
                won ? Palette.Gold : Palette.Bad, 0f, 0.72f, 1f, 0.84f);
            // PB-38: three star shapes, earned ones gold and popping in one after the other.
            RectTransform starRow = Ui.Rect(root, "Stars", 0.25f, 0.635f, 0.75f, 0.715f);
            for (int i = 0; i < 3; i++)
            {
                bool earned = i < reward.Stars;
                Image star = Ui.Box(starRow, "Star " + (i + 1), earned ? Palette.Gold : new Color(1f, 1f, 1f, 0.15f),
                    i / 3f + 0.02f, 0f, (i + 1) / 3f - 0.02f, 1f);
                star.sprite = GemShapes.Get(GemShape.Star);
                star.preserveAspect = true;
                star.raycastTarget = false;
                if (earned)
                {
                    StartCoroutine(UiJuice.PopIn(star.rectTransform, 0.35f + 0.25f * i, 0.45f));
                }
            }
            if (won)
            {
                StartCoroutine(UiJuice.PopIn(title.rectTransform, 0f, 0.5f));
                UiJuice.Confetti(root, 70);
                Haptics.Success();
            }
            if (!string.IsNullOrEmpty(goalMissed))
            {
                // PB-19: the battle was won but the stage goal was not.
                Ui.Label(root, goalMissed, 38, TextAnchor.MiddleCenter, Palette.Bad, 0.06f, 0.6f, 0.94f, 0.66f);
            }

            string lines =
                Loc.F("Survivors: {0} / {1}", result.Survivors, result.Deployed) + "\n" +
                Loc.F("Battle time: {0:0.0} s", battleSeconds) + (result.TimedOut ? " " + Loc.T("(time up)") : "") + "\n\n" +
                Loc.F("Victory gold: {0}", reward.VictoryGold) + (reward.Replay && won ? " " + Loc.T("(replay: half gold)") : "") + "\n" +
                Loc.F("Dismissed units: {0}", reward.DismissGold) + "\n" +
                Loc.F("Extra Banner points: {0}", reward.OverflowGold) + "\n" +
                Loc.F("Total: +{0} gold   (you have {1})", reward.TotalGold, totalGold);
            Ui.Card(root, "Stats Panel", Palette.Panel, 0.05f, 0.285f, 0.95f, 0.625f);
            Ui.Label(root, lines, 44, TextAnchor.UpperCenter, Palette.Text, 0.06f, 0.3f, 0.94f, 0.62f);
            string unlockLine = "";
            if (!string.IsNullOrEmpty(commandersUnlocked))
            {
                unlockLine = Loc.F("Commander unlocked: {0}", commandersUnlocked); // PB-23
            }
            if (!string.IsNullOrEmpty(classesUnlocked))
            {
                unlockLine += (unlockLine.Length > 0 ? "  ·  " : "") + Loc.F("Class unlocked: {0}", classesUnlocked); // PB-21
            }
            if (unlockLine.Length > 0)
            {
                Ui.Label(root, unlockLine, 38, TextAnchor.MiddleCenter, Palette.Good, 0.04f, 0.215f, 0.96f, 0.275f);
            }
            else if (!won && string.IsNullOrEmpty(goalMissed) && result.Reason != DefeatReason.None)
            {
                // PB-35: one line on what would help next time.
                Text hint = Ui.Label(root, DefeatHint(result.Reason), 36, TextAnchor.MiddleCenter, Palette.Gold, 0.04f, 0.215f, 0.96f, 0.275f);
                hint.gameObject.name = "Defeat Hint";
            }

            float y = 0.2f;
            if (actions.Next != null)
            {
                Ui.Button(root, "Next stage", Ui.ButtonStyle.Primary, actions.Next, 0.15f, y, 0.85f, y + 0.07f, 52);
                y -= 0.09f;
            }
            if (actions.Retry != null)
            {
                Ui.Button(root, "Retry", won ? Ui.ButtonStyle.Secondary : Ui.ButtonStyle.Primary, actions.Retry,
                    0.15f, y, 0.85f, y + 0.07f, 52);
                y -= 0.09f;
            }
            if (actions.StageSelect != null)
            {
                Ui.Button(root, "Stage map", Ui.ButtonStyle.Secondary, actions.StageSelect, 0.15f, y, 0.85f, y + 0.07f, 52);
            }
        }

        /// <summary>"* * -" style stars as text (logs and tests; the screen draws star shapes since PB-38).</summary>
        public static string Stars(int stars)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 1; i <= 3; i++)
            {
                text.Append(i <= stars ? "*" : "-");
                if (i < 3)
                {
                    text.Append(' ');
                }
            }
            return text.ToString();
        }
    }
}
