using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Tests.Combat;

namespace PuzzleKingdoms.Tests.Army
{
    /// <summary>PB-34 (D-068): a merged unit is worth more than the units it was made from.</summary>
    public class MergeValueTests
    {
        private static readonly UnitDef Warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("archer", 60, 10, 0.8f, 4f, 1f);

        /// <summary>One unit of <paramref name="tier"/> against three of the tier below, same class, mirrored lanes.</summary>
        private static BattleResult Duel(UnitDef def, int tier)
        {
            var one = new Formation();
            one.Place(new ArmyUnit(def, tier), new GridPos(1, 0));
            var three = new Formation();
            for (int lane = 0; lane < 3; lane++)
            {
                three.Place(new ArmyUnit(def, tier - 1), new GridPos(lane, 0));
            }
            var sim = new BattleSimulator(CombatFixtures.Rules(), new ArmyRules(), one, three, 0);
            while (!sim.IsFinished)
            {
                sim.Step();
            }
            return sim.Result;
        }

        [Test]
        public void ATierTwoBeatsTheThreeTierOnesItCosts()
        {
            Assert.IsTrue(Duel(Warrior, 2).Victory, "warrior");
            Assert.IsTrue(Duel(Archer, 2).Victory, "archer");
        }

        [Test]
        public void ATierThreeBeatsTheThreeTierTwosItCosts()
        {
            Assert.IsTrue(Duel(Warrior, 3).Victory, "warrior");
            Assert.IsTrue(Duel(Archer, 3).Victory, "archer");
        }

        [Test]
        public void EveryTierIsWorthMoreThanTheUnitsItIsMadeOf()
        {
            var rules = new ArmyRules();
            for (int tier = 2; tier <= rules.MaxTier; tier++)
            {
                Assert.Greater(rules.TierMultiplier(tier), rules.MergeCount * rules.TierMultiplier(tier - 1),
                    $"tier {tier} must beat {rules.MergeCount} of tier {tier - 1} in raw stats");
            }
        }
    }
}
