namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>Outcome of a finished battle (docs/GDD.md §10 for stars).</summary>
    public sealed class BattleResult
    {
        public bool Victory;
        public bool TimedOut;
        public int Ticks;
        /// <summary>Player units that started the battle.</summary>
        public int Deployed;
        /// <summary>Player units alive at the end.</summary>
        public int Survivors;

        /// <summary>0 on defeat; 1 = win, 2 = at least half survived, 3 = nobody lost.</summary>
        public int Stars
        {
            get
            {
                if (!Victory)
                {
                    return 0;
                }
                if (Survivors >= Deployed)
                {
                    return 3;
                }
                return Survivors * 2 >= Deployed ? 2 : 1;
            }
        }
    }
}
