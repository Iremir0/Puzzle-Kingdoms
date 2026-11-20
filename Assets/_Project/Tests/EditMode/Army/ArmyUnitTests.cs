using NUnit.Framework;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    public class ArmyUnitTests
    {
        [Test]
        public void TierScalesHpAndDamage()
        {
            ArmyRules rules = ArmyFixtures.Rules();
            UnitDef def = ArmyFixtures.UnitDef("warrior", 120, 12);
            Assert.AreEqual(new[] { 120, 12 }, Stats(new ArmyUnit(def, 1), rules), "tier 1");
            Assert.AreEqual(new[] { 480, 48 }, Stats(new ArmyUnit(def, 2), rules), "tier 2 (x4)");
            Assert.AreEqual(new[] { 1560, 156 }, Stats(new ArmyUnit(def, 3), rules), "tier 3 (x13)");
        }

        [Test]
        public void SameKindComparesClassAndTier()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior");
            UnitDef archer = ArmyFixtures.UnitDef("archer");
            var a = new ArmyUnit(warrior, 1);
            Assert.IsTrue(a.IsSameKind(new ArmyUnit(warrior, 1)));
            Assert.IsFalse(a.IsSameKind(new ArmyUnit(warrior, 2)));
            Assert.IsFalse(a.IsSameKind(new ArmyUnit(archer, 1)));
        }

        private static int[] Stats(ArmyUnit unit, ArmyRules rules) => new[] { unit.MaxHp(rules), unit.Damage(rules) };
    }
}
