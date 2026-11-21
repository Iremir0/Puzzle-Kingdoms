using System.Numerics;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Kingdom;

namespace PuzzleKingdoms.Logic.Combat
{
    public enum Side
    {
        Player = 0,
        Enemy = 1,
    }

    /// <summary>
    /// Runtime state of one unit during a battle. Stats are fixed at spawn from
    /// the ArmyUnit and its tier; Hp, Position, TargetId and Cooldown change every tick.
    /// </summary>
    public sealed class Combatant
    {
        public const int NoTarget = -1;

        public readonly int Id;
        public readonly Side Side;
        public readonly ArmyUnit Unit;
        /// <summary>Formation cell the unit spawned from (x = lane, y = row).</summary>
        public readonly GridPos Cell;
        public int Lane => Cell.X;

        public readonly int MaxHp;
        public readonly int Damage;
        public readonly DamageType DamageType;
        public readonly float AttackInterval;
        public readonly float AttackRange;
        public readonly float MoveSpeed;
        public readonly float Armor;
        public readonly float Ward;
        public readonly UnitDef Def;

        public int Hp;
        /// <summary>Seconds until the next heal is ready (healers only).</summary>
        public float HealCooldown;
        /// <summary>Seconds until the next regeneration (PB-41, regenerating units only).</summary>
        public float RegenCooldown = 1f;
        /// <summary>True once the unit fell to <see cref="UnitDef.EnrageBelow"/> of its HP (PB-41); it stays so.</summary>
        public bool Enraged;
        public Vector2 Position;
        public int TargetId = NoTarget;
        /// <summary>Seconds until the next attack is ready; may carry a small negative rest.</summary>
        public float Cooldown;

        public Combatant(int id, Side side, ArmyUnit unit, GridPos cell, ArmyRules armyRules, KingdomBonuses bonuses = null)
        {
            Id = id;
            Side = side;
            Unit = unit;
            Cell = cell;
            UnitDef def = unit.Def;
            Def = def;
            HealCooldown = def.HealInterval;
            KingdomBonuses kingdom = side == Side.Player ? bonuses ?? KingdomBonuses.None : KingdomBonuses.None; // PB-22: the player's buildings
            if (side == Side.Enemy)
            {
                armyRules = armyRules.ForEnemies(); // D-068: enemy tiers keep their own curve
            }
            MaxHp = ScaleInt(unit.MaxHp(armyRules), kingdom.HpMultiplier(def.Id));
            Hp = side == Side.Player && unit.HpFraction < 1f ? System.Math.Max(1, (int)System.Math.Round(MaxHp * System.Math.Max(0f, unit.HpFraction), System.MidpointRounding.AwayFromZero)) : MaxHp;
            Damage = ScaleInt(unit.Damage(armyRules), kingdom.DamageMultiplier(def.Id));
            DamageType = def.DamageType;
            AttackInterval = def.AttackInterval / kingdom.AttackSpeedMultiplier(def.Id);
            AttackRange = def.AttackRange;
            MoveSpeed = def.MoveSpeed;
            Armor = def.Armor;
            Ward = def.Ward;
        }

        private static int ScaleInt(int value, float multiplier) =>
            multiplier == 1f ? value : System.Math.Max(1, (int)System.Math.Round(value * multiplier, System.MidpointRounding.AwayFromZero));

        public bool IsAlive => Hp > 0;

        public bool IsEnemyOf(Combatant other) => other.Side != Side;
    }
}
