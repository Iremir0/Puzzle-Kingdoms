using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Kingdom;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Logic.Flow
{
    /// <summary>The numbers an expedition is built from (PB-26, D-062); filled from a data asset.</summary>
    public sealed class ExpeditionRules
    {
        /// <summary>Battles in a run; the last one is the boss.</summary>
        public int Steps = 5;
        /// <summary>Puzzle moves before the first battle, then before each later one (fewer: the army carries over).</summary>
        public int FirstMoves = 12;
        public int MovesPerStep = 8;
        /// <summary>Share of its HP a unit regains between battles.</summary>
        public float RestHeal = 0.3f;
        public float EnemyHpGrowth = 0.12f;
        public float EnemyDamageGrowth = 0.08f;
        public int MaxTier = 3;
        /// <summary>Tier of the boss.</summary>
        public int BossTier = 2;
        public int RecruitAmount = 3;
        public int RallyAmount = 1;
        public int GoldPerStep = 60;
        public int GoldForWinning = 250;
        /// <summary>Percent a perk adds to one class's HP, damage or attack speed for the rest of the run (PB-39, D-072).</summary>
        public int PerkPercent = 20;
        public readonly List<UnitDef> EnemyPool = new List<UnitDef>();
        public UnitDef Boss;
        /// <summary>The classes a Recruit reward can offer.</summary>
        public readonly List<UnitDef> PlayerClasses = new List<UnitDef>();
    }

    public enum ExpeditionRewardKind
    {
        /// <summary>New tier 1 units of one class.</summary>
        Recruit = 0,
        /// <summary>Every unit back to full HP.</summary>
        HealAll = 1,
        /// <summary>The weakest unit gains a tier.</summary>
        Promote = 2,
        /// <summary>Extra Rally charges in the next battle.</summary>
        Rally = 3,
        /// <summary>Perk: one class has more HP for the rest of the run (PB-39).</summary>
        PerkHp = 4,
        /// <summary>Perk: one class hits harder for the rest of the run.</summary>
        PerkDamage = 5,
        /// <summary>Perk: one class attacks faster for the rest of the run.</summary>
        PerkSpeed = 6,
    }

    public sealed class ExpeditionReward
    {
        public ExpeditionRewardKind Kind;
        /// <summary>The class of a Recruit reward or a perk.</summary>
        public string UnitId = "";
        /// <summary>Units recruited, Rally charges, or a perk's percent.</summary>
        public int Amount;

        /// <summary>A lasting bonus for the rest of the run rather than a one-off reward (PB-39).</summary>
        public bool IsPerk => Kind == ExpeditionRewardKind.PerkHp || Kind == ExpeditionRewardKind.PerkDamage || Kind == ExpeditionRewardKind.PerkSpeed;
    }

    public enum ExpeditionPhase
    {
        /// <summary>The next battle is waiting: puzzle, War Camp, battle.</summary>
        Battle = 0,
        /// <summary>A battle was won; one of the offers must be picked.</summary>
        Reward = 1,
        Won = 2,
        Lost = 3,
    }

    /// <summary>A unit carried between battles, as saved.</summary>
    [Serializable]
    public sealed class ExpeditionSave
    {
        public string seed = "";
        public int step;
        public int phase;
        public int gold;
        public int bonusRally;
        public List<string> armyIds = new List<string>();
        public List<int> armyTiers = new List<int>();
        /// <summary>HP share in thousandths (JsonUtility-friendly).</summary>
        public List<int> armyHp = new List<int>();
        public List<int> offerKinds = new List<int>();
        public List<string> offerUnits = new List<string>();
        public List<int> offerAmounts = new List<int>();
        /// <summary>Perks taken so far (PB-39); a new field, older saves load with none.</summary>
        public List<int> perkKinds = new List<int>();
        public List<string> perkUnits = new List<string>();
        public List<int> perkAmounts = new List<int>();
    }

    /// <summary>
    /// A roguelite run (PB-26, D-062): a row of battles where the army and its wounds carry over. Before each
    /// battle comes a short puzzle that adds recruits; after a win the player picks one of three rewards. A loss
    /// ends the run. The same seed gives the same enemies and offers. The screens drive it with the usual
    /// <see cref="StageRun"/>; this class only holds what lasts between battles.
    /// </summary>
    public sealed class Expedition
    {
        public readonly ExpeditionRules Rules;
        public readonly ulong Seed;
        /// <summary>Battles won so far; also the index of the battle to fight.</summary>
        public int Step { get; private set; }
        public ExpeditionPhase Phase { get; private set; } = ExpeditionPhase.Battle;
        public int GoldEarned { get; private set; }
        public int BonusRally { get; private set; }
        /// <summary>The units that go on: class, tier and how hurt they are.</summary>
        public readonly List<ArmyUnit> Army = new List<ArmyUnit>();
        public readonly List<ExpeditionReward> Offers = new List<ExpeditionReward>();
        /// <summary>Perks picked so far; they last until the run ends (PB-39, D-072).</summary>
        public readonly List<ExpeditionReward> Perks = new List<ExpeditionReward>();

        public Expedition(ExpeditionRules rules, ulong seed)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Seed = seed;
        }

        public bool IsOver => Phase == ExpeditionPhase.Won || Phase == ExpeditionPhase.Lost;

        public bool IsBossStep => Step == Rules.Steps - 1;

        /// <summary>The perks as battle bonuses for the player's units (the same mechanism as Kingdom buildings).</summary>
        public KingdomBonuses Bonuses()
        {
            var bonuses = new KingdomBonuses();
            foreach (ExpeditionReward perk in Perks)
            {
                float share = perk.Amount / 100f;
                switch (perk.Kind)
                {
                    case ExpeditionRewardKind.PerkHp: bonuses.Add(BuildingStat.UnitHp, perk.UnitId, share); break;
                    case ExpeditionRewardKind.PerkDamage: bonuses.Add(BuildingStat.UnitDamage, perk.UnitId, share); break;
                    case ExpeditionRewardKind.PerkSpeed: bonuses.Add(BuildingStat.UnitAttackSpeed, perk.UnitId, share); break;
                }
            }
            return bonuses;
        }

        /// <summary>The enemy and puzzle of the battle to fight now.</summary>
        public StageDef StageForStep()
        {
            if (Rules.EnemyPool.Count == 0)
            {
                throw new InvalidOperationException("The expedition needs a pool of enemies");
            }
            var random = new SeededRandom(Mix(Seed, (ulong)Step + 1));
            var stage = new StageDef
            {
                Id = "expedition-" + (Step + 1),
                DisplayName = "Expedition",
                Chapter = 1,
                Moves = Step == 0 ? Rules.FirstMoves : Rules.MovesPerStep,
                BoardSeed = Mix(Seed, 1000UL + (ulong)Step) | 1UL,
                EnemyHpMultiplier = 1f + Rules.EnemyHpGrowth * Step,
                EnemyDamageMultiplier = 1f + Rules.EnemyDamageGrowth * Step,
            };
            var cells = new List<GridPos>();
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    cells.Add(new GridPos(x, y));
                }
            }
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                GridPos swap = cells[i];
                cells[i] = cells[j];
                cells[j] = swap;
            }
            int tier = Math.Min(Rules.MaxTier, 1 + Step / 2);
            int escorts = 3 + Step;
            int placed = 0;
            if (IsBossStep && Rules.Boss != null)
            {
                stage.Enemies.Add(new EnemySlot { Unit = Rules.Boss, Tier = Rules.BossTier, Cell = new GridPos(1, 0) });
                cells.RemoveAll(c => c.X == 1 && c.Y == 0);
                escorts = 4;
                tier = Math.Max(1, Rules.MaxTier - 1);
            }
            for (; placed < escorts && placed < cells.Count; placed++)
            {
                stage.Enemies.Add(new EnemySlot
                {
                    Unit = Rules.EnemyPool[random.Range(0, Rules.EnemyPool.Count)],
                    Tier = tier,
                    Cell = cells[placed],
                });
            }
            return stage;
        }

        /// <summary>
        /// Takes in a finished battle. A loss ends the run. A win keeps the survivors and the units that stayed on
        /// the bench, each regaining some HP, and offers three rewards (or ends the run won after the last battle).
        /// </summary>
        public void Finish(StageRun run)
        {
            if (Phase != ExpeditionPhase.Battle)
            {
                throw new InvalidOperationException("No battle is waiting");
            }
            if (run == null || run.Battle == null || !run.Battle.IsFinished)
            {
                throw new InvalidOperationException("The battle is not over");
            }
            BonusRally = 0;
            if (!run.Battle.Result.Victory)
            {
                Phase = ExpeditionPhase.Lost;
                return;
            }

            var next = new List<ArmyUnit>();
            foreach (Combatant c in run.Battle.Combatants)
            {
                if (c.Side == Side.Player && c.IsAlive)
                {
                    next.Add(Carried(c.Unit, c.Hp / (float)c.MaxHp));
                }
            }
            foreach (ArmyUnit unit in run.Roster.Bench)
            {
                next.Add(Carried(unit, unit.HpFraction));
            }
            Army.Clear();
            Army.AddRange(next);
            Step++;
            GoldEarned += Rules.GoldPerStep;
            Offers.Clear();
            if (Step >= Rules.Steps)
            {
                GoldEarned += Rules.GoldForWinning;
                Phase = ExpeditionPhase.Won;
                return;
            }
            Offers.AddRange(DrawOffers());
            Phase = ExpeditionPhase.Reward;
        }

        private ArmyUnit Carried(ArmyUnit unit, float fraction) =>
            new ArmyUnit(unit.Def, unit.Tier) { HpFraction = Math.Min(1f, Math.Max(0.01f, fraction) + Rules.RestHeal) };

        /// <summary>Picks one of the offered rewards and moves on to the next battle.</summary>
        public bool Pick(int index)
        {
            if (Phase != ExpeditionPhase.Reward || index < 0 || index >= Offers.Count)
            {
                return false;
            }
            Apply(Offers[index]);
            Offers.Clear();
            Phase = ExpeditionPhase.Battle;
            return true;
        }

        private void Apply(ExpeditionReward reward)
        {
            switch (reward.Kind)
            {
                case ExpeditionRewardKind.Recruit:
                    UnitDef def = Rules.PlayerClasses.Find(u => u.Id == reward.UnitId);
                    for (int i = 0; def != null && i < reward.Amount; i++)
                    {
                        Army.Add(new ArmyUnit(def));
                    }
                    break;
                case ExpeditionRewardKind.HealAll:
                    foreach (ArmyUnit unit in Army)
                    {
                        unit.HpFraction = 1f;
                    }
                    break;
                case ExpeditionRewardKind.Promote:
                    int weakest = WeakestPromotable();
                    if (weakest >= 0)
                    {
                        ArmyUnit old = Army[weakest];
                        Army[weakest] = new ArmyUnit(old.Def, old.Tier + 1);
                    }
                    break;
                case ExpeditionRewardKind.Rally:
                    BonusRally += reward.Amount;
                    break;
                default:
                    if (reward.IsPerk)
                    {
                        Perks.Add(reward);
                    }
                    break;
            }
        }

        private int WeakestPromotable()
        {
            int best = -1;
            for (int i = 0; i < Army.Count; i++)
            {
                if (Army[i].Tier < Rules.MaxTier && (best < 0 || Army[i].Tier < Army[best].Tier))
                {
                    best = i;
                }
            }
            return best;
        }

        private List<ExpeditionReward> DrawOffers()
        {
            var pool = new List<ExpeditionReward>();
            foreach (UnitDef def in Rules.PlayerClasses)
            {
                pool.Add(new ExpeditionReward { Kind = ExpeditionRewardKind.Recruit, UnitId = def.Id, Amount = Rules.RecruitAmount });
            }
            if (Army.Exists(u => u.HpFraction < 1f))
            {
                pool.Add(new ExpeditionReward { Kind = ExpeditionRewardKind.HealAll });
            }
            if (WeakestPromotable() >= 0)
            {
                pool.Add(new ExpeditionReward { Kind = ExpeditionRewardKind.Promote });
            }
            pool.Add(new ExpeditionReward { Kind = ExpeditionRewardKind.Rally, Amount = Rules.RallyAmount });

            // PB-39: one of the three is always a perk for a class in the army, the other two come from the pool.
            var perks = new List<ExpeditionReward>();
            var inArmy = new List<string>();
            foreach (ArmyUnit unit in Army)
            {
                if (!inArmy.Contains(unit.Def.Id))
                {
                    inArmy.Add(unit.Def.Id);
                }
            }
            foreach (string unitId in inArmy)
            {
                foreach (ExpeditionRewardKind kind in new[] { ExpeditionRewardKind.PerkHp, ExpeditionRewardKind.PerkDamage, ExpeditionRewardKind.PerkSpeed })
                {
                    perks.Add(new ExpeditionReward { Kind = kind, UnitId = unitId, Amount = Rules.PerkPercent });
                }
            }

            var random = new SeededRandom(Mix(Seed, 5000UL + (ulong)Step));
            var offers = new List<ExpeditionReward>();
            if (perks.Count > 0 && Rules.PerkPercent > 0)
            {
                offers.Add(perks[random.Range(0, perks.Count)]);
            }
            while (offers.Count < 3 && pool.Count > 0)
            {
                int at = random.Range(0, pool.Count);
                offers.Add(pool[at]);
                pool.RemoveAt(at);
            }
            if (offers.Count > 1)
            {
                // The perk is not always first.
                int to = random.Range(0, offers.Count);
                ExpeditionReward first = offers[0];
                offers[0] = offers[to];
                offers[to] = first;
            }
            return offers;
        }

        // ---- saving between sessions ----

        public ExpeditionSave ToSave()
        {
            var save = new ExpeditionSave
            {
                seed = Seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                step = Step,
                phase = (int)Phase,
                gold = GoldEarned,
                bonusRally = BonusRally,
            };
            foreach (ArmyUnit unit in Army)
            {
                save.armyIds.Add(unit.Def.Id);
                save.armyTiers.Add(unit.Tier);
                save.armyHp.Add((int)Math.Round(unit.HpFraction * 1000f));
            }
            foreach (ExpeditionReward offer in Offers)
            {
                save.offerKinds.Add((int)offer.Kind);
                save.offerUnits.Add(offer.UnitId ?? "");
                save.offerAmounts.Add(offer.Amount);
            }
            foreach (ExpeditionReward perk in Perks)
            {
                save.perkKinds.Add((int)perk.Kind);
                save.perkUnits.Add(perk.UnitId ?? "");
                save.perkAmounts.Add(perk.Amount);
            }
            return save;
        }

        /// <summary>Restores a saved run; null when the save is unusable (unknown classes, bad numbers).</summary>
        public static Expedition FromSave(ExpeditionSave save, ExpeditionRules rules)
        {
            if (save == null || rules == null || !ulong.TryParse(save.seed, out ulong seed))
            {
                return null;
            }
            var expedition = new Expedition(rules, seed)
            {
                Step = Math.Max(0, Math.Min(rules.Steps, save.step)),
                GoldEarned = Math.Max(0, save.gold),
                BonusRally = Math.Max(0, save.bonusRally),
                Phase = Enum.IsDefined(typeof(ExpeditionPhase), save.phase) ? (ExpeditionPhase)save.phase : ExpeditionPhase.Battle,
            };
            int count = Math.Min(save.armyIds.Count, Math.Min(save.armyTiers.Count, save.armyHp.Count));
            for (int i = 0; i < count; i++)
            {
                UnitDef def = rules.PlayerClasses.Find(u => u.Id == save.armyIds[i]);
                if (def == null)
                {
                    return null;
                }
                expedition.Army.Add(new ArmyUnit(def, Math.Max(1, Math.Min(rules.MaxTier, save.armyTiers[i])))
                {
                    HpFraction = Math.Max(0.01f, Math.Min(1f, save.armyHp[i] / 1000f)),
                });
            }
            int offers = Math.Min(save.offerKinds.Count, Math.Min(save.offerUnits.Count, save.offerAmounts.Count));
            for (int i = 0; i < offers; i++)
            {
                expedition.Offers.Add(new ExpeditionReward
                {
                    Kind = (ExpeditionRewardKind)save.offerKinds[i],
                    UnitId = save.offerUnits[i],
                    Amount = save.offerAmounts[i],
                });
            }
            int perkCount = Math.Min(save.perkKinds?.Count ?? 0, Math.Min(save.perkUnits?.Count ?? 0, save.perkAmounts?.Count ?? 0));
            for (int i = 0; i < perkCount; i++)
            {
                var perk = new ExpeditionReward
                {
                    Kind = (ExpeditionRewardKind)save.perkKinds[i],
                    UnitId = save.perkUnits[i] ?? "",
                    Amount = Math.Max(0, Math.Min(100, save.perkAmounts[i])),
                };
                if (perk.IsPerk)
                {
                    expedition.Perks.Add(perk);
                }
            }
            if (expedition.Phase == ExpeditionPhase.Reward && expedition.Offers.Count == 0)
            {
                expedition.Phase = ExpeditionPhase.Battle;
            }
            return expedition;
        }

        private static ulong Mix(ulong seed, ulong salt)
        {
            ulong hash = 1469598103934665603UL; // FNV-1a over the two numbers
            for (int i = 0; i < 8; i++)
            {
                hash = (hash ^ ((seed >> (8 * i)) & 0xFF)) * 1099511628211UL;
            }
            for (int i = 0; i < 8; i++)
            {
                hash = (hash ^ ((salt >> (8 * i)) & 0xFF)) * 1099511628211UL;
            }
            return hash;
        }
    }
}
