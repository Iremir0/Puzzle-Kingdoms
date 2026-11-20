using System;
using System.Globalization;
using System.IO;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Combat;
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
            string commanderId = "";
            MergeBot mergeBot = MergeBot.None;
            float[] tiers = null;
            float damageScale = 0f;
            string only = null;
            var sets = new System.Collections.Generic.List<string>();
            for (int a = 0; a + 1 < args.Length; a++)
            {
                if (args[a] == "--kingdom")
                {
                    kingdomLevel = int.Parse(args[a + 1], CultureInfo.InvariantCulture);
                }
                else if (args[a] == "--commander")
                {
                    commanderId = args[a + 1]; // PB-23: lead the army with this commander (every stage, unlocked or not)
                }
                else if (args[a] == "--merge")
                {
                    mergeBot = args[a + 1] == "all" ? MergeBot.All : MergeBot.None; // PB-34: "Merge all" before the formation
                }
                else if (args[a] == "--damage")
                {
                    damageScale = float.Parse(args[a + 1], CultureInfo.InvariantCulture); // PB-35: try a battle pace
                }
                else if (args[a] == "--only")
                {
                    only = "," + args[a + 1] + ","; // PB-35: just these stages, e.g. "--only stage_2_2,stage_2_8"
                }
                else if (args[a] == "--set")
                {
                    sets.Add(args[a + 1]); // PB-35: try a stage value, e.g. "--set stage_2_3.hp=1.6" (hp, damage, moves, goal)
                }
                else if (args[a] == "--tiers")
                {
                    // PB-34: try other tier multipliers without editing the asset, e.g. "--tiers 1,3.5,11"
                    tiers = Array.ConvertAll(args[a + 1].Split(','), t => float.Parse(t, CultureInfo.InvariantCulture));
                }
            }
            string data = FindDataFolder();
            var assets = new AssetReader(data);
            if (tiers != null)
            {
                assets.ArmyRules.TierMultipliers = tiers;
            }
            if (damageScale > 0f)
            {
                assets.BattleRules.DamageScale = damageScale;
            }
            Console.WriteLine($"Data: {data}   runs per row: {runs}   kingdom level: {kingdomLevel}   commander: {(commanderId == "" ? "none" : commanderId)}   merge: {mergeBot}   damage: {assets.BattleRules.DamageScale}   tiers: {string.Join("/", assets.ArmyRules.TierMultipliers)}");
            foreach (string set in sets)
            {
                string[] sides = set.Split('=');
                string[] key = sides[0].Split('.');
                StageDef target = assets.Campaign.Find(st => st.Id == key[0]);
                float value = float.Parse(sides[1], CultureInfo.InvariantCulture);
                switch (key[1])
                {
                    case "hp": target.EnemyHpMultiplier = value; break;
                    case "damage": target.EnemyDamageMultiplier = value; break;
                    case "moves": target.Moves = (int)value; break;
                    case "goal": target.Goal.Target = (int)value; break;
                    default: throw new ArgumentException("Unknown stage value: " + set);
                }
                Console.WriteLine("set " + set);
            }
            KingdomBonuses bonuses = kingdomLevel > 0 ? assets.KingdomRules.BonusesAtLevel(kingdomLevel) : null;
            RallyVariant variant = commanderId == "" ? null : assets.Commanders.Find(c => c.Id == commanderId)?.Variant;
            if (commanderId != "" && variant == null)
            {
                Console.WriteLine("Unknown commander: " + commanderId);
                return 1;
            }
            Console.WriteLine();
            Console.WriteLine("stage      puzzle  rally  win%  time(s) winTime  recruit deploy rally  surv%  stars 0/1/2/3  goal");
            foreach (StageDef stage in assets.Campaign)
            {
                if (only != null && !only.Contains("," + stage.Id + ","))
                {
                    continue;
                }
                foreach (PuzzleBot puzzleBot in new[] { PuzzleBot.Random, PuzzleBot.Greedy })
                {
                    foreach (RallyBot rallyBot in new[] { RallyBot.None, RallyBot.Eager })
                    {
                        BalanceStats s = BalanceRunner.Run(seed => new StageRun(stage, assets.UnitsFor(stage),
                                assets.PuzzleRules, assets.ArmyRules, assets.BattleRules, assets.ProgressionRules,
                                new SeededRandom(seed), bonuses, variant),
                            puzzleBot, rallyBot, runs, mergeBot);
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
