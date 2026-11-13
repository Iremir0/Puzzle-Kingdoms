using System;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// Converts cleared gems into rewards: every <see cref="PointsPerReward"/>
    /// points grant one reward (a unit, a Rally charge, ...). Remainders carry
    /// over. Once <see cref="MaxRewards"/> is reached, points go to
    /// <see cref="OverflowPoints"/>.
    /// </summary>
    public sealed class RecruitmentMeter
    {
        public const int Unlimited = -1;

        public readonly int PointsPerReward;
        public readonly int MaxRewards;
        public int Points { get; private set; }
        public int Rewards { get; private set; }
        public int OverflowPoints { get; private set; }

        public RecruitmentMeter(int pointsPerReward = 3, int maxRewards = Unlimited)
        {
            if (pointsPerReward <= 0)
            {
                throw new ArgumentException("pointsPerReward must be positive");
            }
            PointsPerReward = pointsPerReward;
            MaxRewards = maxRewards;
        }

        public bool IsFull => MaxRewards != Unlimited && Rewards >= MaxRewards;

        /// <summary>Progress towards the next reward in [0, 1).</summary>
        public float Progress => (float)Points / PointsPerReward;

        /// <summary>Adds points and returns how many new rewards were earned.</summary>
        public int AddPoints(int amount)
        {
            int earned = 0;
            for (int i = 0; i < amount; i++)
            {
                if (IsFull)
                {
                    OverflowPoints++;
                    continue;
                }
                Points++;
                if (Points >= PointsPerReward)
                {
                    Points -= PointsPerReward;
                    Rewards++;
                    earned++;
                }
            }
            return earned;
        }
    }
}
