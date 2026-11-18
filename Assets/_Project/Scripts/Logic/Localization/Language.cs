namespace PuzzleKingdoms.Logic.Localization
{
    /// <summary>The languages the game ships in (PB-32). English is the source language of every text.</summary>
    public enum Language
    {
        English,
        Turkish,
    }

    /// <summary>Language codes as saved in the settings, and how "automatic" is resolved.</summary>
    public static class Languages
    {
        /// <summary>The setting value that means "follow the phone's language".</summary>
        public const string Auto = "";

        public static readonly Language[] All = { Language.English, Language.Turkish };

        /// <summary>"en" or "tr": the code saved in the settings and the name of the string table.</summary>
        public static string Code(Language language) => language == Language.Turkish ? "tr" : "en";

        /// <summary>The language's own name, shown on the language buttons in every language.</summary>
        public static string NativeName(Language language) => language == Language.Turkish ? "Türkçe" : "English";

        /// <summary>Reads a saved code ("en", "tr", any case, surrounding spaces ignored).</summary>
        public static bool TryParse(string code, out Language language)
        {
            string value = code == null ? "" : code.Trim().ToLowerInvariant();
            if (value == "tr")
            {
                language = Language.Turkish;
                return true;
            }
            language = Language.English;
            return value == "en";
        }

        /// <summary>
        /// The language to show: the saved choice if it is a known code, otherwise (automatic or a
        /// value we do not know) Turkish on a Turkish phone and English everywhere else.
        /// </summary>
        public static Language Resolve(string setting, bool systemIsTurkish) =>
            TryParse(setting, out Language chosen) ? chosen : systemIsTurkish ? Language.Turkish : Language.English;

        /// <summary>A saved value cleaned up: a known code in lower case, anything else becomes automatic.</summary>
        public static string Normalize(string setting) =>
            TryParse(setting, out Language chosen) ? Code(chosen) : Auto;
    }
}
