using PuzzleKingdoms.Logic.Combat;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>
    /// A commander (PB-23, D-053): leads the army and decides what Rally does. It does not fight. Earned by
    /// winning its unlock stage, never bought or drawn.
    /// </summary>
    public sealed class Commander
    {
        public string Id = "";
        public string DisplayName = "";
        public string Description = "";
        /// <summary>The stage (id) whose first win unlocks this commander.</summary>
        public string UnlockStageId = "";
        public RallyVariant Variant = new RallyVariant();
    }
}
