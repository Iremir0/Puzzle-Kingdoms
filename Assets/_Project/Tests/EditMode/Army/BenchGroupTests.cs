using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    /// <summary>PB-9: the War Camp bench shows one tile per class and tier.</summary>
    public class BenchGroupTests
    {
        private static readonly string[] Order = { "warrior", "archer", "mage" };
        private readonly UnitDef _warrior = ArmyFixtures.UnitDef("warrior");
        private readonly UnitDef _archer = ArmyFixtures.UnitDef("archer");
        private readonly UnitDef _mage = ArmyFixtures.UnitDef("mage");

        private static string Describe(IEnumerable<BenchGroup> groups) =>
            string.Join(", ", groups.Select(g => $"{g.Def.Id} T{g.Tier} x{g.Count}"));

        [Test]
        public void EmptyBenchHasNoGroups()
        {
            Assert.IsEmpty(BenchGroup.FromBench(new List<ArmyUnit>(), Order));
        }

        [Test]
        public void GroupsByClassInOrderThenTierHighToLow()
        {
            var bench = new List<ArmyUnit>
            {
                new ArmyUnit(_mage, 1, 0),
                new ArmyUnit(_warrior, 1, 1),
                new ArmyUnit(_archer, 1, 2),
                new ArmyUnit(_warrior, 2, 3),
                new ArmyUnit(_warrior, 1, 4),
                new ArmyUnit(_mage, 3, 5),
            };

            List<BenchGroup> groups = BenchGroup.FromBench(bench, Order);

            Assert.AreEqual("warrior T2 x1, warrior T1 x2, archer T1 x1, mage T3 x1, mage T1 x1", Describe(groups));
        }

        [Test]
        public void FirstIsTheOldestBenchUnitOfTheKind()
        {
            var bench = new List<ArmyUnit>
            {
                new ArmyUnit(_archer, 1, 7),
                new ArmyUnit(_warrior, 1, 2),
                new ArmyUnit(_archer, 1, 3),
            };

            BenchGroup archers = BenchGroup.FromBench(bench, Order).Single(g => g.Def == _archer);

            Assert.AreEqual(2, archers.Count);
            Assert.AreEqual(7, archers.First.Uid, "bench order, not uid order");
        }

        [Test]
        public void UnknownClassesComeLastInBenchOrder()
        {
            UnitDef knight = ArmyFixtures.UnitDef("knight");
            UnitDef monk = ArmyFixtures.UnitDef("monk");
            var bench = new List<ArmyUnit>
            {
                new ArmyUnit(monk, 1, 0),
                new ArmyUnit(knight, 1, 1),
                new ArmyUnit(_archer, 1, 2),
            };

            Assert.AreEqual("archer T1 x1, monk T1 x1, knight T1 x1",
                Describe(BenchGroup.FromBench(bench, Order)));
            Assert.AreEqual("monk T1 x1, knight T1 x1, archer T1 x1",
                Describe(BenchGroup.FromBench(bench, null)), "no order given: bench order");
        }

        [Test]
        public void RosterBenchGroupsMatchTheRoster()
        {
            var roster = new Roster(ArmyFixtures.Rules());
            roster.Recruit(_archer, 2);
            roster.Recruit(_warrior, 4);
            roster.Deploy(roster.Bench[0], new Logic.GridPos(0, 0));

            Assert.AreEqual("warrior T1 x4, archer T1 x1", Describe(BenchGroup.FromBench(roster.Bench, Order)));
        }
    }
}
