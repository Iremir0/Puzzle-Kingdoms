using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Combat
{
    /// <summary>PB-35 (D-069): battle pace, the time-out rule and the defeat hint.</summary>
    public class BattleTempoTests
    {
        private static BattleSimulator Sim(BattleRules rules, Formation player, Formation enemy, int rally = 0) =>
            new BattleSimulator(rules, new ArmyRules(), player, enemy, rally);

        /// <summary>Two archers that shoot each other from the start: the one with more HP left wins on time.</summary>
        [Test]
        public void OnTimeTheSideWithMoreOfItsHpLeftWins()
        {
            BattleRules rules = CombatFixtures.CloseRules();
            rules.TimeoutSeconds = 3f;
            UnitDef sturdy = ArmyFixtures.UnitDef("sturdy", 1000, 10, 0.5f, 4f, 0f);
            UnitDef fragile = ArmyFixtures.UnitDef("fragile", 400, 10, 0.5f, 4f, 0f);

            BattleResult win = Sim(rules, CombatFixtures.Single(sturdy), CombatFixtures.Single(fragile)).RunToEnd();
            BattleResult loss = Sim(rules, CombatFixtures.Single(fragile), CombatFixtures.Single(sturdy)).RunToEnd();

            Assert.IsTrue(win.TimedOut);
            Assert.IsTrue(win.Victory, "same damage taken, a bigger share of HP left");
            Assert.AreEqual(1, win.Stars, "a win on time is one star");
            Assert.AreEqual(DefeatReason.None, win.Reason);
            Assert.IsTrue(loss.TimedOut);
            Assert.IsFalse(loss.Victory);
            Assert.AreEqual(DefeatReason.TimeUp, loss.Reason);
        }

        [Test]
        public void ATieOnTimeIsADefeat()
        {
            BattleRules rules = CombatFixtures.Rules();
            rules.TimeoutSeconds = 2f;
            BattleResult result = Sim(rules,
                CombatFixtures.Single(CombatFixtures.Dummy()), CombatFixtures.Single(CombatFixtures.Dummy())).RunToEnd();
            Assert.IsTrue(result.TimedOut);
            Assert.IsFalse(result.Victory);
        }

        [Test]
        public void DamageScaleMultipliesEveryHit()
        {
            UnitDef archer = ArmyFixtures.UnitDef("archer", 1000, 10, 0.5f, 4f, 0f);
            BattleRules plain = CombatFixtures.CloseRules();
            BattleRules faster = CombatFixtures.CloseRules();
            faster.DamageScale = 1.5f;

            int Hit(BattleRules rules)
            {
                BattleSimulator sim = Sim(rules, CombatFixtures.Single(archer), CombatFixtures.Single(CombatFixtures.Dummy()));
                for (int i = 0; i < 20; i++)
                {
                    foreach (BattleEvent e in sim.Step())
                    {
                        if (e.Type == BattleEventType.Attack)
                        {
                            return e.Amount;
                        }
                    }
                }
                return -1;
            }

            Assert.AreEqual(10, Hit(plain));
            Assert.AreEqual(15, Hit(faster));
        }

        [Test]
        public void ALossWithRallyLeftSaysToUseIt()
        {
            UnitDef weak = ArmyFixtures.UnitDef("weak", 40, 1, 1f, 1f, 1f);
            UnitDef strong = ArmyFixtures.UnitDef("strong", 1000, 50, 0.5f, 4f, 1f);
            BattleResult result = Sim(CombatFixtures.Rules(),
                CombatFixtures.Single(weak), CombatFixtures.Single(strong), rally: 2).RunToEnd();
            Assert.IsFalse(result.Victory);
            Assert.AreEqual(DefeatReason.RallyUnused, result.Reason);
        }

        [Test]
        public void ALossAgainstManyMoreUnitsSaysOutnumbered()
        {
            UnitDef unit = ArmyFixtures.UnitDef("unit", 100, 20, 1f, 1f, 1f);
            var enemies = new Formation();
            for (int lane = 0; lane < 4; lane++)
            {
                enemies.Place(new ArmyUnit(unit), new GridPos(lane, 0));
            }
            BattleResult result = Sim(CombatFixtures.Rules(), CombatFixtures.Single(unit), enemies).RunToEnd();
            Assert.IsFalse(result.Victory);
            Assert.AreEqual(DefeatReason.Outnumbered, result.Reason);
        }

        [Test]
        public void ALossOnStrengthSaysOverpowered()
        {
            UnitDef weak = ArmyFixtures.UnitDef("weak", 40, 1, 1f, 1f, 1f);
            UnitDef strong = ArmyFixtures.UnitDef("strong", 1000, 50, 0.5f, 4f, 1f);
            BattleResult result = Sim(CombatFixtures.Rules(),
                CombatFixtures.Single(weak), CombatFixtures.Single(strong)).RunToEnd();
            Assert.AreEqual(DefeatReason.Overpowered, result.Reason);
        }
    }
}
