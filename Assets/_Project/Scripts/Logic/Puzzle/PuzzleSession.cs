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

        /// <summary>Stones broken so far (each crack frees one gem; PB-19 stone goals count them).</summary>
        public int StonesBroken { get; private set; }

        private readonly int _fourMatchBonusPoints;

        /// <param name="fourMatchBonusPoints">Extra points a plain line of 4 adds to its gem's meter (D-028).</param>
        public PuzzleSession(
            BoardModel board,
            int moves,
            IReadOnlyList<string> gemRewardKeys,
            IReadOnlyDictionary<string, RecruitmentMeter> meters,
            int fourMatchBonusPoints = 0)
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
            _fourMatchBonusPoints = fourMatchBonusPoints;
            MovesLeft = moves;
            GemRewardKeys = gemRewardKeys;
            Meters = meters;
        }

        public bool IsFinished => MovesLeft <= 0;

        /// <summary>
        /// Gives more moves, also after the puzzle ended (a rewarded ad, PB-30). Fires <see cref="MovesChanged"/>;
        /// <see cref="Finished"/> fires again when the new moves run out.
        /// </summary>
        public void AddMoves(int moves)
        {
            if (moves <= 0)
            {
                return;
            }
            MovesLeft += moves;
            MovesChanged?.Invoke(MovesLeft);
        }

        // ---- Boss board effect and ice (PB-20, D-050) ----

        /// <summary>The boss drops stones every this many moves; 0 = no boss effect.</summary>
        public int BossStonesEvery { get; private set; }

        /// <summary>How many stones the boss drops each time.</summary>
        public int BossStonesCount { get; private set; }

        /// <summary>Valid moves played so far.</summary>
        public int MovesPlayed { get; private set; }

        /// <summary>Ice layers on the board when the puzzle began (for the "break the ice" goal).</summary>
        public int IceAtStart { get; private set; }

        /// <summary>Turns on the boss effect: after every <paramref name="every"/> moves, <paramref name="count"/> stones drop.</summary>
        public void SetBossStones(int every, int count)
        {
            BossStonesEvery = Math.Max(0, every);
            BossStonesCount = Math.Max(0, count);
        }

        /// <summary>Remembers the ice on the board now as the starting amount (call once the board is set up).</summary>
        public void MarkIceAtStart() => IceAtStart = Board.IceLeft;

        /// <summary>Moves until the boss drops stones again, or -1 without a boss effect.</summary>
        public int MovesUntilBossStones =>
            BossStonesEvery > 0 && BossStonesCount > 0 ? BossStonesEvery - MovesPlayed % BossStonesEvery : -1;

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
            MovesPlayed++;
            foreach (MoveResult.CascadeStep step in result.Steps)
            {
                StonesBroken += step.Cracked.Count;
            }
            if (BossStonesEvery > 0 && BossStonesCount > 0 && MovesPlayed % BossStonesEvery == 0 && !IsFinished)
            {
                // PB-20: the stage boss throws stones onto the board after the move has settled.
                result.StonesDropped.AddRange(Board.DropStones(BossStonesCount));
            }
            // Cleared gems score 1 point each; a plain line of 4 adds a bonus on top.
            var points = new Dictionary<int, int>(result.ClearedByType);
            foreach (KeyValuePair<int, int> four in result.FourMatchesByType)
            {
                points.TryGetValue(four.Key, out int current);
                points[four.Key] = current + four.Value * _fourMatchBonusPoints;
            }
            foreach (KeyValuePair<int, int> cleared in points)
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
