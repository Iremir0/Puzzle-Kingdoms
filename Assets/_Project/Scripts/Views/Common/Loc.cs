using PuzzleKingdoms.Logic.Localization;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// The language every screen draws in (PB-32, D-046). Screens pass the English text:
    /// <c>Loc.T("Enemy army")</c>, <c>Loc.F("Moves: {0}", moves)</c>; <c>Ui.Button</c>
    /// and <c>Ui.SetCaption</c> translate their captions themselves, so button objects keep their
    /// English names ("Button Merge all") in every language. The tables are
    /// <c>Resources/Strings/&lt;code&gt;.txt</c> (English needs none). <c>GameApp</c> picks the language.
    /// </summary>
    public static class Loc
    {
        public const string TableFolder = "Strings";

        private static Localizer _current = Localizer.English;

        public static Language Language => _current.Language;

        /// <summary>The active localizer (tests read its table).</summary>
        public static Localizer Current => _current;

        /// <summary>True when the device's own language is Turkish.</summary>
        public static bool SystemIsTurkish => Application.systemLanguage == SystemLanguage.Turkish;

        /// <summary>Switches every later lookup to <paramref name="language"/>; screens drawn before keep their text.</summary>
        public static void Use(Language language)
        {
            if (language == _current.Language)
            {
                return;
            }
            _current = language == Language.English ? Localizer.English : Localizer.FromTable(language, LoadTable(language));
        }

        /// <summary>The table text of a language from Resources, or empty if the file is missing.</summary>
        public static string LoadTable(Language language)
        {
            var asset = Resources.Load<TextAsset>(TableFolder + "/" + Languages.Code(language));
            return asset == null ? "" : asset.text;
        }

        /// <summary>The text in the current language (the English source when there is no translation).</summary>
        public static string T(string source) => _current.T(source);

        /// <summary><see cref="T"/> with <c>{0}</c> arguments; numbers in the language's own style.</summary>
        public static string F(string source, params object[] args) => _current.F(source, args);

        /// <summary>Formats a number the way the current language writes it ("1.2" or "1,2").</summary>
        public static string Number(float value, string format) => value.ToString(format, _current.Numbers);
    }
}
