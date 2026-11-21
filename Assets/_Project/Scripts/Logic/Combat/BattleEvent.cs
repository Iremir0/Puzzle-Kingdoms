namespace PuzzleKingdoms.Logic.Combat
{
    public enum BattleEventType
    {
        /// <summary>Source hit target for Amount damage.</summary>
        Attack,
        /// <summary>Source died.</summary>
        Died,
        /// <summary>Source healed target for Amount HP (PB-18 shaman).</summary>
        Heal,
        /// <summary>A commander's Rally hit TargetId for Amount (Arrow Rain, Fireball); there is no attacker (PB-23).</summary>
        RallyStrike,
        RallyStarted,
        RallyEnded,
        /// <summary>Battle over; read BattleSimulator.Result.</summary>
        Finished,
        /// <summary>Source fell to its enrage share of HP and now attacks faster (PB-41 berserker).</summary>
        Enraged,
        /// <summary>Source died and its blast hit TargetId for Amount (PB-41 sapper); comes after Source's Died.</summary>
        Exploded,
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
