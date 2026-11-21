using System;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Logic.Flow
{
    /// <summary>What <see cref="StageTuner.Tune"/> found for one stage.</summary>
    public sealed class TuneResult
    {
        /// <summary>The enemy HP multiplier to write into the stage (the stage already has it when this returns).</summary>
        public float HpMultiplier;
        /// <summary>The bot's numbers at <see cref="HpMultiplier"/>.</summary>
        public BalanceStats Stats;
        /// <summary>False when no multiplier in the search range gives a win rate inside the band.</summary>
        public bool Reached;
        /// <summary>Multipliers tried.</summary>
        public int Tries;
    }

    /// <summary>
    /// The "solver" of PB-40 (D-073): plays a stage with the greedy puzzle bot that presses Merge all and never
    /// uses Rally (the plainest good player), and searches the enemy HP multiplier until its win rate lands in a
    /// band. More enemy HP never makes winning easier, so a bisection finds it in a few tries. Same seeds as
    /// <see cref="BalanceRunner"/>, so the numbers repeat.
    /// </summary>
    public static class StageTuner
    {
        /// <param name="stage">Changed in place: its <see cref="StageDef.EnemyHpMultiplier"/> ends at the result.</param>
        /// <param name="createRun">Makes the run of the stage for one seed.</param>
        /// <param name="minWin">Lowest win rate wanted, 0..1.</param>
        /// <param name="maxWin">Highest win rate wanted, 0..1.</param>
        public static TuneResult Tune(StageDef stage, Func<ulong, StageRun> createRun, double minWin, double maxWin,
            int runs, float low = 0.3f, float high = 8f, int maxTries = 12)
        {
            if (stage == null || createRun == null)
            {
                throw new ArgumentNullException(stage == null ? nameof(stage) : nameof(createRun));
            }
            if (minWin > maxWin || runs <= 0)
            {
                throw new ArgumentException("Need minWin <= maxWin and runs > 0");
            }
            var result = new TuneResult();
            TuneResult closest = null;
            double closestMiss = double.MaxValue;
            for (int i = 0; i < maxTries && low <= high; i++)
            {
                float multiplier = Round(i == 0 && stage.EnemyHpMultiplier >= low && stage.EnemyHpMultiplier <= high
                    ? stage.EnemyHpMultiplier
                    : (low + high) / 2f);
                stage.EnemyHpMultiplier = multiplier;
                BalanceStats stats = BalanceRunner.Run(createRun, PuzzleBot.Greedy, RallyBot.None, runs, MergeBot.All);
                result.Tries++;
                double win = stats.WinRate;
                double miss = win < minWin ? minWin - win : win > maxWin ? win - maxWin : 0;
                if (miss <= closestMiss) // on a tie the later try, nearer the end of the range, says more
                {
                    closestMiss = miss;
                    closest = new TuneResult { HpMultiplier = multiplier, Stats = stats, Reached = miss == 0 };
                }
                if (miss == 0)
                {
                    break;
                }
                if (win > maxWin)
                {
                    low = multiplier + 0.01f; // too easy: more HP
                }
                else
                {
                    high = multiplier - 0.01f; // too hard: less HP
                }
            }
            closest.Tries = result.Tries;
            stage.EnemyHpMultiplier = closest.HpMultiplier;
            return closest;
        }

        /// <summary>Two decimals, as the stage assets are written by hand.</summary>
        private static float Round(float value) => (float)Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
