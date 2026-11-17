using System;
using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>Chapter 1 stage list with best stars, lock state and the player's gold.</summary>
    public sealed class StageSelectScreen : MonoBehaviour
    {
        public static StageSelectScreen Create(Transform parent, GameContent content, PlayerProfile profile,
            Action<int> onPlay, Action onSettings = null)
        {
            RectTransform root = Ui.Stretch(parent, "StageSelectScreen");
            var screen = root.gameObject.AddComponent<StageSelectScreen>();
            screen.Build(content, profile, onPlay, onSettings);
            return screen;
        }

        private void Build(GameContent content, PlayerProfile profile, Action<int> onPlay, Action onSettings)
        {
            Transform root = transform;
            Ui.Box(root, "Background", Palette.Background);
            if (onSettings != null)
            {
                Ui.Button(root, "Settings", Palette.Button, onSettings, 0.7f, 0.935f, 0.97f, 0.985f, 36);
            }
            Ui.Label(root, "Puzzle Kingdoms", 88, TextAnchor.MiddleCenter, Palette.Gold, 0f, 0.83f, 1f, 0.93f);
            Ui.Label(root, "Chapter 1 - grey-box prototype", 40, TextAnchor.MiddleCenter, Palette.Muted, 0f, 0.78f, 1f, 0.83f);
            Ui.Label(root, $"Gold: {profile.Gold}", 48, TextAnchor.MiddleCenter, Palette.Text, 0f, 0.71f, 1f, 0.77f);

            List<string> order = content.CampaignOrder();
            IReadOnlyList<StageDefinition> stages = content.Campaign;
            float top = 0.69f;
            for (int i = 0; i < stages.Count; i++)
            {
                int index = i;
                bool unlocked = profile.IsUnlocked(order, i);
                // Two rows per stage: name and stars, then the difficulty badges (PB-3).
                Button button = Ui.Button(root, "", unlocked ? Palette.Primary : Palette.Panel,
                    () => onPlay(index), 0.08f, top - 0.12f, 0.92f, top, 46);
                button.interactable = unlocked;
                Color text = unlocked ? Palette.Text : Palette.Muted;
                Ui.Label(button.transform, $"{i + 1}. {stages[i].DisplayName}", 46, TextAnchor.MiddleLeft, text,
                    0.04f, 0.5f, 0.7f, 0.95f);
                Ui.Label(button.transform,
                    unlocked ? ResultsScreen.Stars(profile.BestStars(stages[i].Id)) : "locked", 40,
                    TextAnchor.MiddleRight, unlocked ? Palette.Gold : Palette.Muted, 0.6f, 0.5f, 0.96f, 0.95f);
                StageBadges.AddRow(button.transform, stages[i].ToStageDef(), !unlocked, 0.03f, 0.08f, 0.97f, 0.45f);
                top -= 0.14f;
            }
            Ui.Label(root, "Puzzle > War Camp > Battle. Watch the enemy while you recruit.", 34,
                TextAnchor.MiddleCenter, Palette.Muted, 0.06f, 0.05f, 0.94f, 0.12f);
        }
    }
}
