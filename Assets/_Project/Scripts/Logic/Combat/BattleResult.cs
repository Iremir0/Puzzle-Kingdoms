namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>Outcome of a finished battle (docs/GDD.md §10 for stars).</summary>
    /// <summary>The main reason a battle was lost, for a one-line hint on the results screen (PB-35).</summary>
    public enum DefeatReason
    {
        None = 0,
        /// <summary>Time ran out with less of the army left than the enemy's.</summary>
        TimeUp = 1,
        /// <summary>Lost with Rally charges never used.</summary>
        RallyUnused = 2,
        /// <summary>The enemy fielded clearly more units.</summary>
        Outnumbered = 3,
        /// <summary>Lost on strength: stronger units are what would help.</summary>
        Overpowered = 4,
    }

    public sealed class BattleResult
    {
        public bool Victory;
        public bool TimedOut;
        /// <summary>Why the battle was lost (<see cref="DefeatReason.None"/> on a victory).</summary>
        public DefeatReason Reason;
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
                if (TimedOut)
                {
                    return 1; // D-069: a win on time is a narrow one
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
