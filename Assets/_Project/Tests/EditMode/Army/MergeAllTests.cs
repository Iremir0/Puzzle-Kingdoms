using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Tests.Army
{
    public class MergeAllTests
    {
        private readonly UnitDef _warrior = ArmyFixtures.UnitDef("warrior");
        private readonly UnitDef _archer = ArmyFixtures.UnitDef("archer");
        private readonly UnitDef _mage = ArmyFixtures.UnitDef("mage");

        private static Roster MakeRoster() => new Roster(ArmyFixtures.Rules());

        private static int Count(Roster roster, UnitDef def, int tier) =>
            roster.AllUnits().Count(u => u.Def == def && u.Tier == tier);

        [Test]
        public void NothingToMergeDoesNothing()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 2);
            roster.Recruit(_archer, 2);

            Assert.IsEmpty(roster.PlanMergeAll());
            Assert.IsFalse(roster.CanMergeAny);
            Assert.IsEmpty(roster.MergeAll());
            Assert.AreEqual(4, roster.AllUnits().Count);
        }

        [Test]
        public void MergesEveryClassThatHasThree()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 7);
            roster.Recruit(_archer, 3);
            roster.Recruit(_mage, 2);

            List<ArmyUnit> created = roster.MergeAll();

            Assert.AreEqual(3, created.Count);
            Assert.AreEqual(2, Count(roster, _warrior, 2));
            Assert.AreEqual(1, Count(roster, _warrior, 1));
            Assert.AreEqual(1, Count(roster, _archer, 2));
            Assert.AreEqual(2, Count(roster, _mage, 1));
        }

        [Test]
        public void MergesChainIntoHigherTiers()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 9);

            roster.MergeAll();

            Assert.AreEqual(1, roster.AllUnits().Count);
            Assert.AreEqual(1, Count(roster, _warrior, 3));
        }

        [Test]
        public void MaxTierUnitsAreNotMergedFurther()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 27);

            roster.MergeAll();

            Assert.AreEqual(3, roster.AllUnits().Count, "27 -> 9 -> 3 units of the highest tier");
            Assert.AreEqual(3, Count(roster, _warrior, 3));
            Assert.IsEmpty(roster.PlanMergeAll());
        }

        [Test]
        public void ADeployedUnitIsTheMergeTargetSoItKeepsItsCell()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> units = roster.Recruit(_warrior, 4);
            roster.Deploy(units[2], new GridPos(2, 1));

            roster.MergeAll();

            ArmyUnit atCell = roster.Formation.GetUnit(new GridPos(2, 1));
            Assert.IsNotNull(atCell);
            Assert.AreEqual(2, atCell.Tier, "the merged unit took the deployed unit's place");
            Assert.AreEqual(1, roster.Bench.Count, "the fourth copy is left on the bench");
            Assert.AreEqual(1, roster.Formation.Count);
        }

        [Test]
        public void BenchCopiesAreConsumedBeforeDeployedOnes()
        {
            Roster roster = MakeRoster();
            List<ArmyUnit> units = roster.Recruit(_warrior, 4);
            roster.Deploy(units[0], new GridPos(0, 0));
            roster.Deploy(units[1], new GridPos(1, 0));

            roster.MergeAll();

            Assert.AreEqual(2, roster.Formation.Count, "both deployed copies are still in the formation");
            Assert.AreEqual(0, roster.Bench.Count, "the two bench copies were consumed");
            Assert.AreEqual(1, Count(roster, _warrior, 2));
            Assert.AreEqual(1, Count(roster, _warrior, 1));
        }

        [Test]
        public void ThePlanSaysWhatWillHappen()
        {
            Roster roster = MakeRoster();
            roster.Recruit(_warrior, 9);
            roster.Recruit(_archer, 4);

            List<MergePlanEntry> plan = roster.PlanMergeAll();

            CollectionAssert.AreEqual(
                new[] { "warrior:1:3", "archer:1:1", "warrior:2:1" },
                plan.Select(p => $"{p.Def.Id}:{p.FromTier}:{p.Merges}").ToArray());
            Assert.AreEqual(2, plan[0].ToTier);
        }

        [Test]
        public void ThePlanMatchesWhatMergeAllReallyDoes()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var random = new SeededRandom(seed);
                Roster roster = MakeRoster();
                foreach (UnitDef def in new[] { _warrior, _archer, _mage })
                {
                    roster.Recruit(def, random.Range(0, 14));
                }
                // Deploy a few at random so deployed units take part.
                List<ArmyUnit> all = roster.AllUnits();
                for (int i = 0; i < 5 && i < all.Count; i++)
                {
                    roster.Deploy(all[random.Range(0, all.Count)], new GridPos(i % 3, i / 3));
                }

                int before = roster.AllUnits().Count;
                int mergesPlanned = roster.PlanMergeAll().Sum(p => p.Merges);
                List<ArmyUnit> created = roster.MergeAll();

                Assert.AreEqual(mergesPlanned, created.Count, $"seed {seed}");
                Assert.AreEqual(before - 2 * mergesPlanned, roster.AllUnits().Count, $"seed {seed}");
                Assert.IsEmpty(roster.PlanMergeAll(), $"nothing left to merge (seed {seed})");
            }
        }
    }
}
