using System.Collections.Generic;
using PuzzleKingdoms.Logic.Stages;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// What the daily challenge is built from (PB-25, D-061): the enemies a day may pick, how many, how strong,
    /// how many moves and what it pays. Select <c>Data/DailyChallenge.asset</c> and edit it in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Daily Challenge", fileName = "DailyChallenge")]
    public sealed class DailyChallengeAsset : ScriptableObject
    {
        [Tooltip("The enemies a day may pick from. Leave the boss out.")]
        [SerializeField] private List<UnitDefinition> enemyPool = new List<UnitDefinition>();
        [SerializeField, Range(1, 9)] private int minEnemies = 4;
        [SerializeField, Range(1, 9)] private int maxEnemies = 7;
        [SerializeField, Range(1, 3)] private int maxTier = 2;
        [SerializeField, Min(1)] private int minMoves = 10;
        [SerializeField, Min(1)] private int maxMoves = 14;
        [SerializeField, Min(0)] private int maxStones = 4;
        [Tooltip("Gold for the first attempt of a day (win or lose).")]
        [SerializeField, Min(0)] private int goldReward = 120;
        [SerializeField, Min(0.1f)] private float enemyHpMultiplier = 1.2f;
        [SerializeField, Min(0.1f)] private float enemyDamageMultiplier = 1.1f;

        public int GoldReward => goldReward;

        /// <summary>The logic rules built from the asset.</summary>
        public DailyChallengeRules Rules()
        {
            var rules = new DailyChallengeRules
            {
                MinEnemies = minEnemies,
                MaxEnemies = Mathf.Max(minEnemies, maxEnemies),
                MaxTier = maxTier,
                MinMoves = minMoves,
                MaxMoves = Mathf.Max(minMoves, maxMoves),
                MaxStones = maxStones,
                GoldReward = goldReward,
                EnemyHpMultiplier = enemyHpMultiplier,
                EnemyDamageMultiplier = enemyDamageMultiplier,
            };
            foreach (UnitDefinition unit in enemyPool)
            {
                if (unit != null)
                {
                    rules.EnemyPool.Add(unit.Def);
                }
            }
            return rules;
        }
    }
}
