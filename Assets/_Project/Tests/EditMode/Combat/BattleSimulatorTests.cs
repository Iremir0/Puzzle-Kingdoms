using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Combat
{
    public class BattleSimulatorTests
    {
        private static BattleSimulator Sim(BattleRules rules, Formation player, Formation enemy, int rally = 0) =>
            new BattleSimulator(rules, new ArmyRules(), player, enemy, rally);

        private static List<BattleEvent> Run(BattleSimulator sim, int ticks)
        {
            var all = new List<BattleEvent>();
            for (int i = 0; i < ticks; i++)
            {
                all.AddRange(sim.Step());
            }
            return all;
        }

        private static List<BattleEvent> OfType(IEnumerable<BattleEvent> events, BattleEventType type) =>
            events.Where(e => e.Type == type).ToList();

        [Test]
        public void SpawnPositionsFollowFormationCells()
        {
            BattleSimulator sim = Sim(CombatFixtures.Rules(),
                CombatFixtures.Single(CombatFixtures.Dummy(), 1, 0),
                CombatFixtures.Single(CombatFixtures.Dummy(), 0, 2));
            Combatant player = sim.Combatants[0];
            Combatant enemy = sim.Combatants[1];
            Assert.AreEqual(Side.Player, player.Side);
            Assert.AreEqual(new Vector2(0f, -3f), player.Position);
            Assert.AreEqual(new Vector2(-1.2f, 5f), enemy.Position);
            Assert.AreEqual(0, enemy.Lane);
        }

        [Test]
        public void MeleeAdvancesAndStopsAtRange()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 1000, 1, 1f, 1f, 1f);
            BattleSimulator sim = Sim(CombatFixtures.Rules(),
                CombatFixtures.Single(warrior), CombatFixtures.Single(CombatFixtures.Dummy()));
            int firstAttackTick = -1;
            for (int i = 0; i < 200 && firstAttackTick < 0; i++)
            {
                if (OfType(sim.Step(), BattleEventType.Attack).Count > 0)
                {
                    firstAttackTick = sim.Tick;
                }
            }
            float distance = Vector2.Distance(sim.Combatants[0].Position, sim.Combatants[1].Position);
            Assert.That(distance, Is.InRange(0.9f, 1.001f), "stopped at range");
            // 6 cells apart, 1 cell range, 1 cell/s at 20 ticks/s: 5 cells take 100 ticks.
            Assert.That(firstAttackTick, Is.InRange(100, 102));
        }

        [Test]
        public void AttacksFollowAttackInterval()
        {
            BattleSimulator sim = Sim(CombatFixtures.CloseRules(),
                CombatFixtures.Single(CombatFixtures.Archer(0.5f)), CombatFixtures.Single(CombatFixtures.Dummy()));
            List<int> ticks = OfType(Run(sim, 100), BattleEventType.Attack).Select(e => e.Tick).ToList();
            CollectionAssert.AreEqual(new[] { 1, 11, 21, 31, 41, 51, 61, 71, 81, 91 }, ticks);
        }

        [Test]
        public void VictoryWhenEnemiesAreWipedOut()
        {
            UnitDef strong = ArmyFixtures.UnitDef("strong", 1000, 50, 0.5f, 4f, 1f);
            UnitDef weak = ArmyFixtures.UnitDef("weak", 40, 1, 1f, 1f, 1f);
            var enemy = CombatFixtures.Formation(new Dictionary<GridPos, UnitDef>
            {
                { new GridPos(0, 0), weak },
                { new GridPos(2, 0), weak },
            });
            BattleSimulator sim = Sim(CombatFixtures.Rules(), CombatFixtures.Single(strong), enemy);
            BattleResult result = sim.RunToEnd();
            Assert.IsTrue(sim.IsFinished);
            Assert.IsTrue(result.Victory);
            Assert.IsFalse(result.TimedOut);
            Assert.AreEqual(1, result.Deployed);
            Assert.AreEqual(1, result.Survivors);
            Assert.AreEqual(3, result.Stars);
        }

        [Test]
        public void DefeatWhenPlayerIsWipedOut()
        {
            UnitDef weak = ArmyFixtures.UnitDef("weak", 40, 1, 1f, 1f, 1f);
            UnitDef strong = ArmyFixtures.UnitDef("strong", 1000, 50, 0.5f, 4f, 1f);
            BattleResult result = Sim(CombatFixtures.Rules(),
                CombatFixtures.Single(weak), CombatFixtures.Single(strong)).RunToEnd();
            Assert.IsFalse(result.Victory);
            Assert.AreEqual(0, result.Survivors);
            Assert.AreEqual(0, result.Stars);
        }

        [Test]
        public void TimeoutIsADefeat()
        {
            BattleRules rules = CombatFixtures.Rules();
            rules.TimeoutSeconds = 2f;
            BattleResult result = Sim(rules,
                CombatFixtures.Single(CombatFixtures.Dummy()), CombatFixtures.Single(CombatFixtures.Dummy())).RunToEnd();
            Assert.IsTrue(result.TimedOut);
            Assert.IsFalse(result.Victory);
            Assert.AreEqual(40, result.Ticks);
        }

        [Test]
        public void MutualWipeIsADefeat()
        {
            var rules = new BattleRules { FrontGap = 1f };
            UnitDef glass = ArmyFixtures.UnitDef("glass", 5, 10, 1f, 1f, 1f);
            BattleResult result = Sim(rules, CombatFixtures.Single(glass), CombatFixtures.Single(glass)).RunToEnd();
            Assert.AreEqual(1, result.Ticks, "both hit on the first tick");
            Assert.IsFalse(result.Victory);
        }

        [Test]
        public void DeadUnitsStopActingAndEmitOneDeath()
        {
            UnitDef strong = ArmyFixtures.UnitDef("strong", 1000, 50, 0.5f, 4f, 1f);
            UnitDef weak = ArmyFixtures.UnitDef("weak", 40, 1, 1f, 4f, 1f);
            BattleSimulator sim = Sim(CombatFixtures.CloseRules(), CombatFixtures.Single(strong), CombatFixtures.Single(weak));
            List<BattleEvent> events = Run(sim, 50);
            List<BattleEvent> deaths = OfType(events, BattleEventType.Died);
            Assert.AreEqual(1, deaths.Count);
            Assert.AreEqual(1, deaths[0].SourceId, "enemy died");
            Assert.AreEqual(1, OfType(events, BattleEventType.Finished).Count);
            Assert.IsEmpty(sim.Step(), "no events after the end");
        }

        [Test]
        public void RallyBoostsPlayerDamageAndLogsInput()
        {
            BattleSimulator sim = Sim(CombatFixtures.CloseRules(),
                CombatFixtures.Single(CombatFixtures.Archer(0.5f)), CombatFixtures.Single(CombatFixtures.Dummy()), 1);
            Assert.IsTrue(sim.ActivateRally());
            Assert.IsFalse(sim.ActivateRally(), "cannot stack");
            List<BattleEvent> events = sim.Step();
            Assert.AreEqual(BattleEventType.RallyStarted, events[0].Type);
            Assert.AreEqual(15, OfType(events, BattleEventType.Attack)[0].Amount, "+50% damage");
            CollectionAssert.AreEqual(new[] { 0 }, sim.InputLog);
        }

        [Test]
        public void RallySpeedsUpAttacksAndExpires()
        {
            BattleSimulator sim = Sim(CombatFixtures.CloseRules(),
                CombatFixtures.Single(CombatFixtures.Archer(0.5f)), CombatFixtures.Single(CombatFixtures.Dummy()), 1);
            sim.ActivateRally();
            List<BattleEvent> events = Run(sim, 140);
            List<BattleEvent> ended = OfType(events, BattleEventType.RallyEnded);
            Assert.AreEqual(1, ended.Count);
            Assert.AreEqual(100, ended[0].Tick, "after 5 s");
            List<BattleEvent> attacks = OfType(events, BattleEventType.Attack);
            // 0.5 s interval at 1.5x speed: one attack every 6.67 ticks, 15 in 100 ticks.
            Assert.AreEqual(15, attacks.Count(e => e.Tick <= 100));
            Assert.AreEqual(10, attacks.First(e => e.Tick > 100).Amount, "normal damage after rally");
        }

        [Test]
        public void SameSetupSameBattle()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
            UnitDef archer = ArmyFixtures.UnitDef("archer", 60, 10, 0.8f, 4f, 1f);
            UnitDef grunt = ArmyFixtures.UnitDef("grunt", 90, 10, 1f, 1f, 1.2f);
            var logs = new List<string>();
            for (int run = 0; run < 2; run++)
            {
                var player = CombatFixtures.Formation(new Dictionary<GridPos, UnitDef>
                {
                    { new GridPos(0, 0), warrior }, { new GridPos(2, 0), warrior }, { new GridPos(1, 2), archer },
                });
                var enemy = CombatFixtures.Formation(new Dictionary<GridPos, UnitDef>
                {
                    { new GridPos(0, 0), grunt }, { new GridPos(1, 0), grunt }, { new GridPos(2, 0), grunt },
                });
                BattleSimulator sim = Sim(CombatFixtures.Rules(), player, enemy, 1);
                var log = new List<BattleEvent>();
                while (!sim.IsFinished)
                {
                    if (sim.Tick == 60)
                    {
                        sim.ActivateRally();
                    }
                    log.AddRange(sim.Step());
                }
                logs.Add(string.Join(",", log));
            }
            Assert.AreEqual(logs[0], logs[1]);
        }

        [Test]
        public void ReplayFromInputLogReproducesTheBattle()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
            UnitDef grunt = ArmyFixtures.UnitDef("grunt", 150, 10, 1f, 1f, 1.2f);
            BattleSimulator live = Sim(CombatFixtures.Rules(), CombatFixtures.Single(warrior), CombatFixtures.Single(grunt), 2);
            while (!live.IsFinished)
            {
                if (live.Tick == 30 || live.Tick == 150)
                {
                    live.ActivateRally();
                }
                live.Step();
            }
            BattleSimulator replay = Sim(CombatFixtures.Rules(), CombatFixtures.Single(warrior), CombatFixtures.Single(grunt), 2);
            BattleResult replayed = replay.RunToEnd(live.InputLog);
            Assert.AreEqual(live.Result.Ticks, replayed.Ticks);
            Assert.AreEqual(live.Result.Victory, replayed.Victory);
            Assert.AreEqual(live.Combatants[0].Hp, replay.Combatants[0].Hp);
        }

        [Test]
        public void StarsFollowSurvivorRatio()
        {
            var result = new BattleResult { Victory = true, Deployed = 4, Survivors = 4 };
            Assert.AreEqual(3, result.Stars);
            result.Survivors = 2;
            Assert.AreEqual(2, result.Stars);
            result.Survivors = 1;
            Assert.AreEqual(1, result.Stars);
            result.Victory = false;
            Assert.AreEqual(0, result.Stars);
        }
    }
}
