namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>
    /// One line of a "merge all" preview: <see cref="Merges"/> merges of
    /// <see cref="Def"/> from <see cref="FromTier"/> into the next tier.
    /// </summary>
    public sealed class MergePlanEntry
    {
        public readonly UnitDef Def;
        public readonly int FromTier;
        public readonly int Merges;

        public MergePlanEntry(UnitDef def, int fromTier, int merges)
        {
            Def = def;
            FromTier = fromTier;
            Merges = merges;
        }

        public int ToTier => FromTier + 1;
    }
}
