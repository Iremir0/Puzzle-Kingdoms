using System.Numerics;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Combat
{
    public class DamageTests
    {
        private static Combatant Target(float armor, float ward)
        {
            UnitDef def = ArmyFixtures.UnitDef("dummy", 100, 1);
            def.Armor = armor;
            def.Ward = ward;
            return CombatFixtures.Combatant(0, Side.Enemy, def, Vector2.Zero);
        }

        [Test]
        public void PhysicalDamageIsReducedByArmor() =>
            Assert.AreEqual(8, Damage.Compute(16, DamageType.Physical, Target(0.5f, 0f), 1f));

        [Test]
        public void MagicDamageIgnoresArmorAndUsesWard()
        {
            Assert.AreEqual(22, Damage.Compute(22, DamageType.Magic, Target(0.5f, 0f), 1f), "armor ignored");
            Assert.AreEqual(15, Damage.Compute(20, DamageType.Magic, Target(0f, 0.25f), 1f), "ward applies");
        }

        [Test]
        public void MultiplierScalesDamage() =>
            Assert.AreEqual(18, Damage.Compute(12, DamageType.Physical, Target(0f, 0f), 1.5f));

        [Test]
        public void DamageIsAtLeastOne() =>
            Assert.AreEqual(1, Damage.Compute(1, DamageType.Physical, Target(0.9f, 0f), 1f));
    }
}
