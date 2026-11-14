using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    public class FormationTests
    {
        private static ArmyUnit Unit(string id = "warrior") => new ArmyUnit(ArmyFixtures.UnitDef(id), 1);

        [Test]
        public void NewFormationIsEmpty()
        {
            var formation = new Formation();
            Assert.AreEqual(0, formation.Count);
            Assert.AreEqual(Formation.Size * Formation.Size, formation.EmptyCells().Count);
            Assert.IsNull(formation.GetUnit(new GridPos(1, 1)));
        }

        [Test]
        public void PlaceAndRemove()
        {
            var formation = new Formation();
            ArmyUnit unit = Unit();
            Assert.IsNull(formation.Place(unit, new GridPos(2, 0)));
            Assert.AreSame(unit, formation.GetUnit(new GridPos(2, 0)));
            Assert.AreEqual(new GridPos(2, 0), formation.FindCell(unit));
            Assert.AreSame(unit, formation.Remove(new GridPos(2, 0)));
            Assert.AreEqual(0, formation.Count);
            Assert.AreEqual(Formation.NoCell, formation.FindCell(unit));
        }

        [Test]
        public void PlaceOnOccupiedCellReturnsDisplacedUnit()
        {
            var formation = new Formation();
            ArmyUnit first = Unit();
            ArmyUnit second = Unit("archer");
            formation.Place(first, new GridPos(0, 0));
            Assert.AreSame(first, formation.Place(second, new GridPos(0, 0)));
            Assert.AreSame(second, formation.GetUnit(new GridPos(0, 0)));
            Assert.AreEqual(1, formation.Count);
        }

        [Test]
        public void MoveSwapsCells()
        {
            var formation = new Formation();
            ArmyUnit a = Unit();
            ArmyUnit b = Unit("archer");
            formation.Place(a, new GridPos(0, 0));
            formation.Place(b, new GridPos(0, 2));
            formation.Move(new GridPos(0, 0), new GridPos(0, 2));
            Assert.AreSame(a, formation.GetUnit(new GridPos(0, 2)));
            Assert.AreSame(b, formation.GetUnit(new GridPos(0, 0)));
            formation.Move(new GridPos(0, 0), new GridPos(1, 1));
            Assert.AreSame(b, formation.GetUnit(new GridPos(1, 1)));
            Assert.IsNull(formation.GetUnit(new GridPos(0, 0)));
        }

        [Test]
        public void CellsOutsideGridAreRejected()
        {
            Assert.IsFalse(Formation.IsInside(new GridPos(Formation.Size, 0)));
            Assert.IsFalse(Formation.IsInside(new GridPos(0, -1)));
            Assert.IsTrue(Formation.IsInside(new GridPos(Formation.Size - 1, Formation.Size - 1)));
        }

        [Test]
        public void UnitsListedFrontToBackLeftToRight()
        {
            var formation = new Formation();
            ArmyUnit back = Unit();
            ArmyUnit frontRight = Unit();
            ArmyUnit frontLeft = Unit();
            formation.Place(back, new GridPos(0, 2));
            formation.Place(frontRight, new GridPos(2, 0));
            formation.Place(frontLeft, new GridPos(0, 0));
            CollectionAssert.AreEqual(new List<ArmyUnit> { frontLeft, frontRight, back }, formation.Units());
        }
    }
}
