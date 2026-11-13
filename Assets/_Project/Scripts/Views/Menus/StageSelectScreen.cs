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
            Action<int> onPlay)
        {
            RectTransform root = Ui.Stretch(parent, "StageSelectScreen");
            var screen = root.gameObject.AddComponent<StageSelectScreen>();
            screen.Build(content, profile, onPlay);
            return screen;
        }

        private void Build(GameContent content, PlayerProfile profile, Action<int> onPlay)
        {
            Transform root = transform;
            Ui.Box(root, "Background", Palette.Background);
            Ui.Label(root, "Puzzle Kingdoms", 88, TextAnchor.MiddleCenter, Palette.Gold, 0f, 0.83f, 1f, 0.93f);
            Ui.Label(root, "Chapter 1 - grey-box prototype", 40, TextAnchor.MiddleCenter, Palette.Muted, 0f, 0.78f, 1f, 0.83f);
            Ui.Label(root, $"Gold: {profile.Gold}", 48, TextAnchor.MiddleCenter, Palette.Text, 0f, 0.71f, 1f, 0.77f);

            List<string> order = content.CampaignOrder();
            IReadOnlyList<StageDefinition> stages = content.Campaign;
            float y = 0.6f;
            for (int i = 0; i < stages.Count; i++)
            {
                int index = i;
                bool unlocked = profile.IsUnlocked(order, i);
                string caption = $"{i + 1}. {stages[i].DisplayName}   " +
                                 (unlocked ? ResultsScreen.Stars(profile.BestStars(stages[i].Id)) : "(locked)");
                Button button = Ui.Button(root, caption, unlocked ? Palette.Primary : Palette.Panel,
                    () => onPlay(index), 0.1f, y, 0.9f, y + 0.08f, 46);
                button.interactable = unlocked;
                y -= 0.1f;
            }
            Ui.Label(root, "Puzzle > War Camp > Battle. Watch the enemy while you recruit.", 34,
                TextAnchor.MiddleCenter, Palette.Muted, 0.06f, 0.05f, 0.94f, 0.12f);
        }
    }
}
