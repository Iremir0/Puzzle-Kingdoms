using System;
using PuzzleKingdoms.Logic.Army;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>War Camp balancing numbers (docs/GDD.md §6).</summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Army Rules", fileName = "ArmyRules")]
    public sealed class ArmyRulesAsset : ScriptableObject
    {
        [Tooltip("HP and damage multiplier per tier; element 0 is tier 1. Length = max tier.")]
        [SerializeField] private float[] tierMultipliers = { 1f, 4f, 13f };
        [Tooltip("The same for enemy units (Expedition and Daily enemies come in higher tiers). D-068.")]
        [SerializeField] private float[] enemyTierMultipliers = { 1f, 1.8f, 3.2f };
        [SerializeField, Min(2)] private int mergeCount = 3;

        [NonSerialized] private ArmyRules _rules;

        public ArmyRules Rules => _rules ??= new ArmyRules
        {
            TierMultipliers = (float[])tierMultipliers.Clone(),
            EnemyTierMultipliers = (float[])enemyTierMultipliers.Clone(),
            MergeCount = mergeCount,
        };

        private void OnValidate() => _rules = null;
    }
}
