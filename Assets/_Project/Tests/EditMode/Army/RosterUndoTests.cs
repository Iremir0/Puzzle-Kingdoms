using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    /// <summary>PB-9, D-038: Merge and Merge all can be taken back until the battle starts.</summary>
    public class RosterUndoTests
    {
        private readonly UnitDef _warrior = ArmyFixtures.UnitDef("warrior");
        private readonly UnitDef _archer = ArmyFixtures.UnitDef("archer");

        private static Roster MakeRoster() => new Roster(ArmyFixtures.Rules());

        /// <summary>Bench and every cell, by unit identity, so "the same units" means the same objects.</summary>
        private static List<ArmyUnit> State(Roster roster)
        {
            var state = new List<ArmyUnit>(roster.Bench);
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    state.Add(roster.Formation.GetUnit(new GridPos(x, y)));
                }
            }
            return state;
        }

        [Test]
        public void NothingToUndoIsANoOp()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 2);
            List<ArmyUnit> before = State(roster);
            int changes = 0;
            roster.Changed += () => changes++;

            Assert.IsFalse(roster.CanUndo);
            Assert.IsFalse(roster.Undo());
            CollectionAssert.AreEqual(before, State(roster));
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void DeployMovesAreNotUndoSteps()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> units = roster.Recruit(_warrior, 2);
            roster.Deploy(units[0], new GridPos(1, 0));
            roster.Undeploy(new GridPos(1, 0));

            Assert.IsFalse(roster.CanUndo);
        }

        [Test]
        public void UndoAfterMergeRestoresExactUnitsAndCells()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> warriors = roster.Recruit(_warrior, 4);
            roster.Recruit(_archer, 1);
            roster.Deploy(warriors[0], new GridPos(2, 0));
            roster.Deploy(warriors[3], new GridPos(0, 3));
            List<ArmyUnit> before = State(roster);

            Assert.IsNotNull(roster.Merge(warriors[0]));
            Assert.IsTrue(roster.CanUndo);
            int changes = 0;
            roster.Changed += () => changes++;

            Assert.IsTrue(roster.Undo());
            CollectionAssert.AreEqual(before, State(roster));
            Assert.AreEqual(1, changes, "views refresh once");
            Assert.IsFalse(roster.CanUndo);
        }

        [Test]
        public void UndoAlsoTakesBackMovesMadeAfterTheMerge()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> warriors = roster.Recruit(_warrior, 3);
            List<ArmyUnit> archers = roster.Recruit(_archer, 1);
            List<ArmyUnit> before = State(roster);

            ArmyUnit merged = roster.Merge(warriors[0]);
            roster.Deploy(merged, new GridPos(0, 0));
            roster.Deploy(archers[0], new GridPos(3, 3));

            Assert.IsTrue(roster.Undo());
            CollectionAssert.AreEqual(before, State(roster));
            Assert.AreEqual(0, roster.Formation.Count);
        }

        [Test]
        public void UndoAfterMergeAllIsOneStep()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> warriors = roster.Recruit(_warrior, 9);
            roster.Recruit(_archer, 3);
            roster.Deploy(warriors[4], new GridPos(1, 1));
            List<ArmyUnit> before = State(roster);

            Assert.AreEqual(5, roster.MergeAll().Count, "3 + 1 warrior merges, 1 archer merge");

            Assert.IsTrue(roster.Undo());
            CollectionAssert.AreEqual(before, State(roster));
            Assert.IsFalse(roster.CanUndo, "the whole Merge all was a single step");
        }

        [Test]
        public void MergeAllWithNothingToMergeAddsNoStep()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 2);

            roster.MergeAll();
            Assert.IsNull(roster.Merge(roster.Bench[0]));

            Assert.IsFalse(roster.CanUndo);
        }

        [Test]
        public void StepsUndoInReverseOrder()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 3);
            roster.Recruit(_archer, 3);
            List<ArmyUnit> start = State(roster);

            roster.Merge(roster.Bench.First(u => u.Def == _warrior));
            List<ArmyUnit> afterFirst = State(roster);
            roster.Merge(roster.Bench.First(u => u.Def == _archer));

            Assert.IsTrue(roster.Undo());
            CollectionAssert.AreEqual(afterFirst, State(roster));
            Assert.IsTrue(roster.Undo());
            CollectionAssert.AreEqual(start, State(roster));
            Assert.IsFalse(roster.Undo());
        }

        [Test]
        public void MergingAgainAfterUndoWorks()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> warriors = roster.Recruit(_warrior, 3);

            ArmyUnit first = roster.Merge(warriors[0]);
            roster.Undo();
            ArmyUnit second = roster.Merge(warriors[0]);

            Assert.IsNotNull(second);
            Assert.AreNotEqual(first.Uid, second.Uid, "uids stay unique, views can tell them apart");
            Assert.AreEqual(1, roster.AllUnits().Count);
        }

        [Test]
        public void ClearUndoForgetsEveryStep()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 3);
            roster.MergeAll();

            roster.ClearUndo();

            Assert.IsFalse(roster.CanUndo);
            Assert.IsFalse(roster.Undo());
            Assert.AreEqual(2, roster.AllUnits()[0].Tier);
        }
    }
}
