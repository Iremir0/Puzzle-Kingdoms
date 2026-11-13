using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// One stage's puzzle phase: a board, a move budget and one
    /// <see cref="RecruitmentMeter"/> per reward key ("warrior", "rally", ...).
    /// <c>gemRewardKeys[gemType]</c> names the meter that gem type feeds, so the
    /// board itself stays generic.
    /// </summary>
    public sealed class PuzzleSession
    {
        public event Action<string, int> RewardsEarned;
        public event Action<int> MovesChanged;
        public event Action Finished;

        public readonly BoardModel Board;
        public readonly IReadOnlyList<string> GemRewardKeys;
        public readonly IReadOnlyDictionary<string, RecruitmentMeter> Meters;
        public int MovesLeft { get; private set; }

        public PuzzleSession(
            BoardModel board,
            int moves,
            IReadOnlyList<string> gemRewardKeys,
            IReadOnlyDictionary<string, RecruitmentMeter> meters)
        {
            if (gemRewardKeys.Count != board.GemTypeCount)
            {
                throw new ArgumentException("Every gem type needs a reward key");
            }
            foreach (string key in gemRewardKeys)
            {
                if (!meters.ContainsKey(key))
                {
                    throw new ArgumentException($"Missing meter for reward key '{key}'");
                }
            }
            Board = board;
            MovesLeft = moves;
            GemRewardKeys = gemRewardKeys;
            Meters = meters;
        }

        public bool IsFinished => MovesLeft <= 0;

        /// <summary>Plays a swap. Invalid swaps return an invalid result and cost no move.</summary>
        public MoveResult PlaySwap(GridPos a, GridPos b)
        {
            if (IsFinished)
            {
                return new MoveResult { SwapA = a, SwapB = b };
            }
            MoveResult result = Board.TrySwap(a, b);
            if (!result.IsValid)
            {
                return result;
            }

            MovesLeft--;
            foreach (KeyValuePair<int, int> cleared in result.ClearedByType)
            {
                string key = GemRewardKeys[cleared.Key];
                int earned = Meters[key].AddPoints(cleared.Value);
                if (earned > 0)
                {
                    RewardsEarned?.Invoke(key, earned);
                }
            }
            MovesChanged?.Invoke(MovesLeft);
            if (IsFinished)
            {
                Finished?.Invoke();
            }
            return result;
        }

        public int RewardCount(string rewardKey) => Meters[rewardKey].Rewards;
    }
}
