using System;
using PuzzleKingdoms.Logic.Puzzle;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>Puzzle phase numbers (docs/GDD.md §5).</summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Puzzle Rules", fileName = "PuzzleRules")]
    public sealed class PuzzleRulesAsset : ScriptableObject
    {
        [SerializeField, Range(4, 9)] private int boardWidth = 6;
        [SerializeField, Range(4, 9)] private int boardHeight = 6;
        [SerializeField, Min(1)] private int pointsPerUnit = 3;
        [SerializeField, Min(1)] private int pointsPerRallyCharge = 3;
        [SerializeField, Min(0)] private int maxRallyCharges = 3;

        [NonSerialized] private PuzzleRules _rules;

        public PuzzleRules Rules => _rules ??= new PuzzleRules
        {
            BoardWidth = boardWidth,
            BoardHeight = boardHeight,
            PointsPerUnit = pointsPerUnit,
            PointsPerRallyCharge = pointsPerRallyCharge,
            MaxRallyCharges = maxRallyCharges,
        };

        private void OnValidate() => _rules = null;
    }
}
