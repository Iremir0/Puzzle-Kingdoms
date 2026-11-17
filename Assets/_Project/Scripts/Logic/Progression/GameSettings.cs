using System;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>
    /// Player settings (PB-14): sound effects, music and the speed a battle starts
    /// at. Saved by the Flow layer in its own file, separate from the profile, so
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

        /// <summary>The battle speed to start at: an allowed value, 1 for anything else.</summary>
        public int BattleSpeed => Array.IndexOf(BattleSpeeds, battleSpeed) >= 0 ? battleSpeed : 1;

        /// <summary>Fixes values a hand-edited or older file may have; returns this.</summary>
        public GameSettings Normalize()
        {
            battleSpeed = BattleSpeed;
            version = CurrentVersion;
            return this;
        }

        public GameSettings Copy() => (GameSettings)MemberwiseClone();
    }
}
