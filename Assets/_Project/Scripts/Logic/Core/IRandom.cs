namespace PuzzleKingdoms.Logic
{
    /// <summary>
    /// Injected randomness. Logic never uses UnityEngine.Random or System.Random
    /// directly, so results are reproducible on every platform.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Returns an int in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }
}
