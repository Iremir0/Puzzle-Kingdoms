using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    public class MergeTests
    {
        private readonly UnitDef _warrior = ArmyFixtures.UnitDef("warrior");
        private readonly UnitDef _archer = ArmyFixtures.UnitDef("archer");

        private static Roster MakeRoster() => new Roster(ArmyFixtures.Rules());

        [Test]
        public void CannotMergeWithFewerThanThree()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> units = roster.Recruit(_warrior, 2);
            roster.Recruit(_archer);
            Assert.IsFalse(roster.CanMerge(units[0]));
            Assert.IsNull(roster.Merge(units[0]));
            Assert.AreEqual(3, roster.AllUnits().Count);
        }

        [Test]
        public void MergeOnBenchCreatesNextTier()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> units = roster.Recruit(_warrior, 3);
            Assert.IsTrue(roster.CanMerge(units[0]));
            ArmyUnit merged = roster.Merge(units[0]);
            Assert.IsNotNull(merged);
            Assert.AreEqual(2, merged.Tier);
            Assert.AreSame(_warrior, merged.Def);
            CollectionAssert.AreEqual(new[] { merged }, roster.Bench);
        }

        [Test]
        public void MergedUnitKeepsDeployedTargetCell()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> units = roster.Recruit(_warrior, 3);
            roster.Deploy(units[0], new GridPos(1, 0));
            ArmyUnit merged = roster.Merge(units[0]);
            Assert.AreSame(merged, roster.Formation.GetUnit(new GridPos(1, 0)));
            Assert.AreEqual(0, roster.Bench.Count);
        }

        [Test]
        public void MergePrefersBenchCopiesOverDeployedOnes()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> units = roster.Recruit(_warrior, 4);
            roster.Deploy(units[0], new GridPos(0, 0));
            roster.Deploy(units[1], new GridPos(1, 0));
            ArmyUnit merged = roster.Merge(units[0]);
            Assert.AreSame(units[1], roster.Formation.GetUnit(new GridPos(1, 0)), "deployed copy kept");
            Assert.AreSame(merged, roster.Formation.GetUnit(new GridPos(0, 0)));
            Assert.AreEqual(0, roster.Bench.Count);
        }

        [Test]
        public void MergeIgnoresOtherTiersAndClasses()
        {
            Roster roster = MakeRoster();
            ArmyUnit tier2 = roster.Merge(roster.Recruit(_warrior, 3)[0]);
            List<ArmyUnit> fresh = roster.Recruit(_warrior, 2);
            roster.Recruit(_archer);
            Assert.IsFalse(roster.CanMerge(tier2), "only one tier 2 warrior");
            Assert.IsFalse(roster.CanMerge(fresh[0]), "only two tier 1 warriors");
        }

        [Test]
        public void CannotMergeBeyondMaxTier()
        {
            Roster roster = MakeRoster();
            var tier3 = new List<ArmyUnit>();
            for (int i = 0; i < 3; i++)
            {
                var tier2 = new List<ArmyUnit>();
                for (int j = 0; j < 3; j++)
                {
                    tier2.Add(roster.Merge(roster.Recruit(_warrior, 3)[0]));
                }
                tier3.Add(roster.Merge(tier2[0]));
            }
            Assert.AreEqual(3, tier3[0].Tier);
            Assert.IsFalse(roster.CanMerge(tier3[0]), "tier 3 is max");
            Assert.AreEqual(3, roster.AllUnits().Count);
        }
    }
}
