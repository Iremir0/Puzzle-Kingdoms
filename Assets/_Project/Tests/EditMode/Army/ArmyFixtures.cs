using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    /// <summary>Builders with in-code values, so tests do not depend on balancing data.</summary>
    internal static class ArmyFixtures
    {
        public static UnitDef UnitDef(
            string id,
            int hp = 100,
            int damage = 10,
            float interval = 1f,
            float range = 1f,
            float speed = 1f)
        {
            return new UnitDef
            {
                Id = id,
                DisplayName = id,
                MaxHp = hp,
                Damage = damage,
                AttackInterval = interval,
                AttackRange = range,
                MoveSpeed = speed,
            };
        }

        public static ArmyRules Rules() => new ArmyRules();
    }
}
