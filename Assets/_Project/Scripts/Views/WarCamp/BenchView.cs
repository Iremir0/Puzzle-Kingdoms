using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.WarCamp
{
    /// <summary>
    /// The War Camp bench (PB-9): one tile per class and tier with "×N", in a row
    /// that scrolls sideways. Gold edge = enough copies to merge. Tapping or
    /// dragging a tile picks one unit of the group.
    /// </summary>
    public sealed class BenchView
    {
        public const float TileWidth = 190f;
        public const float Gap = 14f;

        public readonly RectTransform Panel;
        public readonly ScrollRect Scroll;
        private readonly RectTransform _content;
        private readonly Outline _dropOutline;
        private readonly Text _empty;
        private readonly Dictionary<(UnitDef, int), RectTransform> _tiles = new Dictionary<(UnitDef, int), RectTransform>();

        public BenchView(Image panel)
        {
            Panel = panel.rectTransform;
            RectTransform viewport = Ui.Stretch(Panel, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = Ui.Rect(viewport, "Tiles", 0f, 0f, 0f, 1f);
            _content.pivot = new Vector2(0f, 0.5f);

            Scroll = panel.gameObject.AddComponent<ScrollRect>();
            Scroll.viewport = viewport;
            Scroll.content = _content;
            Scroll.horizontal = true;
            Scroll.vertical = false;
            Scroll.movementType = ScrollRect.MovementType.Clamped;
            Scroll.scrollSensitivity = 30f;

            _dropOutline = panel.gameObject.AddComponent<Outline>();
            _dropOutline.effectColor = Palette.Primary;
            _dropOutline.effectDistance = new Vector2(6, 6);
            _dropOutline.enabled = false;

            _empty = Ui.Label(viewport, "Empty", 34, TextAnchor.MiddleCenter, Palette.Muted);
        }

        /// <summary>Groups shown by the last <see cref="Show"/>, left to right.</summary>
        public IReadOnlyList<BenchGroup> Groups { get; private set; } = new List<BenchGroup>();

        /// <summary>True when the row is wider than the panel, so it scrolls.</summary>
        public bool Overflows => _content.rect.width > Panel.rect.width + 0.5f;

        /// <summary>The tile of a class and tier, or null if the bench has none.</summary>
        public RectTransform TileFor(UnitDef def, int tier) =>
            _tiles.TryGetValue((def, tier), out RectTransform tile) && tile != null ? tile : null;

        /// <summary>Blue edge while a deployed unit is dragged over the bench (drop = send it back).</summary>
        public void SetDropHover(bool on) => _dropOutline.enabled = on;

        public bool Contains(Vector2 screenPoint, Camera eventCamera) =>
            RectTransformUtility.RectangleContainsScreenPoint(Panel, screenPoint, eventCamera);

        public void Show(List<BenchGroup> groups, ArmyUnit selected, Func<ArmyUnit, bool> canMerge,
            Action<BenchGroup> onTap, IUnitDragHandler dragHandler)
        {
            Groups = groups;
            _tiles.Clear();
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                // Detach first, so the new tiles are laid out without the old ones.
                Transform old = _content.GetChild(i);
                old.SetParent(null, false);
                UnityEngine.Object.Destroy(old.gameObject);
            }
            _content.sizeDelta = new Vector2(groups.Count == 0 ? 0f : Gap + groups.Count * (TileWidth + Gap), 0f);
            _empty.enabled = groups.Count == 0;

            for (int i = 0; i < groups.Count; i++)
            {
                BenchGroup group = groups[i];
                Button button = Ui.Button(_content, "", Palette.For(group.Def), () => onTap(group), 0f, 0f, 0f, 1f, 36);
                var tile = (RectTransform)button.transform;
                tile.name = $"Tile {group.Def.Id} T{group.Tier}";
                tile.pivot = new Vector2(0f, 0.5f);
                tile.anchoredPosition = new Vector2(Gap + i * (TileWidth + Gap), 0f);
                tile.sizeDelta = new Vector2(TileWidth, -2f * Gap);
                _tiles[(group.Def, group.Tier)] = tile;

                UnitSilhouettes.AddIcon(tile, Palette.LookFor(group.Def), group.Tier);
                UnitSilhouettes.AddTierLabel(tile, group.Tier, 28);
                Text count = Ui.Label(tile, "×" + group.Count, 34, TextAnchor.UpperRight, Color.white, 0.3f, 0.6f, 0.95f, 0.98f);
                count.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);

                bool isSelected = selected != null && group.Units.Contains(selected);
                if (isSelected || canMerge(group.First))
                {
                    // White = selected; gold = enough copies to merge.
                    Outline outline = tile.gameObject.AddComponent<Outline>();
                    outline.effectColor = isSelected ? Palette.Highlight : Palette.Gold;
                    outline.effectDistance = isSelected ? new Vector2(6, 6) : new Vector2(4, 4);
                }
                DraggableUnit.Add(tile.gameObject, () => group.Count > 0 ? group.First : null, dragHandler, Scroll);
            }
        }
    }
}
