using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Balance
{
    /// <summary>One stage of a blueprint file (PB-42). Ranges are [min, max].</summary>
    public sealed class StagePlan
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        /// <summary>Asset file name without extension, e.g. "Stage_3_1".</summary>
        public string file { get; set; } = "";
        public string[] units { get; set; } = Array.Empty<string>();
        public string[] required { get; set; } = Array.Empty<string>();
        public string[] pool { get; set; } = Array.Empty<string>();
        public string boss { get; set; }
        public int[] enemies { get; set; } = { 5, 6 };
        public int[] tier2 { get; set; } = { 0, 0 };
        public int[] moves { get; set; } = { 20, 22 };
        public int stones { get; set; }
        /// <summary>0 defeat, 1 stones, 2 win within, 3 ice (StageGoalKind).</summary>
        public int goalKind { get; set; }
        public int goalTarget { get; set; }
        public string[] covers { get; set; } = Array.Empty<string>();
        public int bossStonesEvery { get; set; }
        public int bossStonesCount { get; set; }
        public int gold { get; set; } = 100;
        public float damage { get; set; } = 1f;
        /// <summary>Win-rate band in percent for the greedy Merge-all bot.</summary>
        public int[] band { get; set; } = { 80, 90 };
        /// <summary>Wanted average win time in seconds; among candidates in the band the nearest wins.</summary>
        public float winSeconds { get; set; } = 28f;
        public float[] map { get; set; } = { 0.5f, 0.5f };
        public TutorialPlan[] tutorial { get; set; } = Array.Empty<TutorialPlan>();
    }

    public sealed class TutorialPlan
    {
        public string id { get; set; } = "";
        public int trigger { get; set; }
        public string text { get; set; } = "";
        public string target { get; set; } = "";
    }

    public sealed class ChapterPlan
    {
        public int chapter { get; set; }
        /// <summary>Seeds tried per stage.</summary>
        public int candidates { get; set; } = 6;
        public StagePlan[] stages { get; set; } = Array.Empty<StagePlan>();
    }

    /// <summary>
    /// <c>--generate plan.json</c> (PB-42, D-073): for every stage of the plan, makes candidates with
    /// <see cref="StageGenerator"/>, tunes each with <see cref="StageTuner"/>, keeps the one in its band whose win
    /// time is nearest the wanted one, and writes it as a stage asset (with a stable guid) that the campaign lists.
    /// </summary>
    public static class Generate
    {
        public static int Run(string planPath, string dataFolder, AssetReader assets, int runs)
        {
            ChapterPlan plan = JsonSerializer.Deserialize<ChapterPlan>(File.ReadAllText(planPath));
            Dictionary<string, (UnitDef def, string guid)> units = UnitsById(dataFolder, assets);
            Console.WriteLine($"Generating chapter {plan.chapter}: {plan.stages.Length} stages, {plan.candidates} candidates each, {runs} runs per try");
            Console.WriteLine();
            Console.WriteLine("stage       seed  hp    win%  winTime  goal missed  reached  army");
            int missed = 0;
            foreach (StagePlan s in plan.stages)
            {
                StageBlueprint blueprint = Blueprint(plan.chapter, s, units);
                List<UnitDef> classes = s.units.Select(u => units[u].def).ToList();
                StageDef best = null;
                TuneResult bestResult = null;
                ulong bestSeed = 0;
                for (ulong seed = 1; seed <= (ulong)plan.candidates; seed++)
                {
                    StageDef stage = StageGenerator.Generate(blueprint, seed);
                    stage.EnemyHpMultiplier = 1.5f;
                    TuneResult result = StageTuner.Tune(stage, runSeed => new StageRun(stage, classes, assets.PuzzleRules,
                            assets.ArmyRules, assets.BattleRules, assets.ProgressionRules, new SeededRandom(runSeed)),
                        s.band[0] / 100.0, s.band[1] / 100.0, runs, 0.3f, 10f, 10);
                    if (best == null || Better(result, bestResult, s.winSeconds))
                    {
                        best = stage;
                        bestResult = result;
                        bestSeed = seed;
                    }
                }
                BalanceStats st = bestResult.Stats;
                Console.WriteLine($"{s.id,-11} {bestSeed,4}  {bestResult.HpMultiplier,4:0.00}  {100 * st.WinRate,4:0}  " +
                    $"{(st.Wins > 0 ? st.WinSeconds / st.Wins : 0),7:0.0}  {100.0 * st.GoalMissed / st.Runs,10:0}%  " +
                    $"{(bestResult.Reached ? "yes" : "NO"),7}  {string.Join(" ", best.Enemies.Select(e => e.Unit.Id + (e.Tier > 1 ? "*" : "")))}");
                missed += bestResult.Reached ? 0 : 1;
                WriteStage(dataFolder, s, best, units);
            }
            AddToCampaign(dataFolder, plan.stages);
            return missed == 0 ? 0 : 2;
        }

        /// <summary>In the band beats out of it; then the win time nearer the wanted one; then the HP nearer 1.5.</summary>
        private static bool Better(TuneResult candidate, TuneResult best, float wanted)
        {
            if (candidate.Reached != best.Reached)
            {
                return candidate.Reached;
            }
            double Time(TuneResult r) => r.Stats.Wins > 0 ? Math.Abs(r.Stats.WinSeconds / r.Stats.Wins - wanted) : 999;
            double a = Time(candidate) + Math.Abs(candidate.HpMultiplier - 1.5f) * 4;
            double b = Time(best) + Math.Abs(best.HpMultiplier - 1.5f) * 4;
            return a < b;
        }

        private static StageBlueprint Blueprint(int chapter, StagePlan s, Dictionary<string, (UnitDef def, string guid)> units)
        {
            var blueprint = new StageBlueprint
            {
                Id = s.id,
                DisplayName = s.name,
                Chapter = chapter,
                Boss = string.IsNullOrEmpty(s.boss) ? null : units[s.boss].def,
                MinEnemies = s.enemies[0],
                MaxEnemies = s.enemies[1],
                MinTier2 = s.tier2[0],
                MaxTier2 = s.tier2[1],
                MinMoves = s.moves[0],
                MaxMoves = s.moves[1],
                StoneGems = s.stones,
                Goal = new StageGoal { Kind = (StageGoalKind)s.goalKind, Target = s.goalTarget },
                Covers = s.covers.Length > 0 ? s.covers : null,
                BossStonesEvery = s.bossStonesEvery,
                BossStonesCount = s.bossStonesCount,
                GoldReward = s.gold,
                EnemyDamageMultiplier = s.damage,
            };
            blueprint.Required.AddRange(s.required.Select(id => units[id].def));
            blueprint.Pool.AddRange(s.pool.Select(id => units[id].def));
            return blueprint;
        }

        /// <summary>Every unit and enemy asset by id, with its guid; the same UnitDef objects the campaign uses where it uses them.</summary>
        private static Dictionary<string, (UnitDef, string)> UnitsById(string dataFolder, AssetReader assets)
        {
            var result = new Dictionary<string, (UnitDef, string)>();
            foreach (string folder in new[] { "Units", "Enemies" })
            {
                foreach (string path in Directory.GetFiles(Path.Combine(dataFolder, folder), "*.asset"))
                {
                    string guid = File.ReadAllLines(path + ".meta").First(l => l.StartsWith("guid: ")).Substring(6).Trim();
                    UnitDef def = assets.UnitByGuid(guid);
                    result[def.Id] = (def, guid);
                }
            }
            return result;
        }

        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Ref(string guid) => "{fileID: 11400000, guid: " + guid + ", type: 2}";

        /// <summary>A stable guid from the stage id, so writing the stage again keeps every reference to it.</summary>
        private static string GuidFor(string id)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("puzzle-kingdoms-stage:" + id));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        private static void WriteStage(string dataFolder, StagePlan s, StageDef stage, Dictionary<string, (UnitDef def, string guid)> units)
        {
            var text = new StringBuilder();
            text.Append("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n");
            text.Append("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n");
            text.Append("  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n");
            text.Append("  m_Script: {fileID: 11500000, guid: ae7d63e5b21a4772a577bc78c74fe2b9, type: 3}\n");
            text.Append($"  m_Name: {s.file}\n  m_EditorClassIdentifier: \n");
            text.Append($"  id: {stage.Id}\n  displayName: {stage.DisplayName}\n  chapter: {stage.Chapter}\n  units:\n");
            foreach (string unit in s.units)
            {
                text.Append("  - ").Append(Ref(units[unit].guid)).Append('\n');
            }
            text.Append($"  moves: {stage.Moves}\n  boardSeed: 0\n  enemies:\n");
            foreach (EnemySlot e in stage.Enemies)
            {
                text.Append($"  - unit: {Ref(units[e.Unit.Id].guid)}\n    tier: {e.Tier}\n    cell: {{x: {e.Cell.X}, y: {e.Cell.Y}}}\n");
            }
            text.Append($"  goldReward: {stage.GoldReward}\n  stoneGems: {stage.StoneGems}\n");
            text.Append($"  enemyHpMultiplier: {F(stage.EnemyHpMultiplier)}\n  enemyDamageMultiplier: {F(stage.EnemyDamageMultiplier)}\n");
            text.Append($"  goalKind: {(int)stage.Goal.Kind}\n  goalTarget: {stage.Goal.Target}\n  layout: []\n");
            if (s.covers.Length == 0)
            {
                text.Append("  covers: []\n");
            }
            else
            {
                text.Append("  covers:\n");
                foreach (string row in s.covers)
                {
                    text.Append("  - ").Append(row).Append('\n');
                }
            }
            text.Append($"  bossStonesEvery: {stage.BossStonesEvery}\n  bossStonesCount: {stage.BossStonesCount}\n");
            text.Append($"  mapPosition: {{x: {F(s.map[0])}, y: {F(s.map[1])}}}\n");
            if (s.tutorial.Length == 0)
            {
                text.Append("  tutorial: []\n");
            }
            else
            {
                text.Append("  tutorial:\n");
                foreach (TutorialPlan t in s.tutorial)
                {
                    text.Append($"  - id: {t.id}\n    trigger: {t.trigger}\n    text: '{t.text.Replace("'", "''")}'\n    target: {t.target}\n");
                }
            }
            string path = Path.Combine(dataFolder, "Stages", s.file + ".asset");
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            File.WriteAllText(path + ".meta", "fileFormatVersion: 2\nguid: " + GuidFor(stage.Id) + "\nNativeFormatImporter:\n" +
                "  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
                new UTF8Encoding(false));
        }

        /// <summary>Appends the stages the campaign list does not have yet, in plan order, after its last stage.</summary>
        private static void AddToCampaign(string dataFolder, StagePlan[] stages)
        {
            string path = Path.Combine(dataFolder, "GameContent.asset");
            List<string> lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n').ToList();
            int start = lines.IndexOf("  campaign:");
            int end = start + 1;
            while (end < lines.Count && lines[end].StartsWith("  - "))
            {
                end++;
            }
            foreach (StagePlan s in stages)
            {
                string line = "  - " + Ref(GuidFor(s.id));
                if (!lines.Contains(line))
                {
                    lines.Insert(end++, line);
                }
            }
            File.WriteAllText(path, string.Join("\n", lines), new UTF8Encoding(false));
        }
    }
}
