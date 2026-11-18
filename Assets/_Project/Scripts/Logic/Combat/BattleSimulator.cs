using System;
using System.Collections.Generic;
using System.Numerics;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Kingdom;

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

        private readonly KingdomBonuses _bonuses;
        private readonly int _timeoutTicks;
        private readonly List<BattleEvent> _pendingEvents = new List<BattleEvent>();
        private readonly List<BattleEvent> _hits = new List<BattleEvent>();
        private readonly List<BattleEvent> _heals = new List<BattleEvent>();

        public BattleSimulator(BattleRules rules, ArmyRules armyRules, Formation player, Formation enemy, int rallyCharges,
            KingdomBonuses bonuses = null)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _timeoutTicks = rules.SecondsToTicks(rules.TimeoutSeconds);
            Rally = new Rally(rallyCharges, rules.SecondsToTicks(rules.RallyDurationSeconds));
            _bonuses = bonuses ?? KingdomBonuses.None;
            SpawnSide(player, Side.Player, armyRules);
            SpawnSide(enemy, Side.Enemy, armyRules);
        }

        public bool IsFinished => Result != null;

        /// <summary>Battlefield position of a formation cell for the given side.</summary>
        public Vector2 CellToPosition(Side side, GridPos cell)
        {
            float depth = Rules.FrontGap / 2f + cell.Y * Rules.RowSpacing;
            float x = (cell.X - (Formation.Size - 1) / 2f) * Rules.LaneSpacing;
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
            _heals.Clear();
            foreach (Combatant c in Combatants)
            {
                if (c.IsAlive)
                {
                    Heal(c, dt);
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
            // Heals land after the hits: a unit killed this tick is not saved by a heal (PB-18).
            foreach (BattleEvent heal in _heals)
            {
                Combatant target = Combatants[heal.TargetId];
                int restored = Math.Min(heal.Amount, target.MaxHp - target.Hp);
                if (target.IsAlive && restored > 0)
                {
                    target.Hp += restored;
                    events.Add(new BattleEvent(BattleEventType.Heal, Tick, heal.SourceId, heal.TargetId, restored));
                }
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
                    var c = new Combatant(Combatants.Count, side, unit, cell, armyRules, _bonuses);
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
                _hits.Add(HitOn(c, target, multiplier));
                if (c.Def.SplashRadius > 0f)
                {
                    foreach (Combatant other in Combatants)
                    {
                        if (other != target && other.IsAlive && c.IsEnemyOf(other)
                            && Vector2.Distance(target.Position, other.Position) <= c.Def.SplashRadius)
                        {
                            _hits.Add(HitOn(c, other, multiplier * c.Def.SplashFraction));
                        }
                    }
                }
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

        private BattleEvent HitOn(Combatant attacker, Combatant target, float multiplier)
        {
            if (target.Def.RangedDamageReduction > 0f
                && attacker.AttackRange > UnitDef.RangedThreshold
                && IsFrontMost(target))
            {
                multiplier *= 1f - target.Def.RangedDamageReduction;
            }
            int amount = Damage.Compute(attacker.Damage, attacker.DamageType, target, multiplier);
            return new BattleEvent(BattleEventType.Attack, Tick, attacker.Id, target.Id, amount);
        }

        /// <summary>True when no living ally of <paramref name="c"/> stands nearer the enemy line.</summary>
        private bool IsFrontMost(Combatant c)
        {
            // The player side is at negative y and the enemy side at positive y, so "forward"
            // is up for the player and down for the enemy.
            float forward = c.Side == Side.Player ? c.Position.Y : -c.Position.Y;
            foreach (Combatant other in Combatants)
            {
                if (other != c && other.Side == c.Side && other.IsAlive
                    && (other.Side == Side.Player ? other.Position.Y : -other.Position.Y) > forward + Epsilon)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Shaman: heals the most hurt living ally (lowest HP fraction) in range when ready.</summary>
        private void Heal(Combatant c, float dt)
        {
            if (c.Def.HealAmount <= 0)
            {
                return;
            }
            c.HealCooldown = Math.Max(0f, c.HealCooldown - dt);
            if (c.HealCooldown > Epsilon)
            {
                return;
            }
            Combatant best = null;
            foreach (Combatant other in Combatants)
            {
                if (other == c || other.Side != c.Side || !other.IsAlive || other.Hp >= other.MaxHp
                    || Vector2.Distance(c.Position, other.Position) > c.Def.HealRange + Epsilon)
                {
                    continue;
                }
                if (best == null || (long)other.Hp * best.MaxHp < (long)best.Hp * other.MaxHp)
                {
                    best = other;
                }
            }
            if (best != null)
            {
                _heals.Add(new BattleEvent(BattleEventType.Heal, Tick, c.Id, best.Id, c.Def.HealAmount));
                c.HealCooldown = c.Def.HealInterval;
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
