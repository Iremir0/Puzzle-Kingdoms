using System;
using System.Collections.Generic;
using System.Numerics;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>
    /// Deterministic fixed-tick battle (docs/GDD.md §9, docs/ARCHITECTURE.md).
    ///
    /// Battlefield space is measured in cells. The player side spawns at
    /// negative y, the enemy side at positive y; x = 0 is the center lane. Each
    /// <see cref="Step"/> advances one tick in three phases:
    ///   1. every living unit, in id order, picks a target and either attacks
    ///      (in range and ready) or moves towards it;
    ///   2. allies that overlap are pushed apart;
    ///   3. all hits of the tick land at once, so units killed this tick still
    ///      strike back (D-017).
    /// A battle is reproducible from (armies, rules, InputLog).
    /// </summary>
    public sealed class BattleSimulator
    {
        private const float Epsilon = 0.000001f;

        public readonly BattleRules Rules;
        public int Tick { get; private set; }
        /// <summary>Player units first (front to back, left to right), then enemies.</summary>
        public readonly List<Combatant> Combatants = new List<Combatant>();
        public readonly Rally Rally;
        /// <summary>Ticks at which the player activated Rally (before that tick's step).</summary>
        public readonly List<int> InputLog = new List<int>();
        /// <summary>Set when the battle is over.</summary>
        public BattleResult Result { get; private set; }

        private readonly int _timeoutTicks;
        private readonly List<BattleEvent> _pendingEvents = new List<BattleEvent>();
        private readonly List<BattleEvent> _hits = new List<BattleEvent>();

        public BattleSimulator(BattleRules rules, ArmyRules armyRules, Formation player, Formation enemy, int rallyCharges)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _timeoutTicks = rules.SecondsToTicks(rules.TimeoutSeconds);
            Rally = new Rally(rallyCharges, rules.SecondsToTicks(rules.RallyDurationSeconds));
            SpawnSide(player, Side.Player, armyRules);
            SpawnSide(enemy, Side.Enemy, armyRules);
        }

        public bool IsFinished => Result != null;

        /// <summary>Battlefield position of a formation cell for the given side.</summary>
        public Vector2 CellToPosition(Side side, GridPos cell)
        {
            float depth = Rules.FrontGap / 2f + cell.Y * Rules.RowSpacing;
            float x = (cell.X - 1) * Rules.LaneSpacing;
            return new Vector2(x, side == Side.Player ? -depth : depth);
        }

        public Combatant GetCombatant(int id) =>
            id >= 0 && id < Combatants.Count ? Combatants[id] : null;

        public int AliveCount(Side side)
        {
            int count = 0;
            foreach (Combatant c in Combatants)
            {
                if (c.Side == side && c.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Starts Rally before the next tick. Returns false if not possible.</summary>
        public bool ActivateRally()
        {
            if (IsFinished || !Rally.Activate())
            {
                return false;
            }
            InputLog.Add(Tick);
            _pendingEvents.Add(new BattleEvent(BattleEventType.RallyStarted, Tick + 1));
            return true;
        }

        /// <summary>Advances one tick and returns what happened, in playback order.</summary>
        public List<BattleEvent> Step()
        {
            var events = new List<BattleEvent>();
            if (IsFinished)
            {
                return events;
            }
            events.AddRange(_pendingEvents);
            _pendingEvents.Clear();
            Tick++;

            float dt = 1f / Rules.TicksPerSecond;
            _hits.Clear();
            foreach (Combatant c in Combatants)
            {
                if (c.IsAlive)
                {
                    Act(c, dt);
                }
            }
            SeparateAllies();

            var died = new List<Combatant>();
            foreach (BattleEvent hit in _hits)
            {
                Combatant target = Combatants[hit.TargetId];
                if (target.IsAlive)
                {
                    target.Hp -= hit.Amount;
                    if (!target.IsAlive)
                    {
                        died.Add(target);
                    }
                }
                events.Add(hit);
            }
            foreach (Combatant c in died)
            {
                c.Hp = 0;
                events.Add(new BattleEvent(BattleEventType.Died, Tick, c.Id));
            }

            if (Rally.Tick())
            {
                events.Add(new BattleEvent(BattleEventType.RallyEnded, Tick));
            }
            CheckFinished(events);
            return events;
        }

        /// <summary>Runs the battle to its end. <paramref name="rallyTicks"/> replays recorded Rally input.</summary>
        public BattleResult RunToEnd(IReadOnlyCollection<int> rallyTicks = null)
        {
            var rallyAt = rallyTicks != null ? new HashSet<int>(rallyTicks) : new HashSet<int>();
            while (!IsFinished)
            {
                if (rallyAt.Contains(Tick))
                {
                    ActivateRally();
                }
                Step();
            }
            return Result;
        }

        private void SpawnSide(Formation formation, Side side, ArmyRules armyRules)
        {
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    var cell = new GridPos(x, y);
                    ArmyUnit unit = formation.GetUnit(cell);
                    if (unit == null)
                    {
                        continue;
                    }
                    var c = new Combatant(Combatants.Count, side, unit, cell, armyRules);
                    c.Position = CellToPosition(side, cell);
                    Combatants.Add(c);
                }
            }
        }

        private void Act(Combatant c, float dt)
        {
            Combatant target = CurrentTarget(c);
            if (target == null)
            {
                return;
            }
            bool buffed = c.Side == Side.Player && Rally.IsActive;
            float distance = Vector2.Distance(c.Position, target.Position);
            bool inRange = distance <= c.AttackRange + Epsilon;

            if (inRange && c.Cooldown <= Epsilon)
            {
                float multiplier = 1f + (buffed ? Rules.RallyDamageBonus : 0f);
                int amount = Damage.Compute(c.Damage, c.DamageType, target, multiplier);
                _hits.Add(new BattleEvent(BattleEventType.Attack, Tick, c.Id, target.Id, amount));
                c.Cooldown += c.AttackInterval;
            }
            else if (!inRange)
            {
                float stepLength = Math.Min(c.MoveSpeed * dt, distance - c.AttackRange);
                c.Position += (target.Position - c.Position) / distance * stepLength;
            }

            float speed = 1f + (buffed ? Rules.RallyAttackSpeedBonus : 0f);
            c.Cooldown -= dt * speed;
            if (c.Cooldown < 0f && !inRange)
            {
                c.Cooldown = 0f; // no stored attacks while walking
            }
        }

        private Combatant CurrentTarget(Combatant c)
        {
            Combatant target = GetCombatant(c.TargetId);
            if (target == null || !target.IsAlive)
            {
                target = Targeting.PickTarget(c, Combatants, Rules.SameLaneTieDistance);
                c.TargetId = target?.Id ?? Combatant.NoTarget;
            }
            return target;
        }

        private void SeparateAllies()
        {
            float minDistance = Rules.SeparationDistance;
            for (int i = 0; i < Combatants.Count; i++)
            {
                Combatant a = Combatants[i];
                if (!a.IsAlive)
                {
                    continue;
                }
                for (int j = i + 1; j < Combatants.Count; j++)
                {
                    Combatant b = Combatants[j];
                    if (b.Side != a.Side || !b.IsAlive)
                    {
                        continue;
                    }
                    Vector2 offset = b.Position - a.Position;
                    float distance = offset.Length();
                    if (distance >= minDistance)
                    {
                        continue;
                    }
                    Vector2 direction = distance > Epsilon ? offset / distance : Vector2.UnitX;
                    Vector2 push = direction * (minDistance - distance) / 2f;
                    a.Position -= push;
                    b.Position += push;
                }
            }
        }

        private void CheckFinished(List<BattleEvent> events)
        {
            int playerAlive = AliveCount(Side.Player);
            int enemyAlive = AliveCount(Side.Enemy);
            bool timedOut = Tick >= _timeoutTicks;
            if (playerAlive > 0 && enemyAlive > 0 && !timedOut)
            {
                return;
            }
            var result = new BattleResult
            {
                Victory = enemyAlive == 0 && playerAlive > 0,
                TimedOut = timedOut && playerAlive > 0 && enemyAlive > 0,
                Ticks = Tick,
                Survivors = playerAlive,
            };
            foreach (Combatant c in Combatants)
            {
                if (c.Side == Side.Player)
                {
                    result.Deployed++;
                }
            }
            Result = result;
            events.Add(new BattleEvent(BattleEventType.Finished, Tick));
        }
    }
}
