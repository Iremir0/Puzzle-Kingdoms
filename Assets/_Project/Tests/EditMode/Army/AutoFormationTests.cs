using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    public class AutoFormationTests
    {
        private readonly UnitDef _warrior = ArmyFixtures.UnitDef("warrior", range: 1f);
        private readonly UnitDef _mage = ArmyFixtures.UnitDef("mage", range: 3.5f);
        private readonly UnitDef _archer = ArmyFixtures.UnitDef("archer", range: 4f);

        [Test]
        public void PlacesUnitsByRangeCenterFirst()
        {
            var roster = new Roster(ArmyFixtures.Rules());
            roster.Recruit(_archer);
            roster.Recruit(_mage);
            roster.Recruit(_warrior);
            Assert.AreEqual(3, AutoFormation.Fill(roster));
            Assert.AreEqual("warrior", roster.Formation.GetUnit(new GridPos(1, 0)).Def.Id);
            Assert.AreEqual("mage", roster.Formation.GetUnit(new GridPos(1, 1)).Def.Id);
            Assert.AreEqual("archer", roster.Formation.GetUnit(new GridPos(1, 2)).Def.Id);
        }

        [Test]
        public void OverflowGoesToNearestRowAndExtraStaysOnBench()
        {
            var roster = new Roster(ArmyFixtures.Rules());
            roster.Recruit(_warrior, 11);
            Assert.AreEqual(9, AutoFormation.Fill(roster));
            Assert.AreEqual(2, roster.Bench.Count);
            Assert.AreEqual(0, roster.Formation.EmptyCells().Count);
        }

        [Test]
        public void HigherTiersAreDeployedFirst()
        {
            var roster = new Roster(ArmyFixtures.Rules());
            ArmyUnit veteran = roster.Merge(roster.Recruit(_warrior, 3)[0]);
            roster.Recruit(_warrior, 9);
            AutoFormation.Fill(roster);
            Assert.AreSame(veteran, roster.Formation.GetUnit(new GridPos(1, 0)), "tier 2 takes the best cell");
        }
    }
}
