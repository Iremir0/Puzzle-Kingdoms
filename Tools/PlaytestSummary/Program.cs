using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PuzzleKingdoms.Tools.PlaytestSummary
{
    /// <summary>
    /// Reads playtest logs and prints Markdown: one table per stage over all
    /// testers, one row per tester (file) with wins/tries per stage.
    /// A file is one tester; its name (without extension) is the tester label.
    /// </summary>
    public static class Program
    {
        private sealed class Attempt
        {
            public string Tester;
            public string Stage;
            public bool Victory;
            public int Stars;
            public double PuzzleSeconds;
            public double CampSeconds;
            public double BattleSeconds;
            public int Recruited;
            public int Merges;
            public int Deployed;
            public int RallyEarned;
            public int RallyUsed;
        }

        public static int Main(string[] args)
        {
            List<string> files = ExpandArgs(args);
            if (files.Count == 0)
            {
                Console.Error.WriteLine("Usage: dotnet run --project Tools/PlaytestSummary -- <log.jsonl> [more.jsonl ...]");
                return 1;
            }

            var attempts = new List<Attempt>();
            foreach (string file in files)
            {
                string tester = Path.GetFileNameWithoutExtension(file);
                int lineNo = 0;
                foreach (string line in File.ReadLines(file))
                {
                    lineNo++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    try
                    {
                        attempts.Add(Parse(tester, line));
                    }
                    catch (Exception e) when (e is JsonException || e is KeyNotFoundException || e is InvalidOperationException)
                    {
                        Console.Error.WriteLine($"{file}:{lineNo}: skipped ({e.Message})");
                    }
                }
            }

            Console.WriteLine($"Playtest summary: {files.Count} log file(s), {attempts.Count} attempt(s)");
            Console.WriteLine();
            PrintStages(attempts);
            Console.WriteLine();
            PrintTesters(attempts);
            return 0;
        }

        private static List<string> ExpandArgs(string[] args)
        {
            // Shells on Windows do not expand wildcards, so do it here.
            var files = new List<string>();
            foreach (string arg in args)
            {
                string dir = Path.GetDirectoryName(arg);
                string pattern = Path.GetFileName(arg);
                if (pattern.Contains('*') || pattern.Contains('?'))
                {
                    string folder = string.IsNullOrEmpty(dir) ? "." : dir;
                    if (Directory.Exists(folder))
                    {
                        files.AddRange(Directory.GetFiles(folder, pattern).OrderBy(f => f, StringComparer.Ordinal));
                    }
                }
                else if (File.Exists(arg))
                {
                    files.Add(arg);
                }
                else
                {
                    Console.Error.WriteLine($"Not found: {arg}");
                }
            }
            return files;
        }

        private static Attempt Parse(string tester, string line)
        {
            using JsonDocument doc = JsonDocument.Parse(line);
            JsonElement r = doc.RootElement;
            return new Attempt
            {
                Tester = tester,
                Stage = r.GetProperty("stage").GetString(),
                Victory = r.GetProperty("victory").GetBoolean(),
                Stars = r.GetProperty("stars").GetInt32(),
                PuzzleSeconds = r.GetProperty("puzzleSeconds").GetDouble(),
                CampSeconds = r.GetProperty("campSeconds").GetDouble(),
                BattleSeconds = r.GetProperty("battleSeconds").GetDouble(),
                Recruited = r.GetProperty("recruited").GetInt32(),
                Merges = r.GetProperty("merges").GetInt32(),
                Deployed = r.GetProperty("deployed").GetInt32(),
                RallyEarned = r.GetProperty("rallyEarned").GetInt32(),
                RallyUsed = r.GetProperty("rallyUsed").GetInt32(),
            };
        }

        private static void PrintStages(List<Attempt> attempts)
        {
            Console.WriteLine("| Stage | Testers | Tries | Wins | Win % | Won by testers | Puzzle s | Camp s | Battle s | Stage min | Recruited | Merges | Rally used/earned |");
            Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (IGrouping<string, Attempt> stage in attempts.GroupBy(a => a.Stage).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                int testers = stage.Select(a => a.Tester).Distinct().Count();
                int wonBy = stage.Where(a => a.Victory).Select(a => a.Tester).Distinct().Count();
                int tries = stage.Count();
                int wins = stage.Count(a => a.Victory);
                double minutes = stage.Average(a => a.PuzzleSeconds + a.CampSeconds + a.BattleSeconds) / 60.0;
                Console.WriteLine(string.Join(" | ", new[]
                {
                    "| " + stage.Key,
                    testers.ToString(CultureInfo.InvariantCulture),
                    tries.ToString(CultureInfo.InvariantCulture),
                    wins.ToString(CultureInfo.InvariantCulture),
                    F(100.0 * wins / tries, "0"),
                    $"{wonBy}/{testers}",
                    F(stage.Average(a => a.PuzzleSeconds), "0"),
                    F(stage.Average(a => a.CampSeconds), "0"),
                    F(stage.Average(a => a.BattleSeconds), "0"),
                    F(minutes, "0.0"),
                    F(stage.Average(a => a.Recruited), "0.0"),
                    F(stage.Average(a => a.Merges), "0.0"),
                    $"{stage.Sum(a => a.RallyUsed)}/{stage.Sum(a => a.RallyEarned)} |",
                }));
            }
        }

        private static void PrintTesters(List<Attempt> attempts)
        {
            List<string> stages = attempts.Select(a => a.Stage).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
            Console.WriteLine("| Tester | " + string.Join(" | ", stages.Select(s => s + " won/tries")) + " | Best stars | Total merges | Rally used/earned |");
            Console.WriteLine("|---|" + string.Concat(stages.Select(_ => "---|")) + "---|---|---|");
            foreach (IGrouping<string, Attempt> tester in attempts.GroupBy(a => a.Tester).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                IEnumerable<string> perStage = stages.Select(s =>
                {
                    List<Attempt> mine = tester.Where(a => a.Stage == s).ToList();
                    return mine.Count == 0 ? "-" : $"{mine.Count(a => a.Victory)}/{mine.Count}";
                });
                IEnumerable<string> best = stages.Select(s =>
                    tester.Where(a => a.Stage == s).Select(a => a.Stars).DefaultIfEmpty(0).Max().ToString(CultureInfo.InvariantCulture));
                Console.WriteLine($"| {tester.Key} | {string.Join(" | ", perStage)} | {string.Join("/", best)} | " +
                                  $"{tester.Sum(a => a.Merges)} | {tester.Sum(a => a.RallyUsed)}/{tester.Sum(a => a.RallyEarned)} |");
            }
        }

        private static string F(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
    }
}
