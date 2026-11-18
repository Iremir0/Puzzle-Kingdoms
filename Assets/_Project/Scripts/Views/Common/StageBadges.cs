using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Stages;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// How hard a stage is, in words the player can read before the fight (PB-3):
    /// stone gems (D-032), the enemy strength multipliers (D-033) and the enemy
    /// count, or "Boss" when a Chief-class enemy leads the army.
    /// </summary>
    public static class StageBadges
    {
        /// <summary>Multipliers up to this are shown gold, above it red.</summary>
        private const float GoldUpTo = 1.15f;

        private static readonly Color StoneGemColor = Palette.Hex("C0453F");

        /// <summary>"No stones", "1 stone", "3 stones".</summary>
        public static string Stones(StageDef stage) =>
            stage.StoneGems <= 0 ? Loc.T("No stones") : stage.StoneGems == 1 ? Loc.T("1 stone") : Loc.F("{0} stones", stage.StoneGems);

        /// <summary>
        /// The stage goal in words (PB-19), empty for "defeat the army"; with <paramref name="stonesBroken"/>
        /// the progress of a stone goal ("Goal: break 6 stones  2/6").
        /// </summary>
        public static string GoalText(StageDef stage, int stonesBroken = -1, int iceMelted = -1, int iceTotal = 0)
        {
            StageGoal goal = stage.Goal;
            if (goal == null || goal.Kind == StageGoalKind.DefeatArmy)
            {
                return "";
            }
            if (goal.Kind == StageGoalKind.WinWithin)
            {
                return Loc.F("Goal: win within {0} s", goal.Target);
            }
            if (goal.Kind == StageGoalKind.BreakIce)
            {
                // PB-20: progress counts ice layers melted out of those on the start board.
                string ice = Loc.T("Goal: break all the ice");
                return iceMelted < 0 || iceTotal <= 0 ? ice : $"{ice}  {System.Math.Min(iceMelted, iceTotal)}/{iceTotal}";
            }
            string text = Loc.F("Goal: break {0} stones", goal.Target);
            return stonesBroken < 0 ? text : $"{text}  {System.Math.Min(stonesBroken, goal.Target)}/{goal.Target}";
        }

        /// <summary>
        /// The goal in the top right of the enemy panel that the puzzle screen and the War Camp share; null
        /// when the stage has no goal beyond winning.
        /// </summary>
        public static Text AddGoalLine(Transform root, StageDef stage, int stonesBroken = -1)
        {
            string text = GoalText(stage, stonesBroken);
            if (text.Length == 0)
            {
                return null;
            }
            Text label = Ui.Label(root, text, 30, TextAnchor.UpperRight, Palette.Gold, 0.66f, 0.76f, 0.95f, 0.915f);
            label.gameObject.name = "Goal";
            return label;
        }

        /// <summary>"×1.1" style multiplier text ("×1,1" in Turkish).</summary>
        public static string Times(float multiplier) => "×" + Loc.Number(multiplier, "0.0#");

        /// <summary>Grey at ×1.0 (or less), gold up to ×1.15, red above.</summary>
        public static Color MultiplierColor(float multiplier) =>
            multiplier <= 1.001f ? Palette.Muted : multiplier <= GoldUpTo + 0.001f ? Palette.Gold : Palette.Bad;

        /// <summary>True when a Chief-class enemy is in the army.</summary>
        public static bool HasBoss(StageDef stage)
        {
            foreach (EnemySlot slot in stage.Enemies)
            {
                if (Palette.LookFor(slot.Unit) == UnitLook.GoblinChief)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The badges of a stage, left to right: text and colour.</summary>
        public static List<(string Text, Color Color)> Badges(StageDef stage)
        {
            var badges = new List<(string, Color)>
            {
                (Stones(stage), stage.StoneGems > 0 ? Palette.Text : Palette.Muted),
                (Loc.F("Enemy {0}", Times(stage.EnemyHpMultiplier)), MultiplierColor(stage.EnemyHpMultiplier)),
            };
            if (Mathf.Abs(stage.EnemyDamageMultiplier - 1f) > 0.001f)
            {
                badges.Add((Loc.F("DMG {0}", Times(stage.EnemyDamageMultiplier)), MultiplierColor(stage.EnemyDamageMultiplier)));
            }
            badges.Add(HasBoss(stage) ? (Loc.T("Boss"), Palette.Bad) : (Loc.F("{0} enemies", stage.Enemies.Count), Palette.Muted));
            return badges;
        }

        /// <summary>One line for the enemy panels: "HP ×1.1 · 3 stones".</summary>
        public static string PanelLine(StageDef stage)
        {
            string line = Loc.F("HP {0}", Times(stage.EnemyHpMultiplier));
            if (Mathf.Abs(stage.EnemyDamageMultiplier - 1f) > 0.001f)
            {
                line += " · " + Loc.F("DMG {0}", Times(stage.EnemyDamageMultiplier));
            }
            return line + " · " + Stones(stage);
        }

        /// <summary>
        /// The <see cref="PanelLine"/> under the "Enemy army" title of the enemy panel
        /// that the puzzle screen and the War Camp share (left of the enemy grid).
        /// </summary>
        public static Text AddPanelLine(Transform root, StageDef stage)
        {
            Text label = Ui.Label(root, PanelLine(stage), 30, TextAnchor.UpperLeft,
                MultiplierColor(Mathf.Max(stage.EnemyHpMultiplier, stage.EnemyDamageMultiplier)),
                0.06f, 0.76f, 0.34f, 0.865f);
            if (stage.EnemyHpMultiplier <= 1.001f && stage.StoneGems <= 0)
            {
                label.color = Palette.Muted;
            }
            return label;
        }

        /// <summary>
        /// Draws the badges in a row across the given rectangle: rounded-looking
        /// dark chips, the stone chip with a small half-stone gem in front.
        /// </summary>
        public static void AddRow(Transform parent, StageDef stage, bool dimmed,
            float xMin, float yMin, float xMax, float yMax, int fontSize = 30)
        {
            RectTransform row = Ui.Rect(parent, "Badges", xMin, yMin, xMax, yMax);
            List<(string Text, Color Color)> badges = Badges(stage);
            float alpha = dimmed ? 0.45f : 1f;
            float width = 1f / badges.Count;
            for (int i = 0; i < badges.Count; i++)
            {
                (string text, Color color) = badges[i];
                Image chip = Ui.Box(row, "Badge " + i, WithAlpha(Palette.Background, 0.55f * alpha),
                    i * width + 0.01f, 0f, (i + 1) * width - 0.01f, 1f);
                chip.raycastTarget = false;
                float textLeft = 0.04f;
                if (i == 0 && stage.StoneGems > 0)
                {
                    AddStoneIcon(chip.transform, alpha);
                    textLeft = 0.3f;
                }
                Text label = Ui.Label(chip.transform, text, fontSize, TextAnchor.MiddleCenter,
                    WithAlpha(color, alpha), textLeft, 0f, 0.96f, 1f);
                label.raycastTarget = false;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 16;
                label.resizeTextMaxSize = fontSize;
            }
        }

        /// <summary>A red square gem with its right half turned to stone, like on the board.</summary>
        private static void AddStoneIcon(Transform chip, float alpha)
        {
            // FitInParent fills its parent, so the square sits inside its own slot.
            RectTransform slot = Ui.Rect(chip, "Stone Icon Slot", 0.04f, 0.14f, 0.28f, 0.86f);
            RectTransform icon = Ui.Stretch(slot, "Stone Icon");
            var fitter = icon.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            Image gem = Ui.Box(icon, "Gem", WithAlpha(StoneGemColor, alpha));
            gem.sprite = GemShapes.Get(GemShape.Square);
            gem.raycastTarget = false;
            Image stone = Ui.Box(icon, "Stone", WithAlpha(Color.white, alpha));
            stone.sprite = GemShapes.GetStoneHalf(GemShape.Square);
            stone.raycastTarget = false;
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);
    }
}
