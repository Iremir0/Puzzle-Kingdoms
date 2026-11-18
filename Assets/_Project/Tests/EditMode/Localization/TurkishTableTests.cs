using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Localization;

namespace PuzzleKingdoms.Tests.Localization
{
    /// <summary>
    /// Checks the Turkish table against the project files (PB-32): it reads cleanly, keeps every
    /// placeholder, and has a translation for every text the screens and the data assets show. Runs
    /// in Unity and in <c>dotnet test</c>; both find the project folder by walking up to <c>Assets/_Project</c>.
    /// </summary>
    public class TurkishTableTests
    {
        private const string TablePath = "Assets/_Project/Resources/Strings/tr.txt";

        // A C# string literal: "..." with \" and \\ inside.
        private const string Literal = "\"((?:[^\"\\\\]|\\\\.)*)\"";
        private static readonly Regex LocCall = new Regex(@"Loc\.[TF]\(\s*(\$?)" + Literal);
        private static readonly Regex ButtonCall = new Regex(@"Ui\.Button\(\s*[^,()]+,\s*(\$?)" + Literal);
        private static readonly Regex CaptionCall = new Regex(@"Ui\.SetCaption\(\s*[^,]+,([^;]*)\);");
        private static readonly Regex AnyLiteral = new Regex("(\\$?)" + Literal);

        private static string _root;
        private static Dictionary<string, string> _table;

        private static string Root => _root ??= FindRoot();

        private static Dictionary<string, string> Table =>
            _table ??= Localizer.ParseTable(File.ReadAllText(Path.Combine(Root, TablePath), Encoding.UTF8), null);

        private static string FindRoot()
        {
            var starts = new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory, AppContext.BaseDirectory };
            foreach (string start in starts)
            {
                for (DirectoryInfo dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "Assets", "_Project")))
                    {
                        return dir.FullName;
                    }
                }
            }
            throw new DirectoryNotFoundException("Assets/_Project not found above " + string.Join(", ", starts));
        }

        [Test]
        public void TableReadsWithoutProblems()
        {
            var problems = new List<string>();
            Localizer.ParseTable(File.ReadAllText(Path.Combine(Root, TablePath), Encoding.UTF8), problems);
            CollectionAssert.IsEmpty(problems);
            Assert.Greater(Table.Count, 50);
        }

        [Test]
        public void EveryTranslationKeepsThePlaceholders()
        {
            var wrong = Table.Where(pair => !Localizer.Placeholders(pair.Key).SetEquals(Localizer.Placeholders(pair.Value)))
                .Select(pair => pair.Key).ToList();
            CollectionAssert.IsEmpty(wrong, "placeholders differ between English and Turkish");
            foreach (KeyValuePair<string, string> pair in Table)
            {
                Assert.DoesNotThrow(() => string.Format(pair.Value, new object[8]), pair.Key);
            }
        }

        [Test]
        public void EveryTextOnTheScreensIsTranslated()
        {
            var missing = new SortedSet<string>();
            var interpolated = new List<string>();
            foreach (string file in CodeFiles())
            {
                string code = File.ReadAllText(file);
                string name = Path.GetFileName(file);
                foreach (Match match in LocCall.Matches(code))
                {
                    Check(match.Groups[1].Value, match.Groups[2].Value, name, missing, interpolated);
                }
                foreach (Match match in ButtonCall.Matches(code))
                {
                    Check(match.Groups[1].Value, match.Groups[2].Value, name, missing, interpolated, interpolatedIsOk: true);
                }
                foreach (Match call in CaptionCall.Matches(code))
                {
                    foreach (Match literal in AnyLiteral.Matches(call.Groups[1].Value))
                    {
                        Check(literal.Groups[1].Value, literal.Groups[2].Value, name, missing, interpolated, interpolatedIsOk: true);
                    }
                }
            }
            CollectionAssert.IsEmpty(interpolated, "Loc.T / Loc.F need a plain English text with {0} placeholders, not $\"...\"");
            CollectionAssert.IsEmpty(missing, "add these to " + TablePath);
            Assert.Greater(Table.Count, 0);
        }

        /// <summary>
        /// A label or text set straight from a plain literal skips the table: "RALLY!" stayed English
        /// that way. Every word the player reads must go through <c>Loc</c> (object names may not).
        /// </summary>
        [Test]
        public void NoScreenTextSkipsTheTable()
        {
            var direct = new Regex("(?:\\.text\\s*=\\s*|Ui\\.Label\\(\\s*[^,()]+,\\s*)" + Literal);
            var found = new List<string>();
            foreach (string file in CodeFiles())
            {
                foreach (Match match in direct.Matches(File.ReadAllText(file)))
                {
                    // "T" + tier is the tier badge ("T1"), written the same in every language.
                    if (Regex.IsMatch(match.Groups[1].Value, "[A-Za-z]") && match.Groups[1].Value != "T")
                    {
                        found.Add($"{Path.GetFileName(file)}: \"{match.Groups[1].Value}\"");
                    }
                }
            }
            CollectionAssert.IsEmpty(found, "wrap these in Loc.T / Loc.F");
        }

        [Test]
        public void EveryStageUnitAndTutorialTextIsTranslated()
        {
            var missing = new SortedSet<string>();
            int seen = 0;
            foreach (string folder in new[] { "Stages", "Units", "Enemies" })
            {
                foreach (string file in Directory.GetFiles(Path.Combine(Root, "Assets/_Project/Data", folder), "*.asset"))
                {
                    foreach (string line in File.ReadAllLines(file))
                    {
                        Match match = Regex.Match(line, @"^\s*(?:displayName|text):\s*(.*)$");
                        if (!match.Success)
                        {
                            continue;
                        }
                        string text = YamlScalar(match.Groups[1].Value);
                        if (text.Length == 0)
                        {
                            continue;
                        }
                        seen++;
                        if (!Table.ContainsKey(text))
                        {
                            missing.Add($"{Path.GetFileName(file)}: {text}");
                        }
                    }
                }
            }
            Assert.Greater(seen, 10, "stage, unit and tutorial texts were read");
            CollectionAssert.IsEmpty(missing, "add these to " + TablePath);
        }

        private static IEnumerable<string> CodeFiles()
        {
            foreach (string folder in new[] { "Views", "Flow" })
            {
                foreach (string file in Directory.GetFiles(Path.Combine(Root, "Assets/_Project/Scripts", folder), "*.cs",
                             SearchOption.AllDirectories))
                {
                    yield return file;
                }
            }
        }

        private static void Check(string dollar, string literal, string file, SortedSet<string> missing,
            List<string> interpolated, bool interpolatedIsOk = false)
        {
            if (dollar.Length > 0)
            {
                if (!interpolatedIsOk)
                {
                    interpolated.Add($"{file}: $\"{literal}\"");
                }
                return;
            }
            string text = Unescape(literal);
            if (text.Length > 0 && !Table.ContainsKey(text))
            {
                missing.Add($"{file}: {text.Replace("\n", "\\n")}");
            }
        }

        private static string Unescape(string literal) =>
            Regex.Replace(literal, @"\\(.)", m => m.Groups[1].Value == "n" ? "\n" : m.Groups[1].Value == "t" ? "\t" : m.Groups[1].Value);

        /// <summary>The value of a one-line YAML scalar as Unity writes it: plain, 'single' or "double" quoted.</summary>
        private static string YamlScalar(string raw)
        {
            string value = raw.Trim();
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
            {
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            }
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                return Unescape(value.Substring(1, value.Length - 2));
            }
            return value;
        }
    }
}
