using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Kingdom;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>
    /// A battle that can be played again (PB-28, D-063): the two armies as they stood at the first tick, the Rally
    /// variant and bonuses, and the ticks the player pressed Rally. The simulator is deterministic, so this is all a
    /// replay needs; it is a few hundred bytes of text. A <see cref="Stamp"/> of the rules and unit numbers tells
    /// whether the game still plays the battle the way it was fought.
    /// </summary>
    public sealed class BattleReplay
    {
        public const int FormatVersion = 1;
        private const string Header = "pk-replay";

        /// <summary>One unit of an army: class id, tier, formation cell and health share in thousandths.</summary>
        public sealed class UnitEntry
        {
            public string UnitId = "";
            public int Tier = 1;
            public int X;
            public int Y;
            public int HpPermille = 1000;
        }

        public string Title = "";
        /// <summary>Which stage the battle was in, for the arena picture.</summary>
        public string StageId = "";
        public int Chapter = 1;
        public string Stamp = "";
        public int RallyCharges;
        public RallyVariant Variant;
        public float EnemyHp = 1f;
        public float EnemyDamage = 1f;
        public readonly List<UnitEntry> Player = new List<UnitEntry>();
        public readonly List<UnitEntry> Enemy = new List<UnitEntry>();
        public readonly List<(BuildingStat stat, string unitId, float bonus)> Bonuses = new List<(BuildingStat, string, float)>();
        public readonly List<int> RallyTicks = new List<int>();
        public bool Victory;
        public int Ticks;
        public int Survivors;
        public int Deployed;

        public double Seconds(BattleRules rules) => (double)Ticks / rules.TicksPerSecond;

        /// <summary>Records a finished battle of a run. Null when the battle is not over.</summary>
        public static BattleReplay From(StageRun run, string title)
        {
            if (run == null || run.Battle == null || !run.Battle.IsFinished)
            {
                return null;
            }
            BattleSimulator battle = run.Battle;
            var replay = new BattleReplay
            {
                Title = title ?? "",
                StageId = run.Stage.Id,
                Chapter = run.Stage.Chapter,
                RallyCharges = run.RallyCharges + run.BonusRally,
                Variant = battle.Variant,
                EnemyHp = run.Stage.EnemyHpMultiplier,
                EnemyDamage = run.Stage.EnemyDamageMultiplier,
                Victory = battle.Result.Victory,
                Ticks = battle.Result.Ticks,
                Survivors = battle.Result.Survivors,
                Deployed = battle.Result.Deployed,
            };
            foreach (Combatant c in battle.Combatants)
            {
                if (c.Side == Side.Player)
                {
                    replay.Player.Add(new UnitEntry
                    {
                        UnitId = c.Def.Id,
                        Tier = c.Unit.Tier,
                        X = c.Cell.X,
                        Y = c.Cell.Y,
                        HpPermille = (int)Math.Round(c.Unit.HpFraction * 1000f),
                    });
                }
            }
            foreach (EnemySlot slot in run.Stage.Enemies)
            {
                replay.Enemy.Add(new UnitEntry { UnitId = slot.Unit.Id, Tier = slot.Tier, X = slot.Cell.X, Y = slot.Cell.Y });
            }
            replay.Bonuses.AddRange(run.Bonuses.Entries());
            replay.RallyTicks.AddRange(battle.InputLog);
            replay.Stamp = StampOf(battle.Rules, run.ArmyRules, replay, id => FindUnit(run, id));
            return replay;
        }

        private static UnitDef FindUnit(StageRun run, string id)
        {
            foreach (EnemySlot slot in run.Stage.Enemies)
            {
                if (slot.Unit.Id == id)
                {
                    return slot.Unit;
                }
            }
            foreach (Combatant c in run.Battle.Combatants)
            {
                if (c.Side == Side.Player && c.Def.Id == id)
                {
                    return c.Def;
                }
            }
            return null;
        }

        // ---- playing it again ----

        /// <summary>
        /// A simulator at tick 0 with the armies of the replay. Null when a unit is unknown. Drive it with
        /// <see cref="Advance"/> so the Rally presses happen on their ticks.
        /// </summary>
        public BattleSimulator Rebuild(BattleRules rules, ArmyRules armyRules, Func<string, UnitDef> findUnit)
        {
            var player = new Formation();
            foreach (UnitEntry entry in Player)
            {
                UnitDef def = findUnit(entry.UnitId);
                if (def == null)
                {
                    return null;
                }
                player.Place(new ArmyUnit(def, entry.Tier) { HpFraction = entry.HpPermille / 1000f }, new GridPos(entry.X, entry.Y));
            }
            var slots = new List<EnemySlot>();
            foreach (UnitEntry entry in Enemy)
            {
                UnitDef def = findUnit(entry.UnitId);
                if (def == null)
                {
                    return null;
                }
                slots.Add(new EnemySlot { Unit = def, Tier = entry.Tier, Cell = new GridPos(entry.X, entry.Y) });
            }
            var bonuses = new KingdomBonuses();
            foreach ((BuildingStat stat, string unitId, float bonus) in Bonuses)
            {
                bonuses.Add(stat, unitId, bonus);
            }
            return new BattleSimulator(rules, armyRules, player, Formation.FromEnemySlots(slots, EnemyHp, EnemyDamage),
                RallyCharges, bonuses, Variant);
        }

        /// <summary>Plays one tick of the replay: presses Rally if it was pressed now, then steps.</summary>
        public List<BattleEvent> Advance(BattleSimulator sim)
        {
            if (sim.IsFinished)
            {
                return new List<BattleEvent>();
            }
            if (RallyTicks.Contains(sim.Tick))
            {
                sim.ActivateRally();
            }
            return sim.Step();
        }

        /// <summary>True when the rules and unit numbers are the ones the battle was fought with.</summary>
        public bool IsCurrent(BattleRules rules, ArmyRules armyRules, Func<string, UnitDef> findUnit) =>
            Stamp == StampOf(rules, armyRules, this, findUnit);

        /// <summary>A fingerprint of everything outside the replay that decides the battle.</summary>
        public static string StampOf(BattleRules rules, ArmyRules armyRules, BattleReplay replay, Func<string, UnitDef> findUnit)
        {
            var text = new StringBuilder();
            Append(text, rules);
            Append(text, armyRules);
            var ids = new SortedSet<string>(StringComparer.Ordinal);
            foreach (UnitEntry entry in replay.Player)
            {
                ids.Add(entry.UnitId);
            }
            foreach (UnitEntry entry in replay.Enemy)
            {
                ids.Add(entry.UnitId);
            }
            foreach (string id in ids)
            {
                UnitDef def = findUnit(id);
                text.Append(id).Append('=');
                if (def != null)
                {
                    Append(text, def);
                }
            }
            ulong hash = 1469598103934665603UL; // FNV-1a
            foreach (char c in text.ToString())
            {
                hash = (hash ^ c) * 1099511628211UL;
            }
            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }

        private static void Append(StringBuilder text, object value)
        {
            FieldInfo[] fields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            foreach (FieldInfo field in fields)
            {
                object v = field.GetValue(value);
                if (v is float f)
                {
                    text.Append(field.Name).Append(':').Append(f.ToString("R", CultureInfo.InvariantCulture)).Append(';');
                }
                else if (v is float[] array)
                {
                    text.Append(field.Name).Append(':');
                    foreach (float item in array)
                    {
                        text.Append(item.ToString("R", CultureInfo.InvariantCulture)).Append(',');
                    }
                    text.Append(';');
                }
                else if (v is int || v is bool || v is string || v is Enum)
                {
                    text.Append(field.Name).Append(':').Append(Convert.ToString(v, CultureInfo.InvariantCulture)).Append(';');
                }
            }
        }

        // ---- text format ----

        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        public string ToText()
        {
            var text = new StringBuilder();
            text.Append(Header).Append(' ').Append(FormatVersion).Append('\n');
            text.Append("title=").Append(Title.Replace('\n', ' ')).Append('\n');
            text.Append("stamp=").Append(Stamp).Append('\n');
            text.Append("stage=").Append(StageId).Append('|').Append(Chapter).Append('\n');
            text.Append("rally=").Append(RallyCharges).Append('\n');
            RallyVariant v = Variant ?? new RallyVariant();
            text.Append("variant=").Append(v.Id).Append('|').Append((int)v.Kind).Append('|').Append(F(v.DurationSeconds)).Append('|')
                .Append(F(v.DamageBonus)).Append('|').Append(F(v.AttackSpeedBonus)).Append('|').Append(F(v.FrontRowDamageReduction)).Append('|')
                .Append(v.VolleyCount).Append('|').Append(F(v.VolleyPercentMaxHp)).Append('|').Append(F(v.BlastRadius)).Append('|')
                .Append(F(v.BlastPercentMaxHp)).Append('\n');
            text.Append("enemy-scale=").Append(F(EnemyHp)).Append('|').Append(F(EnemyDamage)).Append('\n');
            foreach (UnitEntry u in Player)
            {
                text.Append("p=").Append(u.UnitId).Append('|').Append(u.Tier).Append('|').Append(u.X).Append('|').Append(u.Y).Append('|').Append(u.HpPermille).Append('\n');
            }
            foreach (UnitEntry u in Enemy)
            {
                text.Append("e=").Append(u.UnitId).Append('|').Append(u.Tier).Append('|').Append(u.X).Append('|').Append(u.Y).Append('\n');
            }
            foreach ((BuildingStat stat, string unitId, float bonus) in Bonuses)
            {
                text.Append("bonus=").Append((int)stat).Append('|').Append(unitId).Append('|').Append(F(bonus)).Append('\n');
            }
            text.Append("input=").Append(string.Join(",", RallyTicks)).Append('\n');
            text.Append("result=").Append(Victory ? 1 : 0).Append('|').Append(Ticks).Append('|').Append(Survivors).Append('|').Append(Deployed).Append('\n');
            return text.ToString();
        }

        /// <summary>Reads a replay; null with a reason in <paramref name="error"/> when the text is not one (or is a newer format).</summary>
        public static BattleReplay Parse(string text, out string error)
        {
            error = "";
            if (string.IsNullOrEmpty(text))
            {
                error = "empty";
                return null;
            }
            string[] lines = text.Split('\n');
            string[] head = lines[0].Trim().Split(' ');
            if (head.Length != 2 || head[0] != Header || !int.TryParse(head[1], out int version))
            {
                error = "not a replay";
                return null;
            }
            if (version > FormatVersion)
            {
                error = "newer format";
                return null;
            }
            var replay = new BattleReplay();
            try
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].TrimEnd('\r');
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }
                    string key = line.Substring(0, eq);
                    string value = line.Substring(eq + 1);
                    string[] p = value.Split('|');
                    switch (key)
                    {
                        case "title": replay.Title = value; break;
                        case "stamp": replay.Stamp = value; break;
                        case "stage": replay.StageId = p[0]; replay.Chapter = p.Length > 1 ? Int(p[1]) : 1; break;
                        case "rally": replay.RallyCharges = Int(value); break;
                        case "variant":
                            replay.Variant = new RallyVariant
                            {
                                Id = p[0],
                                Kind = (RallyKind)Int(p[1]),
                                DurationSeconds = Float(p[2]),
                                DamageBonus = Float(p[3]),
                                AttackSpeedBonus = Float(p[4]),
                                FrontRowDamageReduction = Float(p[5]),
                                VolleyCount = Int(p[6]),
                                VolleyPercentMaxHp = Float(p[7]),
                                BlastRadius = Float(p[8]),
                                BlastPercentMaxHp = Float(p[9]),
                            };
                            break;
                        case "enemy-scale": replay.EnemyHp = Float(p[0]); replay.EnemyDamage = Float(p[1]); break;
                        case "p": replay.Player.Add(new UnitEntry { UnitId = p[0], Tier = Int(p[1]), X = Int(p[2]), Y = Int(p[3]), HpPermille = Int(p[4]) }); break;
                        case "e": replay.Enemy.Add(new UnitEntry { UnitId = p[0], Tier = Int(p[1]), X = Int(p[2]), Y = Int(p[3]) }); break;
                        case "bonus": replay.Bonuses.Add(((BuildingStat)Int(p[0]), p[1], Float(p[2]))); break;
                        case "input":
                            foreach (string tick in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                replay.RallyTicks.Add(Int(tick));
                            }
                            break;
                        case "result":
                            replay.Victory = Int(p[0]) == 1;
                            replay.Ticks = Int(p[1]);
                            replay.Survivors = Int(p[2]);
                            replay.Deployed = Int(p[3]);
                            break;
                    }
                }
            }
            catch (Exception e) when (e is FormatException || e is IndexOutOfRangeException || e is OverflowException)
            {
                error = "damaged";
                return null;
            }
            if (replay.Variant == null || replay.Player.Count == 0 || replay.Enemy.Count == 0)
            {
                error = "incomplete";
                return null;
            }
            return replay;
        }

        private static int Int(string s) => int.Parse(s, CultureInfo.InvariantCulture);

        private static float Float(string s) => float.Parse(s, CultureInfo.InvariantCulture);
    }
}
