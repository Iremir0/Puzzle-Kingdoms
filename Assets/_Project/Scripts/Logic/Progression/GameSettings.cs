using System;
using PuzzleKingdoms.Logic.Localization;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>
    /// Player settings (PB-14): sound effects, music, the speed a battle starts
    /// at and the language (PB-32). Saved by the Flow layer in its own file, separate from the profile, so
    /// "Reset progress" keeps them (D-039). Public fields for JsonUtility.
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        public const int CurrentVersion = 1;
        public static readonly int[] BattleSpeeds = { 1, 2 };

        public int version = CurrentVersion;
        public bool sound = true;
        public bool music = true;
        public int battleSpeed = 1;

        /// <summary>
        /// "en", "tr", or empty for "follow the phone" (PB-32). A new field: files written before it
        /// load with the default, so the version stays 1.
        /// </summary>
        public string language = Languages.Auto;

        /// <summary>The battle speed to start at: an allowed value, 1 for anything else.</summary>
        public int BattleSpeed => Array.IndexOf(BattleSpeeds, battleSpeed) >= 0 ? battleSpeed : 1;

        /// <summary>The language to show on a phone whose own language is Turkish or not.</summary>
        public Language LanguageFor(bool systemIsTurkish) => Languages.Resolve(language, systemIsTurkish);

        /// <summary>Fixes values a hand-edited or older file may have; returns this.</summary>
        public GameSettings Normalize()
        {
            battleSpeed = BattleSpeed;
            language = Languages.Normalize(language);
            version = CurrentVersion;
            return this;
        }

        public GameSettings Copy() => (GameSettings)MemberwiseClone();
    }
}
