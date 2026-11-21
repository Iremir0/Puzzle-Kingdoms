using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>Reward numbers (docs/GDD.md §5, §6, §10).</summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Progression Rules", fileName = "ProgressionRules")]
    public sealed class ProgressionRulesAsset : ScriptableObject
    {
        [SerializeField, Min(0)] private int dismissGoldPerUnit = 5;
        [SerializeField, Min(0)] private int goldPerOverflowRallyPoint = 1;
        [Tooltip("Which campaign stage opens which game system; a system not listed is open from the start (PB-37, D-070).")]
        [SerializeField] private List<FeatureUnlock> featureUnlocks = FeatureUnlockRules.Default().Unlocks;

        [NonSerialized] private ProgressionRules _rules;

        public ProgressionRules Rules => _rules ??= Build();

        private ProgressionRules Build()
        {
            var unlocks = new FeatureUnlockRules();
            foreach (FeatureUnlock unlock in featureUnlocks)
            {
                unlocks.Add(unlock.Feature, unlock.StageId);
            }
            return new ProgressionRules
            {
                DismissGoldPerUnit = dismissGoldPerUnit,
                GoldPerOverflowRallyPoint = goldPerOverflowRallyPoint,
                Unlocks = unlocks,
            };
        }

        private void OnValidate() => _rules = null;
    }
}
