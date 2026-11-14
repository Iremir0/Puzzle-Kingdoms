using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Army;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>Grey-box colors (UI chrome) and per-unit colors read from the unit assets.</summary>
    public static class Palette
    {
        public static readonly Color Background = Hex("1E2230");
        public static readonly Color Panel = Hex("2A3042");
        public static readonly Color PanelLight = Hex("394159");
        public static readonly Color Text = Color.white;
        public static readonly Color Muted = Hex("9AA3B5");
        public static readonly Color Gold = Hex("F4BE4F");
        public static readonly Color Good = Hex("58C889");
        public static readonly Color Bad = Hex("E85B55");
        public static readonly Color Button = Hex("4A5675");
        public static readonly Color Primary = Hex("3D7BD9");
        public static readonly Color Highlight = new Color(1f, 1f, 1f, 0.9f);
        public static readonly Color EnemyFallback = Hex("8C7A5B");

        /// <summary>Banner gem / Rally (GDD §5, Kingdom Gold).</summary>
        public static readonly Color Rally = Gold;

        private static readonly Dictionary<string, Color> UnitColors = new Dictionary<string, Color>();
        private static readonly Dictionary<string, string> UnitTags = new Dictionary<string, string>();
        private static readonly Dictionary<string, GemShape> UnitShapes = new Dictionary<string, GemShape>();
        private static readonly Dictionary<string, UnitLook> UnitLooks = new Dictionary<string, UnitLook>();

        /// <summary>Shapes handed to unit gems in registration order (recruitable units register first).</summary>
        private static readonly GemShape[] UnitShapeOrder =
        {
            GemShape.Square, GemShape.Triangle, GemShape.Diamond, GemShape.Circle,
        };

        /// <summary>Remembers each unit's color by id so logic objects (UnitDef) can be drawn.</summary>
        public static void Register(UnitDefinition unit)
        {
            if (unit != null)
            {
                UnitColors[unit.Def.Id] = unit.Color;
                UnitTags[unit.Def.Id] = Initials(unit.Def);
                UnitLooks[unit.Def.Id] = unit.Look;
                if (!UnitShapes.ContainsKey(unit.Def.Id))
                {
                    UnitShapes[unit.Def.Id] = UnitShapeOrder[UnitShapes.Count % UnitShapeOrder.Length];
                }
            }
        }

        /// <summary>Silhouette of a unit in the battle view (Generic for units that were never registered).</summary>
        public static UnitLook LookFor(UnitDef def) =>
            def != null && UnitLooks.TryGetValue(def.Id, out UnitLook look) ? look : UnitLook.Generic;

        public static Color For(UnitDef def) =>
            def != null && UnitColors.TryGetValue(def.Id, out Color color) ? color : EnemyFallback;

        /// <summary>Color of a gem by its reward key (a unit id, or the Rally key).</summary>
        public static Color ForGem(string rewardKey, string rallyKey) =>
            rewardKey == rallyKey ? Rally
            : UnitColors.TryGetValue(rewardKey, out Color color) ? color : EnemyFallback;

        /// <summary>
        /// Shape of a gem: Rally is always a star, each unit class gets its own shape
        /// (so gems differ by shape as well as by color).
        /// </summary>
        public static GemShape ShapeForGem(string rewardKey, string rallyKey) =>
            rewardKey == rallyKey ? GemShape.Star
            : UnitShapes.TryGetValue(rewardKey, out GemShape shape) ? shape : GemShape.Circle;

        /// <summary>Letter for a meter label: the unit's initials, "R" for Rally.</summary>
        public static string GemTag(string rewardKey, string rallyKey) =>
            rewardKey == rallyKey ? "R"
            : UnitTags.TryGetValue(rewardKey, out string tag) ? tag : rewardKey.Substring(0, 1).ToUpperInvariant();

        /// <summary>Short tag such as "W" or "GB" (initials of the display name).</summary>
        public static string Initials(UnitDef def)
        {
            string name = def == null ? "?" : (string.IsNullOrEmpty(def.DisplayName) ? def.Id : def.DisplayName);
            var initials = new System.Text.StringBuilder();
            foreach (string word in name.Split(' ', '_'))
            {
                if (word.Length > 0)
                {
                    initials.Append(char.ToUpperInvariant(word[0]));
                }
            }
            return initials.ToString();
        }

        /// <summary>"W", "W2", "W3": the tag plus the tier above 1.</summary>
        public static string Tag(ArmyUnit unit) =>
            unit == null ? "" : Initials(unit.Def) + (unit.Tier > 1 ? unit.Tier.ToString() : "");

        public static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out Color color);
            return color;
        }
    }
}
