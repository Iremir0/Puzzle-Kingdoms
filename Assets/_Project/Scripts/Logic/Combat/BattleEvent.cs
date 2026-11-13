namespace PuzzleKingdoms.Logic.Combat
{
    public enum BattleEventType
    {
        /// <summary>Source hit target for Amount damage.</summary>
        Attack,
        /// <summary>Source died.</summary>
        Died,
        RallyStarted,
        RallyEnded,
        /// <summary>Battle over; read BattleSimulator.Result.</summary>
        Finished,
    }

    /// <summary>Something that happened during one simulator tick, for views to play back.</summary>
    public readonly struct BattleEvent
    {
        public readonly BattleEventType Type;
        public readonly int Tick;
        public readonly int SourceId;
        public readonly int TargetId;
        public readonly int Amount;

        public BattleEvent(
            BattleEventType type,
            int tick,
            int sourceId = Combatant.NoTarget,
            int targetId = Combatant.NoTarget,
            int amount = 0)
        {
            Type = type;
            Tick = tick;
            SourceId = sourceId;
            TargetId = targetId;
            Amount = amount;
        }

        public override string ToString() => $"{Tick}:{Type}:{SourceId}>{TargetId}:{Amount}";
    }
}
