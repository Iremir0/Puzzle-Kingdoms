using System;
using System.Globalization;
using System.IO;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Kingdom;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Balance
{
    /// <summary>
    /// Plays every campaign stage many times with simple bot players and prints
    /// win rate, battle length and army size, to tune the data assets against
    /// the GDD targets (docs/GDD.md §2, §9; targets in D-026). The bots live in
    /// Logic (<see cref="BalanceRunner"/>), so the Stage Editor's "Test difficulty"
    /// gives the same numbers (PB-19).
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            int runs = args.Length > 0 && !args[0].StartsWith("--") ? int.Parse(args[0], CultureInfo.InvariantCulture) : 300;
            // PB-22: "--kingdom N" gives every building level N, to check a stage at the level a player is expected to have.
            int kingdomLevel = 0;
            for (int a = 0; a + 1 < args.Length; a++)
            {
                if (args[a] == "--kingdom")
                {
                    kingdomLevel = int.Parse(args[a + 1], CultureInfo.InvariantCulture);
                }
            }
            string data = FindDataFolder();
            var assets = new AssetReader(data);
            Console.WriteLine($"Data: {data}   runs per row: {runs}   kingdom level: {kingdomLevel}");
            KingdomBonuses bonuses = kingdomLevel > 0 ? assets.KingdomRules.BonusesAtLevel(kingdomLevel) : null;
            Console.WriteLine();
            Console.WriteLine("stage      puzzle  rally  win%  time(s) winTime  recruit deploy rally  surv%  stars 0/1/2/3  goal");
            foreach (StageDef stage in assets.Campaign)
            {
                foreach (PuzzleBot puzzleBot in new[] { PuzzleBot.Random, PuzzleBot.Greedy })
                {
                    foreach (RallyBot rallyBot in new[] { RallyBot.None, RallyBot.Eager })
                    {
                        BalanceStats s = BalanceRunner.Run(seed => new StageRun(stage, assets.Recruitable,
                                assets.PuzzleRules, assets.ArmyRules, assets.BattleRules, assets.ProgressionRules,
                                new SeededRandom(seed), bonuses),
                            puzzleBot, rallyBot, runs);
                        string goal = stage.Goal.Kind == StageGoalKind.DefeatArmy
                            ? ""
                            : $"{stage.Goal.Kind} {stage.Goal.Target}: missed {100.0 * s.GoalMissed / s.Runs:0}%";
                        Console.WriteLine(
                            $"{stage.Id,-10} {puzzleBot,-7} {rallyBot,-6} {100.0 * s.WinRate,4:0}  " +
                            $"{s.AverageSeconds,6:0.0}  {(s.Wins > 0 ? s.WinSeconds / s.Wins : 0),6:0.0}  " +
                            $"{s.Recruited / s.Runs,6:0.0} {s.Deployed / s.Runs,6:0.0} {s.Rally / s.Runs,5:0.0}  " +
                            $"{100.0 * s.SurvivorShare / s.Runs,5:0}  {string.Join("/", s.Stars)}  {goal}");
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
    }
}
