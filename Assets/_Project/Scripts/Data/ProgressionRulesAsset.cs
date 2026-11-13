using System;
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

        [NonSerialized] private ProgressionRules _rules;

        public ProgressionRules Rules => _rules ??= new ProgressionRules
        {
            DismissGoldPerUnit = dismissGoldPerUnit,
            GoldPerOverflowRallyPoint = goldPerOverflowRallyPoint,
        };

        private void OnValidate() => _rules = null;
    }
}
