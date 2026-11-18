using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Puzzle;

namespace PuzzleKingdoms.Logic.Flow
{
    /// <summary>How the balance bot plays the puzzle (D-026).</summary>
    public enum PuzzleBot
    {
        /// <summary>Any valid swap.</summary>
        Random,
        /// <summary>The swap that clears the most gems right away (cascades ignored).</summary>
        Greedy,
    }

    /// <summary>How the balance bot uses Rally.</summary>
    public enum RallyBot
    {
        None,
        /// <summary>Fires every charge as soon as it can.</summary>
        Eager,
    }

    /// <summary>Totals of many bot runs of one stage.</summary>
    public sealed class BalanceStats
    {
        public int Runs;
        /// <summary>Stages won: battle won and goal reached (PB-19).</summary>
        public int Wins;
        /// <summary>Battles won but the stage goal missed.</summary>
        public int GoalMissed;
        public double Seconds;
        public double WinSeconds;
        public double Recruited;
        public double Deployed;
        public double Rally;
        public double StonesBroken;
        public double SurvivorShare;
        public readonly int[] Stars = new int[4];

        public double WinRate => Runs == 0 ? 0 : (double)Wins / Runs;

        public double AverageStars => Runs == 0 ? 0 : (Stars[1] + 2.0 * Stars[2] + 3.0 * Stars[3]) / Runs;

        public double AverageSeconds => Runs == 0 ? 0 : Seconds / Runs;
    }

    /// <summary>
    /// Plays a stage many times with simple bots and adds up the outcome (D-026, PB-19). Shared by
    /// <c>Tools/Balance</c> (all stages from a terminal) and the Stage Editor's "Test difficulty".
    /// The bots: a random or greedy puzzle player, <see cref="AutoFormation"/> in the War Camp (no
    /// merges), Rally never or as soon as possible. Same seeds, same numbers.
    /// </summary>
    public static class BalanceRunner
    {
        public const ulong FirstSeed = 1000;

        /// <param name="createRun">Makes the run for one seed (the stage and rules are the caller's).</param>
        public static BalanceStats Run(Func<ulong, StageRun> createRun, PuzzleBot puzzleBot, RallyBot rallyBot, int runs)
        {
            var stats = new BalanceStats();
            for (int i = 0; i < runs; i++)
            {
                ulong seed = FirstSeed + (ulong)i;
                StageRun run = createRun(seed);
                var botRandom = new SeededRandom(seed * 7919UL + 13UL);
                while (!run.Puzzle.IsFinished)
                {
                    (GridPos a, GridPos b) = puzzleBot == PuzzleBot.Random
                        ? RandomMove(run.Puzzle.Board, botRandom)
                        : GreedyMove(run.Puzzle.Board);
                    run.Puzzle.PlaySwap(a, b);
                }
                int recruited = run.Roster.Bench.Count;
                AutoFormation.Fill(run.Roster);
                stats.Runs++;
                stats.Recruited += recruited;
                stats.Rally += run.RallyCharges;
                stats.StonesBroken += run.Puzzle.StonesBroken;
                if (!run.StartBattle())
                {
                    stats.Stars[0]++; // nobody to deploy: a loss
                    continue;
                }
                BattleSimulator battle = run.Battle;
                while (!battle.IsFinished)
                {
                    if (rallyBot == RallyBot.Eager && battle.Rally.CanActivate)
                    {
                        battle.ActivateRally();
                    }
                    battle.Step();
                }
                BattleResult result = battle.Result;
                int stars = run.Finish().Stars;
                float seconds = (float)result.Ticks / battle.Rules.TicksPerSecond;
                stats.Seconds += seconds;
                stats.Deployed += result.Deployed;
                stats.SurvivorShare += result.Deployed > 0 ? (double)result.Survivors / result.Deployed : 0;
                stats.Stars[stars]++;
                if (run.Won)
                {
                    stats.Wins++;
                    stats.WinSeconds += seconds;
                }
                else if (result.Victory)
                {
                    stats.GoalMissed++;
                }
            }
            return stats;
        }

        /// <summary>Every swap that would make a match.</summary>
        public static List<(GridPos, GridPos)> ValidMoves(BoardModel board)
        {
            var moves = new List<(GridPos, GridPos)>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var a = new GridPos(x, y);
                    foreach (GridPos b in new[] { a + GridPos.Right, a + GridPos.Down })
                    {
                        if (board.IsInside(b) && board.WouldSwapMatch(a, b))
                        {
                            moves.Add((a, b));
                        }
                    }
                }
            }
            return moves;
        }

        private static (GridPos, GridPos) RandomMove(BoardModel board, IRandom random)
        {
            List<(GridPos, GridPos)> moves = ValidMoves(board);
            return moves[random.Range(0, moves.Count)];
        }

        /// <summary>Picks the swap that clears the most gems right away (cascades ignored).</summary>
        private static (GridPos, GridPos) GreedyMove(BoardModel board)
        {
            int[,] grid = board.ToGrid();
            SpecialKind[,] specials = board.ToSpecialGrid();
            bool[,] stones = board.ToStoneGrid();
            int[][] rows = Enumerable.Range(0, board.Height)
                .Select(y => Enumerable.Range(0, board.Width).Select(x => grid[x, y]).ToArray())
                .ToArray();
            (GridPos, GridPos) best = default;
            int bestCleared = -1;
            foreach ((GridPos a, GridPos b) in ValidMoves(board))
            {
                var probe = new BoardModel(board.Width, board.Height, board.GemTypeCount, new SeededRandom(1));
                probe.LoadRows(rows);
                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        if (specials[x, y] != SpecialKind.None)
                        {
                            probe.SetSpecial(new GridPos(x, y), specials[x, y]);
                        }
                        if (stones[x, y])
                        {
                            probe.SetStone(new GridPos(x, y), true);
                        }
                    }
                }
                MoveResult result = probe.TrySwap(a, b);
                int cleared = result.IsValid ? result.Steps[0].Cleared.Count : 0;
                if (cleared > bestCleared)
                {
                    bestCleared = cleared;
                    best = (a, b);
                }
            }
            return best;
        }
    }
}
