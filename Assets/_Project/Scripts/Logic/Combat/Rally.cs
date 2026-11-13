namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>
    /// Player-timed army buff (docs/GDD.md §9). One activation at a time; each
    /// consumes a charge earned on the puzzle board.
    /// </summary>
    public sealed class Rally
    {
        public int Charges { get; private set; }
        public readonly int DurationTicks;
        public int TicksLeft { get; private set; }

        public Rally(int charges, int durationTicks)
        {
            Charges = charges;
            DurationTicks = durationTicks;
        }

        public bool IsActive => TicksLeft > 0;

        public bool CanActivate => Charges > 0 && !IsActive;

        public bool Activate()
        {
            if (!CanActivate)
            {
                return false;
            }
            Charges--;
            TicksLeft = DurationTicks;
            return true;
        }

        /// <summary>Advances one tick. Returns true on the tick the buff runs out.</summary>
        public bool Tick()
        {
            if (!IsActive)
            {
                return false;
            }
            TicksLeft--;
            return TicksLeft == 0;
        }
    }
}
