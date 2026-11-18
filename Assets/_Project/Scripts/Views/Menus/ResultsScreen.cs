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
            string commandersUnlocked = "")
        {
            RectTransform root = Ui.Stretch(parent, "ResultsScreen");
            var screen = root.gameObject.AddComponent<ResultsScreen>();
            screen.Build(stageName, result, battleSeconds, reward, totalGold, actions, stageWon && result.Victory, goalMissed, commandersUnlocked);
            return screen;
        }

        private void Build(string stageName, BattleResult result, float battleSeconds, StageReward reward, int totalGold,
            Actions actions, bool won, string goalMissed, string commandersUnlocked)
        {
            Transform root = transform;
            Ui.Backdrop(root);
            Ui.Label(root, Loc.T(stageName), 48, TextAnchor.MiddleCenter, Palette.Muted, 0f, 0.84f, 1f, 0.9f);
            Text title = Ui.Label(root, won ? Loc.T("Victory!") : Loc.T("Defeat"), 110, TextAnchor.MiddleCenter,
                won ? Palette.Gold : Palette.Bad, 0f, 0.72f, 1f, 0.84f);
            Text starsLabel = Ui.Label(root, Stars(reward.Stars), 90, TextAnchor.MiddleCenter, Palette.Gold, 0f, 0.63f, 1f, 0.72f);
            if (won)
            {
                StartCoroutine(UiJuice.PopIn(title.rectTransform, 0f, 0.5f));
                StartCoroutine(UiJuice.PopIn(starsLabel.rectTransform, 0.35f, 0.5f));
                UiJuice.Confetti(root, 70);
            }
            if (!string.IsNullOrEmpty(goalMissed))
            {
                // PB-19: the battle was won but the stage goal was not.
                Ui.Label(root, goalMissed, 38, TextAnchor.MiddleCenter, Palette.Bad, 0.06f, 0.6f, 0.94f, 0.66f);
            }

            string lines =
                Loc.F("Survivors: {0} / {1}", result.Survivors, result.Deployed) + "\n" +
                Loc.F("Battle time: {0:0.0} s", battleSeconds) + (result.TimedOut ? " " + Loc.T("(time up)") : "") + "\n\n" +
                Loc.F("Victory gold: {0}", reward.VictoryGold) + "\n" +
                Loc.F("Dismissed units: {0}", reward.DismissGold) + "\n" +
                Loc.F("Extra Banner points: {0}", reward.OverflowGold) + "\n" +
                Loc.F("Total: +{0} gold   (you have {1})", reward.TotalGold, totalGold);
            Ui.Card(root, "Stats Panel", Palette.Panel, 0.05f, 0.285f, 0.95f, 0.625f);
            Ui.Label(root, lines, 44, TextAnchor.UpperCenter, Palette.Text, 0.06f, 0.3f, 0.94f, 0.62f);
            if (!string.IsNullOrEmpty(commandersUnlocked))
            {
                // PB-23: a stage that unlocks a commander says so.
                Ui.Label(root, Loc.F("Commander unlocked: {0}", commandersUnlocked), 44, TextAnchor.MiddleCenter, Palette.Good,
                    0.04f, 0.215f, 0.96f, 0.275f);
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

        /// <summary>"* * -" style stars; the built-in font has no star glyph.</summary>
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
