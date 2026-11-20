using System;
using PuzzleKingdoms.Logic.Combat;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>Battle and Rally numbers (docs/GDD.md §9). Distances in battlefield cells.</summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Battle Rules", fileName = "BattleRules")]
    public sealed class BattleRulesAsset : ScriptableObject
    {
        [SerializeField, Min(1)] private int ticksPerSecond = 20;
        [SerializeField, Min(1f)] private float timeoutSeconds = 60f;
        [Tooltip("Every hit and heal of both sides is multiplied by this: the knob for battle length (D-069).")]
        [SerializeField, Min(0.1f)] private float damageScale = 1f;
        [SerializeField, Min(0f)] private float frontGap = 6f;
        [SerializeField, Min(0f)] private float rowSpacing = 1f;
        [SerializeField, Min(0f)] private float laneSpacing = 1.2f;
        [SerializeField, Min(0f)] private float sameLaneTieDistance = 0.5f;
        [SerializeField, Min(0f)] private float separationDistance = 0.6f;
        [SerializeField, Min(0f)] private float rallyDamageBonus = 0.5f;
        [SerializeField, Min(0f)] private float rallyAttackSpeedBonus = 0.5f;
        [SerializeField, Min(0f)] private float rallyDurationSeconds = 5f;

        [NonSerialized] private BattleRules _rules;

        public BattleRules Rules => _rules ??= new BattleRules
        {
            TicksPerSecond = ticksPerSecond,
            TimeoutSeconds = timeoutSeconds,
            DamageScale = damageScale,
            FrontGap = frontGap,
            RowSpacing = rowSpacing,
            LaneSpacing = laneSpacing,
            SameLaneTieDistance = sameLaneTieDistance,
            SeparationDistance = separationDistance,
            RallyDamageBonus = rallyDamageBonus,
            RallyAttackSpeedBonus = rallyAttackSpeedBonus,
            RallyDurationSeconds = rallyDurationSeconds,
        };

        private void OnValidate() => _rules = null;
    }
}
