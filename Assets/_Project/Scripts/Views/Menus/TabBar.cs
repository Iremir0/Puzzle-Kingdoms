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
    }

    /// <summary>
    /// Bottom tab bar: Map · Kingdom · Commanders · Shop. The current tab is blue; tabs whose card has not
    /// landed yet (Commanders: PB-23, Shop: PB-30) are greyed and do nothing.
    /// </summary>
    public static class TabBar
    {
        public const float Height = 0.085f;
        private static readonly Color Soon = Palette.Hex("323A4E");

        private static readonly (MainTab Tab, string Caption, bool Ready)[] Tabs =
        {
            (MainTab.Map, "Map", true),
            (MainTab.Kingdom, "Kingdom", true),
            (MainTab.Commanders, "Commanders", true),
            (MainTab.Shop, "Shop", false),
        };

        private static GemShape IconFor(MainTab tab) =>
            tab == MainTab.Map ? GemShape.Triangle : tab == MainTab.Kingdom ? GemShape.Square
            : tab == MainTab.Commanders ? GemShape.Star : GemShape.Circle;

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
                Ui.SetAnchors(text.rectTransform, 0f, 0f, 1f, 0.5f);
                Image icon = Ui.Box(button.transform, "Icon", new Color(1f, 1f, 1f, ready ? 0.95f : 0.4f), 0.36f, 0.52f, 0.64f, 0.94f);
                icon.sprite = GemShapes.Get(IconFor(tab));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                button.gameObject.name = "Tab " + caption;
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
