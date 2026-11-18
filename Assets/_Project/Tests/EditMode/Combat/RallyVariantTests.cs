using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Combat
{
    /// <summary>PB-23 (D-053): the commanders' Rally variants.</summary>
    public class RallyVariantTests
    {
        private static BattleSimulator Sim(Formation player, Formation enemy, RallyVariant variant, int charges = 1) =>
            new BattleSimulator(CombatFixtures.CloseRules(), new ArmyRules(), player, enemy, charges, null, variant);

        private static Formation Place(params (int x, int y, UnitDef def)[] units)
        {
            var result = new Formation();
            foreach ((int x, int y, UnitDef def) in units)
            {
                result.Place(new ArmyUnit(def), new GridPos(x, y));
            }
            return result;
        }

        private static List<BattleEvent> Run(BattleSimulator sim, int ticks)
        {
            var all = new List<BattleEvent>();
            for (int i = 0; i < ticks; i++)
            {
                all.AddRange(sim.Step());
            }
            return all;
        }

        private static UnitDef Tank(int hp = 1000) => ArmyFixtures.UnitDef("tank", hp, 1, 1f, 0.1f, 0f);

        private static UnitDef Hitter(int damage = 100) => ArmyFixtures.UnitDef("hitter", 100000, damage, 1f, 1f, 0f);

        // ---- classic and defaults ----

        [Test]
        public void NoVariantIsTheClassicRallyFromTheRules()
        {
            var sim = Sim(Place((1, 0, Tank())), Place((1, 0, Tank())), null);
            Assert.AreEqual(RallyKind.Classic, sim.Variant.Kind);
            Assert.AreEqual(sim.Rules.RallyDamageBonus, sim.Variant.DamageBonus);
            Assert.AreEqual(sim.Rules.SecondsToTicks(sim.Rules.RallyDurationSeconds), sim.Rally.DurationTicks);
        }

        [Test]
        public void ClassicRallyStillBuffsDamage()
        {
            var sim = Sim(Place((1, 0, Hitter(100))), Place((1, 0, Tank(100000))), null);
            sim.Combatants[0].Position = new System.Numerics.Vector2(-0.6f, 0.5f);
            sim.ActivateRally();
            BattleEvent hit = Run(sim, 1).First(e => e.Type == BattleEventType.Attack && e.SourceId == 0);
            Assert.AreEqual(150, hit.Amount); // +50 %
        }

        [Test]
        public void AVariantCanChangeTheBuffAndTheDuration()
        {
            var variant = new RallyVariant { DamageBonus = 1f, AttackSpeedBonus = 0f, DurationSeconds = 2f };
            var sim = Sim(Place((1, 0, Hitter(100))), Place((1, 0, Tank(100000))), variant);
            Assert.AreEqual(40, sim.Rally.DurationTicks);
            sim.Combatants[0].Position = new System.Numerics.Vector2(-0.6f, 0.5f);
            sim.ActivateRally();
            Assert.AreEqual(200, Run(sim, 1).First(e => e.Type == BattleEventType.Attack && e.SourceId == 0).Amount);
        }

        // ---- Shield Wall ----

        private static RallyVariant ShieldWall(float reduction = 0.5f) => new RallyVariant
        {
            Kind = RallyKind.ShieldWall,
            DurationSeconds = 4f,
            DamageBonus = 0f,
            AttackSpeedBonus = 0f,
            FrontRowDamageReduction = reduction,
        };

        [Test]
        public void ShieldWallHalvesDamageOnTheFrontRowOnly()
        {
            // An enemy hitter reaches a front-row tank and a back-row tank at once.
            UnitDef hitter = ArmyFixtures.UnitDef("hitter", 100000, 100, 1f, 4f, 0f);
            var sim = Sim(Place((1, 0, Tank()), (1, 1, Tank())), Place((1, 0, hitter)), ShieldWall());
            sim.ActivateRally();
            List<BattleEvent> events = Run(sim, 1);
            BattleEvent first = events.First(e => e.Type == BattleEventType.Attack && e.SourceId == 2);
            Assert.AreEqual(0, sim.Combatants[first.TargetId].Cell.Y, "the nearest player unit is the front one");
            Assert.AreEqual(50, first.Amount);
        }

        [Test]
        public void ShieldWallDoesNothingWithoutRallyActive()
        {
            UnitDef hitter = ArmyFixtures.UnitDef("hitter", 100000, 100, 1f, 4f, 0f);
            var sim = Sim(Place((1, 0, Tank())), Place((1, 0, hitter)), ShieldWall(), 0);
            Assert.AreEqual(100, Run(sim, 1).First(e => e.Type == BattleEventType.Attack && e.SourceId == 1).Amount);
        }

        [Test]
        public void ShieldWallEndsWithTheRally()
        {
            UnitDef hitter = ArmyFixtures.UnitDef("hitter", 100000, 100, 0.5f, 4f, 0f);
            var sim = Sim(Place((1, 0, Tank(100000))), Place((1, 0, hitter)), ShieldWall());
            sim.ActivateRally();
            List<BattleEvent> events = Run(sim, 100); // 5 s: 4 s of wall, then none
            var hits = events.Where(e => e.Type == BattleEventType.Attack).ToList();
            Assert.AreEqual(50, hits.First().Amount);
            Assert.AreEqual(100, hits.Last().Amount);
        }

        [Test]
        public void EnemyFrontRowIsNotShielded()
        {
            UnitDef hitter = ArmyFixtures.UnitDef("hitter", 100000, 100, 1f, 4f, 0f);
            var sim = Sim(Place((1, 0, hitter)), Place((1, 0, Tank())), ShieldWall());
            sim.ActivateRally();
            Assert.AreEqual(100, Run(sim, 1).First(e => e.Type == BattleEventType.Attack && e.SourceId == 0).Amount);
        }

        // ---- Arrow Rain ----

        private static RallyVariant ArrowRain(int volleys = 5, float percent = 0.1f) => new RallyVariant
        {
            Kind = RallyKind.ArrowRain,
            DurationSeconds = 5f,
            DamageBonus = 0f,
            AttackSpeedBonus = 0f,
            VolleyCount = volleys,
            VolleyPercentMaxHp = percent,
        };

        [Test]
        public void ArrowRainHitsOnlyTheBackRowInEvenVolleys()
        {
            var sim = Sim(Place((1, 0, Tank())),
                Place((0, 0, Tank(1000)), (1, 2, Tank(1000)), (2, 3, Tank(1000)), (3, 3, Tank(1000))), ArrowRain());
            sim.ActivateRally();
            List<BattleEvent> strikes = Run(sim, 100).Where(e => e.Type == BattleEventType.RallyStrike).ToList();
            Assert.AreEqual(10, strikes.Count, "5 volleys on 2 back-row units");
            Assert.IsTrue(strikes.All(s => sim.Combatants[s.TargetId].Cell.Y == 3));
            Assert.IsTrue(strikes.All(s => s.Amount == 100 && s.SourceId == Combatant.NoTarget));
            Assert.AreEqual(5, strikes.Select(s => s.Tick).Distinct().Count());
            Assert.AreEqual(1000, sim.Combatants[1].Hp, "the front enemy was spared");
            Assert.AreEqual(500, sim.Combatants[3].Hp);
        }

        [Test]
        public void ArrowRainMovesToTheNextRowWhenTheBackRowIsDead()
        {
            var sim = Sim(Place((1, 0, Tank())), Place((0, 0, Tank(1000)), (2, 3, Tank(40))), ArrowRain(5, 0.5f));
            sim.ActivateRally();
            List<BattleEvent> strikes = Run(sim, 100).Where(e => e.Type == BattleEventType.RallyStrike).ToList();
            Assert.IsTrue(strikes.Any(s => sim.Combatants[s.TargetId].Cell.Y == 0), "later volleys reach row 0");
            Assert.AreEqual(0, sim.Combatants[2].Hp);
        }

        [Test]
        public void ArrowRainIgnoresArmor()
        {
            UnitDef armored = Tank(1000);
            armored.Armor = 0.9f;
            var sim = Sim(Place((1, 0, Tank())), Place((1, 3, armored)), ArrowRain(1, 0.1f));
            sim.ActivateRally();
            Assert.AreEqual(100, Run(sim, 1).First(e => e.Type == BattleEventType.RallyStrike).Amount);
        }

        [Test]
        public void StrikesLeaveAtLeastOneDamageAndCanKill()
        {
            var sim = Sim(Place((1, 0, Tank())), Place((1, 3, Tank(5))), ArrowRain(1, 0.001f));
            sim.ActivateRally();
            List<BattleEvent> events = Run(sim, 1);
            Assert.AreEqual(1, events.First(e => e.Type == BattleEventType.RallyStrike).Amount);
            Assert.AreEqual(4, sim.Combatants[1].Hp);
        }

        // ---- Fireball ----

        private static RallyVariant Fireball(float radius = 1.5f, float percent = 0.3f) => new RallyVariant
        {
            Kind = RallyKind.Fireball,
            DurationSeconds = 3f,
            DamageBonus = 0f,
            AttackSpeedBonus = 0f,
            BlastRadius = radius,
            BlastPercentMaxHp = percent,
        };

        [Test]
        public void FireballBlastsTheDensestGroupOnce()
        {
            // Lanes 0, 1, 2 are 1.2 apart (a group); lane 3 is alone, 3 rows back.
            var sim = Sim(Place((1, 0, Tank())),
                Place((0, 0, Tank(1000)), (1, 0, Tank(1000)), (2, 0, Tank(1000)), (3, 3, Tank(1000))), Fireball());
            sim.ActivateRally();
            List<BattleEvent> strikes = Run(sim, 60).Where(e => e.Type == BattleEventType.RallyStrike).ToList();
            Assert.AreEqual(3, strikes.Count, "one blast, three units caught");
            Assert.IsTrue(strikes.All(s => s.Amount == 300));
            Assert.AreEqual(1000, sim.Combatants[4].Hp, "the loner is spared");
        }

        [Test]
        public void FireballTiesGoToTheLowerId()
        {
            var sim = Sim(Place((1, 0, Tank())), Place((0, 0, Tank(1000)), (3, 3, Tank(1000))), Fireball());
            sim.ActivateRally();
            List<BattleEvent> strikes = Run(sim, 1).Where(e => e.Type == BattleEventType.RallyStrike).ToList();
            Assert.AreEqual(1, strikes.Count);
            Assert.AreEqual(1, strikes[0].TargetId);
        }

        [Test]
        public void FireballWithNoEnemiesDoesNothing()
        {
            var sim = Sim(Place((1, 0, Tank())), Place((1, 0, Tank(1))), Fireball());
            sim.Combatants[1].Hp = 0;
            sim.ActivateRally();
            Assert.DoesNotThrow(() => Run(sim, 2));
        }

        // ---- replay and determinism ----

        [Test]
        public void BattlesWithVariantsReplayExactly()
        {
            foreach (RallyVariant variant in new[] { ShieldWall(), ArrowRain(), Fireball() })
            {
                BattleSimulator Make() => Sim(
                    Place((0, 0, Hitter(40)), (1, 0, Hitter(40)), (2, 1, CombatFixtures.Archer())),
                    Place((0, 0, Tank(900)), (1, 0, Tank(900)), (2, 2, Tank(900)), (3, 3, Tank(900))), variant, 2);
                var live = Make();
                live.Step();
                live.ActivateRally();
                BattleResult first = live.RunToEnd();
                var replay = Make();
                BattleResult second = replay.RunToEnd(live.InputLog);
                Assert.AreEqual(first.Ticks, second.Ticks, variant.Kind.ToString());
                Assert.AreEqual(first.Survivors, second.Survivors, variant.Kind.ToString());
                for (int i = 0; i < live.Combatants.Count; i++)
                {
                    Assert.AreEqual(live.Combatants[i].Hp, replay.Combatants[i].Hp, variant.Kind + " unit " + i);
                }
            }
        }
    }
}
