using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    public class RosterTests
    {
        private readonly UnitDef _warrior = ArmyFixtures.UnitDef("warrior");
        private readonly UnitDef _archer = ArmyFixtures.UnitDef("archer");

        private static Roster MakeRoster() => new Roster(ArmyFixtures.Rules());

        [Test]
        public void RecruitsGoToBenchWithUniqueIds()
        {
            Roster roster = MakeRoster();
            int changes = 0;
            roster.Changed += () => changes++;
            List<ArmyUnit> added = roster.Recruit(_warrior, 3);
            Assert.AreEqual(3, added.Count);
            Assert.AreEqual(3, roster.Bench.Count);
            Assert.AreEqual(0, roster.Formation.Count);
            Assert.IsTrue(added.All(u => u.Tier == 1));
            Assert.AreEqual(3, added.Select(u => u.Uid).Distinct().Count());
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void DeployMovesUnitFromBenchToFormation()
        {
            Roster roster = MakeRoster();
            ArmyUnit unit = roster.Recruit(_warrior)[0];
            Assert.IsTrue(roster.Deploy(unit, new GridPos(1, 0)));
            Assert.AreEqual(0, roster.Bench.Count);
            Assert.AreSame(unit, roster.Formation.GetUnit(new GridPos(1, 0)));
        }

        [Test]
        public void DeployOntoOccupiedCellBenchesPreviousUnit()
        {
            Roster roster = MakeRoster();
            ArmyUnit a = roster.Recruit(_warrior)[0];
            ArmyUnit b = roster.Recruit(_archer)[0];
            roster.Deploy(a, new GridPos(0, 0));
            roster.Deploy(b, new GridPos(0, 0));
            Assert.AreSame(b, roster.Formation.GetUnit(new GridPos(0, 0)));
            CollectionAssert.AreEqual(new[] { a }, roster.Bench);
        }

        [Test]
        public void DeployOfDeployedUnitMovesIt()
        {
            Roster roster = MakeRoster();
            ArmyUnit a = roster.Recruit(_warrior)[0];
            roster.Deploy(a, new GridPos(0, 0));
            Assert.IsTrue(roster.Deploy(a, new GridPos(2, 2)));
            Assert.IsNull(roster.Formation.GetUnit(new GridPos(0, 0)));
            Assert.AreSame(a, roster.Formation.GetUnit(new GridPos(2, 2)));
        }

        [Test]
        public void DeployRejectsUnknownUnitAndBadCell()
        {
            Roster roster = MakeRoster();
            Assert.IsFalse(roster.Deploy(new ArmyUnit(_warrior), new GridPos(0, 0)), "unknown unit");
            ArmyUnit unit = roster.Recruit(_warrior)[0];
            Assert.IsFalse(roster.Deploy(unit, new GridPos(Formation.Size, Formation.Size)), "outside grid");
            Assert.AreEqual(1, roster.Bench.Count);
        }

        [Test]
        public void UndeployReturnsUnitToBench()
        {
            Roster roster = MakeRoster();
            ArmyUnit unit = roster.Recruit(_warrior)[0];
            roster.Deploy(unit, new GridPos(1, 1));
            Assert.IsTrue(roster.Undeploy(new GridPos(1, 1)));
            Assert.AreEqual(1, roster.Bench.Count);
            Assert.IsFalse(roster.Undeploy(new GridPos(1, 1)));
        }

        [Test]
        public void AllUnitsCountsBenchAndFormation()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> units = roster.Recruit(_warrior, 4);
            roster.Deploy(units[0], new GridPos(0, 0));
            roster.Deploy(units[1], new GridPos(1, 0));
            Assert.AreEqual(4, roster.AllUnits().Count);
        }
    }
}
