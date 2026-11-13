using System;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;

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
            StageReward reward, int totalGold, Actions actions)
        {
            RectTransform root = Ui.Stretch(parent, "ResultsScreen");
            var screen = root.gameObject.AddComponent<ResultsScreen>();
            screen.Build(stageName, result, battleSeconds, reward, totalGold, actions);
            return screen;
        }

        private void Build(string stageName, BattleResult result, float battleSeconds, StageReward reward, int totalGold,
            Actions actions)
        {
            Transform root = transform;
            Ui.Box(root, "Background", Palette.Background);
            Ui.Label(root, stageName, 48, TextAnchor.MiddleCenter, Palette.Muted, 0f, 0.84f, 1f, 0.9f);
            Ui.Label(root, result.Victory ? "Victory!" : "Defeat", 110, TextAnchor.MiddleCenter,
                result.Victory ? Palette.Gold : Palette.Bad, 0f, 0.72f, 1f, 0.84f);
            Ui.Label(root, Stars(reward.Stars), 90, TextAnchor.MiddleCenter, Palette.Gold, 0f, 0.63f, 1f, 0.72f);

            string lines =
                $"Survivors: {result.Survivors} / {result.Deployed}\n" +
                $"Battle time: {battleSeconds:0.0} s{(result.TimedOut ? " (time up)" : "")}\n\n" +
                $"Victory gold: {reward.VictoryGold}\n" +
                $"Dismissed units: {reward.DismissGold}\n" +
                $"Extra Banner points: {reward.OverflowGold}\n" +
                $"Total: +{reward.TotalGold} gold   (you have {totalGold})";
            Ui.Label(root, lines, 44, TextAnchor.UpperCenter, Palette.Text, 0.06f, 0.3f, 0.94f, 0.62f);

            float y = 0.2f;
            if (actions.Next != null)
            {
                Ui.Button(root, "Next stage", Palette.Primary, actions.Next, 0.15f, y, 0.85f, y + 0.07f, 52);
                y -= 0.09f;
            }
            if (actions.Retry != null)
            {
                Ui.Button(root, "Retry", result.Victory ? Palette.Button : Palette.Primary, actions.Retry,
                    0.15f, y, 0.85f, y + 0.07f, 52);
                y -= 0.09f;
            }
            if (actions.StageSelect != null)
            {
                Ui.Button(root, "Stage map", Palette.Button, actions.StageSelect, 0.15f, y, 0.85f, y + 0.07f, 52);
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
