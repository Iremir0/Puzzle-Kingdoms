namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// A special property a gem can carry on top of its type (D-028). A special
    /// gem keeps its color and still matches like a normal gem of that color.
    /// </summary>
    public enum SpecialKind
    {
        None = 0,
        /// <summary>Made by a 2×2 square. When cleared, it also clears the 3×3 area around it.</summary>
        Blast = 1,
        /// <summary>
        /// Made by a straight line of 5 or more. Swapped with a neighbour, it clears every gem
        /// of the neighbour's type; cleared in a match, it clears every gem of its own type.
        /// </summary>
        Prism = 2,
    }
}
