using System;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>The main tabs along the bottom of the map and the Kingdom (PB-22).</summary>
    public enum MainTab
    {
        Map = 0,
        Kingdom = 1,
        Commanders = 2,
        Shop = 3,
        Quests = 4,
    }

    /// <summary>
    /// Bottom tab bar: Map · Kingdom · Commanders · Quests · Shop. The current tab is blue; tabs whose card has not
    /// landed yet (Commanders: PB-23, Shop: PB-30) are greyed and do nothing.
    /// </summary>
    public static class TabBar
    {
        public const float Height = 0.085f;

        /// <summary>Rewards waiting in the Quests tab (PB-24); a red dot with the count shows on the tab while above 0.</summary>
        public static int QuestBadge { get; set; }
        private static readonly Color Soon = Palette.Hex("323A4E");

        private static readonly (MainTab Tab, string Caption, bool Ready)[] Tabs =
        {
            (MainTab.Map, "Map", true),
            (MainTab.Kingdom, "Kingdom", true),
            (MainTab.Commanders, "Commanders", true),
            (MainTab.Quests, "Quests", true),
            (MainTab.Shop, "Shop", false),
        };


        public static RectTransform Add(Transform parent, MainTab current, Action<MainTab> onSelect)
        {
            RectTransform bar = Ui.Rect(parent, "Tab Bar", 0f, 0f, 1f, Height);
            Ui.Card(bar, "Backdrop", Palette.Panel, 0f, 0f, 1f, 1f);
            float width = 1f / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                (MainTab tab, string caption, bool ready) = Tabs[i];
                bool isCurrent = tab == current;
                Color color = isCurrent ? Palette.Primary : ready ? Palette.Button : Soon;
                MainTab target = tab;
                Button button = Ui.Button(bar, caption, color, () => onSelect?.Invoke(target),
                    i * width + 0.006f, 0.12f, (i + 1) * width - 0.006f, 0.88f, 30);
                // An icon above the caption: a shape per tab.
                Text text = button.GetComponentInChildren<Text>();
                Ui.SetAnchors(text.rectTransform, 0f, 0f, 1f, 0.46f);
                Image icon = Ui.Box(button.transform, "Icon", new Color(1f, 1f, 1f, ready ? 0.95f : 0.4f), 0.32f, 0.46f, 0.68f, 0.98f);
                icon.sprite = TabIcons.Get(tab);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                button.gameObject.name = "Tab " + caption;
                if (tab == MainTab.Quests && QuestBadge > 0)
                {
                    Image dot = Ui.Box(button.transform, "Badge", Palette.Bad, 0.62f, 0.62f, 0.9f, 0.98f);
                    dot.sprite = GemShapes.Get(GemShape.Circle);
                    dot.preserveAspect = true;
                    dot.raycastTarget = false;
                    Ui.Label(dot.transform, QuestBadge.ToString(), 26, TextAnchor.MiddleCenter, Color.white);
                }
                button.interactable = ready && !isCurrent;
                if (isCurrent)
                {
                    // The current tab keeps its full colour even though pressing it does nothing.
                    ColorBlock colors = button.colors;
                    colors.disabledColor = Color.white;
                    button.colors = colors;
                }
                if (!ready)
                {
                    button.GetComponentInChildren<Text>().color = Palette.Muted;
                }
            }
            return bar;
        }
    }
}
