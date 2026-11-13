using System.Numerics;
using PuzzleKingdoms.Logic.Army;

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

        public int Hp;
        public Vector2 Position;
        public int TargetId = NoTarget;
        /// <summary>Seconds until the next attack is ready; may carry a small negative rest.</summary>
        public float Cooldown;

        public Combatant(int id, Side side, ArmyUnit unit, GridPos cell, ArmyRules armyRules)
        {
            Id = id;
            Side = side;
            Unit = unit;
            Cell = cell;
            UnitDef def = unit.Def;
            MaxHp = unit.MaxHp(armyRules);
            Hp = MaxHp;
            Damage = unit.Damage(armyRules);
            DamageType = def.DamageType;
            AttackInterval = def.AttackInterval;
            AttackRange = def.AttackRange;
            MoveSpeed = def.MoveSpeed;
            Armor = def.Armor;
            Ward = def.Ward;
        }

        public bool IsAlive => Hp > 0;

        public bool IsEnemyOf(Combatant other) => other.Side != Side;
    }
}
