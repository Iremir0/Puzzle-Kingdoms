namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>Puzzle phase numbers (docs/GDD.md §5). Stage data may override moves.</summary>
    public sealed class PuzzleRules
    {
        public int BoardWidth = 6;
        public int BoardHeight = 6;
        /// <summary>Cleared gems of one unit class needed for one unit.</summary>
        public int PointsPerUnit = 3;
        /// <summary>Cleared Banner gems needed for one Rally charge.</summary>
        public int PointsPerRallyCharge = 3;
        public int MaxRallyCharges = 3;
        /// <summary>Extra recruitment points for a plain line of exactly 4 gems (D-028).</summary>
        public int FourMatchBonusPoints = 2;
    }
}
