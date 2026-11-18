using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>
    /// What one stage attempt pays out (D-022). Dismissal and Banner overflow
    /// gold are earned during the attempt, so they are paid on defeat too;
    /// the stage's gold reward and stars only on victory.
    /// </summary>
    public sealed class StageReward
    {
        public int Stars;
        public int VictoryGold;
        public int DismissGold;
        public int OverflowGold;

        public int TotalGold => VictoryGold + DismissGold + OverflowGold;

        public static StageReward Compute(
            StageDef stage,
            BattleResult result,
            int dismissedUnits,
            int rallyOverflowPoints,
            ProgressionRules rules,
            bool goalMet = true)
        {
            // A won battle that missed the stage goal (PB-19) pays like a defeat: no stars, no victory gold.
            bool won = result.Victory && goalMet;
            return new StageReward
            {
                Stars = won ? result.Stars : 0,
                VictoryGold = won ? stage.GoldReward : 0,
                DismissGold = dismissedUnits * rules.DismissGoldPerUnit,
                OverflowGold = rallyOverflowPoints * rules.GoldPerOverflowRallyPoint,
            };
        }
    }
}
