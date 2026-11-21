using System.Collections.Generic;
using PuzzleKingdoms.Logic.Flow;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// The expedition mode (PB-26, D-062): how many battles, how fast the enemies grow, what rests and rewards give.
    /// Select <c>Data/ExpeditionRules.asset</c> and edit the numbers in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Expedition Rules", fileName = "ExpeditionRules")]
    public sealed class ExpeditionRulesAsset : ScriptableObject
    {
        [Tooltip("Battles in a run; the last one is the boss.")]
        [SerializeField, Range(2, 8)] private int steps = 5;
        [Tooltip("Puzzle moves before the first battle.")]
        [SerializeField, Min(1)] private int firstMoves = 12;
        [Tooltip("Puzzle moves before every later battle (the army carries over, so fewer).")]
        [SerializeField, Min(1)] private int movesPerStep = 8;
        [Tooltip("Share of its HP a unit regains between battles (0.3 = 30 %).")]
        [SerializeField, Range(0f, 1f)] private float restHeal = 0.3f;
        [SerializeField, Min(0f)] private float enemyHpGrowth = 0.12f;
        [SerializeField, Min(0f)] private float enemyDamageGrowth = 0.08f;
        [Tooltip("Tier of the boss on the last battle.")]
        [SerializeField, Range(1, 3)] private int bossTier = 2;
        [SerializeField, Range(1, 3)] private int recruitAmount = 3;
        [SerializeField, Range(1, 3)] private int rallyAmount = 1;
        [SerializeField, Min(0)] private int goldPerStep = 60;
        [SerializeField, Min(0)] private int goldForWinning = 250;
        [Tooltip("Percent a perk adds to one class's HP, damage or attack speed for the rest of the run (PB-39).")]
        [SerializeField, Range(0, 100)] private int perkPercent = 20;
        [Tooltip("The enemies the battles pick from. Leave the boss out.")]
        [SerializeField] private List<UnitDefinition> enemyPool = new List<UnitDefinition>();
        [SerializeField] private UnitDefinition boss;
        [Tooltip("The classes a Recruit reward can offer (the same ones the puzzle recruits).")]
        [SerializeField] private List<UnitDefinition> playerClasses = new List<UnitDefinition>();

        /// <summary>The logic rules built from the asset.</summary>
        public ExpeditionRules Rules()
        {
            var rules = new ExpeditionRules
            {
                Steps = steps,
                FirstMoves = firstMoves,
                MovesPerStep = movesPerStep,
                RestHeal = restHeal,
                EnemyHpGrowth = enemyHpGrowth,
                EnemyDamageGrowth = enemyDamageGrowth,
                BossTier = bossTier,
                RecruitAmount = recruitAmount,
                RallyAmount = rallyAmount,
                GoldPerStep = goldPerStep,
                GoldForWinning = goldForWinning,
                PerkPercent = perkPercent,
                Boss = boss != null ? boss.Def : null,
            };
            foreach (UnitDefinition unit in enemyPool)
            {
                if (unit != null)
                {
                    rules.EnemyPool.Add(unit.Def);
                }
            }
            foreach (UnitDefinition unit in playerClasses)
            {
                if (unit != null)
                {
                    rules.PlayerClasses.Add(unit.Def);
                }
            }
            return rules;
        }
    }
}
