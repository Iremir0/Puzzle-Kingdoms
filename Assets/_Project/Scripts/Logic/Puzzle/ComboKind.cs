namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>Two special gems swapped into each other (PB-20, D-050).</summary>
    public enum ComboKind
    {
        None = 0,
        /// <summary>Blast + Blast: everything within two cells of the swap (5×5) is hit.</summary>
        BlastBlast = 1,
        /// <summary>Prism + Blast: every gem of the Blast's colour becomes a Blast and goes off.</summary>
        PrismBlast = 2,
        /// <summary>Prism + Prism: the whole board is hit.</summary>
        PrismPrism = 3,
    }
}
