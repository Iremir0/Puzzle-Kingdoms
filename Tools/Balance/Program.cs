using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Balance
{
    /// <summary>
    /// Plays every campaign stage many times with simple bot players and prints
    /// win rate, battle length and army size, to tune the data assets against
    /// the GDD targets (docs/GDD.md §2, §9; targets in D-026).
    /// </summary>
    public static class Program
    {
        private enum PuzzleBot { Random, Greedy }
        private enum RallyBot { None, Eager }

        private sealed class Stats
        {
            public int Runs;
            public int Wins;
            public double Seconds;
            public double WinSeconds;
            public double Recruited;
            public double Deployed;
            public double Rally;
            public double SurvivorShare;
            public readonly int[] Stars = new int[4];
        }

        public static int Main(string[] args)
        {
            int runs = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 300;
            string data = FindDataFolder();
            var assets = new AssetReader(data);
            Console.WriteLine($"Data: {data}   runs per row: {runs}");
            Console.WriteLine();
            Console.WriteLine("stage      puzzle  rally  win%  time(s) winTime  recruit deploy rally  surv%  stars 0/1/2/3");
            foreach (StageDef stage in assets.Campaign)
            {
                foreach (PuzzleBot puzzleBot in new[] { PuzzleBot.Random, PuzzleBot.Greedy })
                {
                    foreach (RallyBot rallyBot in new[] { RallyBot.None, RallyBot.Eager })
                    {
                        Stats s = Run(assets, stage, puzzleBot, rallyBot, runs);
                        Console.WriteLine(
                            $"{stage.Id,-10} {puzzleBot,-7} {rallyBot,-6} {100.0 * s.Wins / s.Runs,4:0}  " +
                            $"{s.Seconds / s.Runs,6:0.0}  {(s.Wins > 0 ? s.WinSeconds / s.Wins : 0),6:0.0}  " +
                            $"{s.Recruited / s.Runs,6:0.0} {s.Deployed / s.Runs,6:0.0} {s.Rally / s.Runs,5:0.0}  " +
                            $"{100.0 * s.SurvivorShare / s.Runs,5:0}  {string.Join("/", s.Stars)}");
                    }
                }
            }
            return 0;
        }

        private static string FindDataFolder()
        {
            string dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "Assets", "_Project", "Data");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("Run from inside the repository.");
        }

        private static Stats Run(AssetReader assets, StageDef stage, PuzzleBot puzzleBot, RallyBot rallyBot, int runs)
        {
            var stats = new Stats();
            for (int i = 0; i < runs; i++)
            {
                ulong seed = (ulong)(1000 + i);
                var run = new StageRun(stage, assets.Recruitable, assets.PuzzleRules, assets.ArmyRules,
                    assets.BattleRules, assets.ProgressionRules, new SeededRandom(seed));
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
                run.StartBattle();
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
                float seconds = (float)result.Ticks / assets.BattleRules.TicksPerSecond;
                stats.Runs++;
                stats.Seconds += seconds;
                stats.Recruited += recruited;
                stats.Deployed += result.Deployed;
                stats.Rally += run.RallyCharges;
                stats.SurvivorShare += result.Deployed > 0 ? (double)result.Survivors / result.Deployed : 0;
                stats.Stars[result.Stars]++;
                if (result.Victory)
                {
                    stats.Wins++;
                    stats.WinSeconds += seconds;
                }
            }
            return stats;
        }

        private static List<(GridPos, GridPos)> ValidMoves(BoardModel board)
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
