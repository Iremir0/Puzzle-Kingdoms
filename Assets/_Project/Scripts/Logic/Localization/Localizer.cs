using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PuzzleKingdoms.Logic.Localization
{
    /// <summary>
    /// Every text the player reads, in one language (PB-32, D-046). Texts are looked up by their English
    /// source ("Merge all" → "Hepsini birleştir"), so the code, the scene object names and the English
    /// game stay readable and a missing translation simply shows the English text. A table is a plain
    /// text file with one <c>English&lt;TAB&gt;translation</c> pair per line (<see cref="ParseTable"/>),
    /// so a translator can edit it in any editor or spreadsheet.
    /// </summary>
    public sealed class Localizer
    {
        private static readonly NumberFormatInfo EnglishNumbers = CultureInfo.InvariantCulture.NumberFormat;
        private static readonly NumberFormatInfo TurkishNumbers = MakeTurkishNumbers();

        private readonly Dictionary<string, string> _table;

        public Localizer(Language language, IDictionary<string, string> table)
        {
            Language = language;
            _table = table == null ? new Dictionary<string, string>() : new Dictionary<string, string>(table);
        }

        /// <summary>English: every text is its own source.</summary>
        public static Localizer English { get; } = new Localizer(Language.English, null);

        public Language Language { get; }

        /// <summary>How numbers are written: "2.5" in English, "2,5" in Turkish.</summary>
        public IFormatProvider Numbers => Language == Language.Turkish ? TurkishNumbers : EnglishNumbers;

        /// <summary>The source texts this table translates.</summary>
        public IEnumerable<string> Sources => _table.Keys;

        /// <summary>The translated texts.</summary>
        public IEnumerable<string> Translations => _table.Values;

        /// <summary>Builds a localizer from table text; problems in the text are ignored line by line.</summary>
        public static Localizer FromTable(Language language, string tableText) =>
            language == Language.English ? English : new Localizer(language, ParseTable(tableText, null));

        /// <summary>True when the table has a translation for <paramref name="source"/> (always true in English).</summary>
        public bool Has(string source) => Language == Language.English || (source != null && _table.ContainsKey(source));

        /// <summary>The text in this language; the source itself when there is no translation (or in English).</summary>
        public string T(string source)
        {
            if (string.IsNullOrEmpty(source) || Language == Language.English)
            {
                return source ?? "";
            }
            return _table.TryGetValue(source, out string text) ? text : source;
        }

        /// <summary>
        /// <see cref="T"/> with <c>{0}</c>-style arguments filled in, numbers written the way this
        /// language writes them (<c>{0:0.0}</c> gives "2.5" or "2,5").
        /// </summary>
        public string F(string source, params object[] args)
        {
            string format = T(source);
            try
            {
                return string.Format(Numbers, format, args);
            }
            catch (FormatException)
            {
                // A broken translation must never stop the game: fall back to the English pattern.
                return string.Format(Numbers, source ?? "", args);
            }
        }

        /// <summary>
        /// Reads a table: one <c>source&lt;TAB&gt;translation</c> pair per line. Empty lines and lines starting
        /// with <c>#</c> are skipped; <c>\n</c> in either column is a line break and <c>\t</c> a tab. Lines
        /// without a tab, with an empty column or with a source seen before are reported in
        /// <paramref name="problems"/> (when given): the first pair of a source wins.
        /// </summary>
        public static Dictionary<string, string> ParseTable(string text, List<string> problems)
        {
            var table = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(text))
            {
                return table;
            }
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (i == 0 && line.Length > 0 && line[0] == '﻿')
                {
                    line = line.Substring(1); // byte order mark
                }
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }
                int tab = line.IndexOf('\t');
                if (tab < 0)
                {
                    problems?.Add($"line {i + 1}: no tab between the English text and the translation");
                    continue;
                }
                string source = Unescape(line.Substring(0, tab));
                string translation = Unescape(line.Substring(tab + 1).TrimEnd());
                if (source.Length == 0 || translation.Length == 0)
                {
                    problems?.Add($"line {i + 1}: an empty column");
                    continue;
                }
                if (table.ContainsKey(source))
                {
                    problems?.Add($"line {i + 1}: \"{source}\" is already in the table");
                    continue;
                }
                table.Add(source, translation);
            }
            return table;
        }

        /// <summary>The <c>{0}</c>, <c>{1:0.0}</c> ... placeholders of a text, by number (format part ignored).</summary>
        public static SortedSet<int> Placeholders(string text)
        {
            var found = new SortedSet<int>();
            if (string.IsNullOrEmpty(text))
            {
                return found;
            }
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '{')
                {
                    continue;
                }
                if (i + 1 < text.Length && text[i + 1] == '{')
                {
                    i++; // "{{" is a literal brace
                    continue;
                }
                int j = i + 1;
                int number = 0;
                bool digits = false;
                while (j < text.Length && char.IsDigit(text[j]))
                {
                    number = number * 10 + (text[j] - '0');
                    digits = true;
                    j++;
                }
                if (digits && j < text.Length && (text[j] == '}' || text[j] == ':' || text[j] == ','))
                {
                    found.Add(number);
                }
            }
            return found;
        }

        private static string Unescape(string text)
        {
            if (text.IndexOf('\\') < 0)
            {
                return text;
            }
            var result = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\\' && i + 1 < text.Length)
                {
                    char next = text[i + 1];
                    if (next == 'n' || next == 't' || next == '\\')
                    {
                        result.Append(next == 'n' ? '\n' : next == 't' ? '\t' : '\\');
                        i++;
                        continue;
                    }
                }
                result.Append(text[i]);
            }
            return result.ToString();
        }

        private static NumberFormatInfo MakeTurkishNumbers()
        {
            // Built by hand instead of CultureInfo("tr-TR"), so it is the same on every platform and build.
            var numbers = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            numbers.NumberDecimalSeparator = ",";
            numbers.NumberGroupSeparator = ".";
            numbers.PercentDecimalSeparator = ",";
            numbers.PercentGroupSeparator = ".";
            return NumberFormatInfo.ReadOnly(numbers);
        }
    }
}
