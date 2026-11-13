namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>Battle simulation and Rally numbers (docs/GDD.md §9). Distances in cells.</summary>
    public sealed class BattleRules
    {
        public int TicksPerSecond = 20;
        /// <summary>A battle still running after this long is a defeat.</summary>
        public float TimeoutSeconds = 60f;
        /// <summary>Distance between the two front rows at spawn.</summary>
        public float FrontGap = 6f;
        /// <summary>Distance between formation rows of one side.</summary>
        public float RowSpacing = 1f;
        /// <summary>Distance between lanes.</summary>
        public float LaneSpacing = 1.2f;
        /// <summary>Targets this much farther than the nearest one still win if in the same lane.</summary>
        public float SameLaneTieDistance = 0.5f;
        /// <summary>Allies closer than this are pushed apart.</summary>
        public float SeparationDistance = 0.6f;
        public float RallyDamageBonus = 0.5f;
        public float RallyAttackSpeedBonus = 0.5f;
        public float RallyDurationSeconds = 5f;

        public int SecondsToTicks(float seconds) =>
            (int)System.Math.Round(seconds * TicksPerSecond, System.MidpointRounding.AwayFromZero);
    }
}
