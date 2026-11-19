using System;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// The saved battle replays (PB-28, D-063): the last few battles and the starred ones, each with a Watch button.
    /// A replay made with older rules cannot be watched (it would play out differently) and says so.
    /// </summary>
    public sealed class ReplaysScreen : MonoBehaviour
    {
        public static ReplaysScreen Create(Transform parent, ReplayLog log, Func<ReplayEntry, bool> isPlayable,
            Action<ReplayEntry> onWatch, Action<ReplayEntry> onToggleStar, Action onBack, int ticksPerSecond)
        {
            RectTransform root = Ui.Stretch(parent, "ReplaysScreen");
            var screen = root.gameObject.AddComponent<ReplaysScreen>();
            screen.Build(log, isPlayable, onWatch, onToggleStar, onBack, ticksPerSecond);
            return screen;
        }

        /// <summary>The Watch button of a row (tests press it); null when the row does not exist.</summary>
        public Button WatchButton(int index)
        {
            Transform button = transform.Find("Row " + index + "/Button Watch");
            return button != null ? button.GetComponent<Button>() : null;
        }

        public Button StarButton(int index)
        {
            Transform button = transform.Find("Row " + index + "/Button Keep");
            return button != null ? button.GetComponent<Button>() : null;
        }

        /// <summary>"Expedition 2" keeps its number while the word is translated.</summary>
        private static string TitleText(string title) =>
            title.StartsWith("Expedition ", StringComparison.Ordinal) ? Loc.T("Expedition") + title.Substring(10) : Loc.T(title);

        private void Build(ReplayLog log, Func<ReplayEntry, bool> isPlayable, Action<ReplayEntry> onWatch,
            Action<ReplayEntry> onToggleStar, Action onBack, int ticksPerSecond)
        {
            Transform root = transform;
            Ui.Backdrop(root);
            Ui.Label(root, Loc.T("Battle replays"), 48, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.8f, 0.99f);
            if (log.Entries.Count == 0)
            {
                Ui.Label(root, Loc.T("No replays yet. Every battle you fight is saved here."), 34, TextAnchor.MiddleCenter, Palette.Muted,
                    0.08f, 0.5f, 0.92f, 0.7f);
            }
            const float rowHeight = 0.054f;
            for (int i = 0; i < log.Entries.Count; i++)
            {
                ReplayEntry entry = log.Entries[i];
                float yMax = 0.92f - i * (rowHeight + 0.004f);
                bool playable = isPlayable(entry);
                RectTransform row = Ui.Card(root, "Row " + i, entry.Starred ? Color.Lerp(Palette.Panel, Palette.Gold, 0.15f) : Palette.Panel,
                    0.03f, yMax - rowHeight, 0.97f, yMax).rectTransform;
                Ui.Label(row, TitleText(entry.Title), 30, TextAnchor.MiddleLeft, Palette.Text, 0.03f, 0.08f, 0.5f, 0.95f);
                string result = (entry.Victory ? Loc.T("Won") : Loc.T("Lost")) + "  " + Loc.F("{0} s", Mathf.RoundToInt(entry.Ticks / (float)ticksPerSecond));
                Ui.Label(row, playable ? result : Loc.T("Older version"), 26, TextAnchor.MiddleLeft,
                    playable ? (entry.Victory ? Palette.Good : Palette.Bad) : Palette.Muted, 0.5f, 0.08f, 0.7f, 0.95f);
                ReplayEntry captured = entry;
                Button keep = Ui.Button(row, entry.Starred ? "Kept" : "Keep", entry.Starred ? Palette.Gold : Palette.Button,
                    () => onToggleStar(captured), 0.7f, 0.12f, 0.83f, 0.88f, 24);
                keep.gameObject.name = "Button Keep";
                Button watch = Ui.Button(row, "Watch", Ui.ButtonStyle.Primary, () => onWatch(captured), 0.85f, 0.12f, 0.98f, 0.88f, 26);
                watch.gameObject.name = "Button Watch";
                watch.interactable = playable;
            }
            Ui.Button(root, "Back", Ui.ButtonStyle.Secondary, onBack, 0.3f, 0.02f, 0.7f, 0.085f, 40);
        }
    }
}
