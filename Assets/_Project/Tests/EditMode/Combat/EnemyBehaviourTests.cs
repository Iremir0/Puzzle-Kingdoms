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
    /// <summary>PB-18 (D-051): shield goblin, shaman, wolf rider and bomber.</summary>
    public class EnemyBehaviourTests
    {
        private static BattleSimulator Sim(Formation player, Formation enemy) =>
            new BattleSimulator(CombatFixtures.CloseRules(), new ArmyRules(), player, enemy, 0);

        private static List<BattleEvent> Run(BattleSimulator sim, int ticks)
        {
            var all = new List<BattleEvent>();
            for (int i = 0; i < ticks; i++)
            {
                all.AddRange(sim.Step());
            }
            return all;
        }

        private static Formation Place(params (int x, int y, UnitDef def)[] units)
        {
            var result = new Formation();
            foreach ((int x, int y, UnitDef def) in units)
            {
                result.Place(new ArmyUnit(def), new GridPos(x, y));
            }
            return result;
        }

        private static Combatant Unit(int id, Side side, int lane, int row, Vector2 position) =>
            new Combatant(id, side, new ArmyUnit(CombatFixtures.Dummy()), new GridPos(lane, row), new ArmyRules())
            {
                Position = position,
            };

        private static UnitDef Shield(float reduction = 0.5f)
        {
            UnitDef def = CombatFixtures.Dummy(1000);
            def.Id = "shield";
            def.RangedDamageReduction = reduction;
            return def;
        }

        private static UnitDef Shaman(int heal = 60, float interval = 1f)
        {
            UnitDef def = CombatFixtures.Dummy();
            def.Id = "shaman";
            def.HealAmount = heal;
            def.HealInterval = interval;
            return def;
        }

        private static UnitDef Bomber()
        {
            UnitDef def = ArmyFixtures.UnitDef("bomber", 100, 20, 1f, 3f, 0f);
            def.SplashRadius = 1.5f;
            def.SplashFraction = 0.5f;
            return def;
        }

        private static UnitDef Rider()
        {
            UnitDef def = CombatFixtures.Dummy();
            def.TargetsBackRow = true;
            return def;
        }

        // ---- shield goblin ----

        [Test]
        public void ShieldFrontMostHalvesRangedDamage()
        {
            BattleSimulator sim = Sim(Place((1, 0, CombatFixtures.Archer())), Place((1, 0, Shield())));
            BattleEvent hit = Run(sim, 1).First(e => e.Type == BattleEventType.Attack);
            Assert.AreEqual(5, hit.Amount); // archer damage 10, reduced by 50%
        }

        [Test]
        public void ShieldTakesFullMeleeDamage()
        {
            UnitDef grunt = ArmyFixtures.UnitDef("grunt", 100, 10, 1f, 1f, 0f);
            BattleSimulator sim = Sim(Place((1, 0, grunt)), Place((1, 0, Shield())));
            sim.Combatants[0].Position = new Vector2(0f, 0.5f); // already in reach
            BattleEvent hit = Run(sim, 1).First(e => e.Type == BattleEventType.Attack && e.TargetId == 1);
            Assert.AreEqual(10, hit.Amount);
        }

        [Test]
        public void ShieldBehindAnAllyTakesFullRangedDamage()
        {
            // The blocker stands in the front row of lane 3; the shield is a row behind in lane 1
            // and is the archer's nearest target (same lane wins the tie).
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Archer())),
                Place((3, 0, CombatFixtures.Dummy()), (1, 1, Shield())));
            BattleEvent hit = Run(sim, 1).First(e => e.Type == BattleEventType.Attack && e.SourceId == 0);
            Assert.AreEqual("shield", sim.Combatants[hit.TargetId].Def.Id);
            Assert.AreEqual(10, hit.Amount);
        }

        // ---- shaman ----

        [Test]
        public void ShamanHealsTheMostHurtAllyInRange()
        {
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Dummy())),
                Place((0, 0, CombatFixtures.Dummy(100)), (1, 0, CombatFixtures.Dummy(100)), (2, 1, Shaman())));
            sim.Combatants[1].Hp = 80; // 80 %
            sim.Combatants[2].Hp = 20; // 20 %: the most hurt
            BattleEvent heal = Run(sim, 25).First(e => e.Type == BattleEventType.Heal);
            Assert.AreEqual(2, heal.TargetId);
            Assert.AreEqual(3, heal.SourceId);
            Assert.AreEqual(60, heal.Amount);
            Assert.AreEqual(80, sim.Combatants[2].Hp);
        }

        [Test]
        public void HealNeverGoesAboveMaxHp()
        {
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Dummy())),
                Place((0, 0, CombatFixtures.Dummy(100)), (2, 1, Shaman())));
            sim.Combatants[1].Hp = 95;
            BattleEvent heal = Run(sim, 25).First(e => e.Type == BattleEventType.Heal);
            Assert.AreEqual(5, heal.Amount);
            Assert.AreEqual(100, sim.Combatants[1].Hp);
        }

        [Test]
        public void ShamanDoesNotHealFullHpAlliesOrTheOtherSide()
        {
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Dummy(100))),
                Place((0, 0, CombatFixtures.Dummy(100)), (2, 1, Shaman())));
            sim.Combatants[0].Hp = 10; // a hurt player unit must not be healed
            Assert.IsEmpty(Run(sim, 60).Where(e => e.Type == BattleEventType.Heal));
            Assert.AreEqual(10, sim.Combatants[0].Hp);
        }

        [Test]
        public void ShamanOutOfRangeDoesNotHeal()
        {
            UnitDef shaman = Shaman();
            shaman.HealRange = 0.5f;
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Dummy())),
                Place((0, 0, CombatFixtures.Dummy(100)), (3, 3, shaman)));
            sim.Combatants[1].Hp = 10;
            Assert.IsEmpty(Run(sim, 60).Where(e => e.Type == BattleEventType.Heal));
        }

        [Test]
        public void HealIsRepeatedEveryInterval()
        {
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Dummy())),
                Place((0, 0, CombatFixtures.Dummy(1000)), (2, 1, Shaman(10, 1f))));
            sim.Combatants[1].Hp = 100;
            // 1 s interval at 20 ticks per second: heals at about tick 20, 40 and 60.
            Assert.AreEqual(3, Run(sim, 61).Count(e => e.Type == BattleEventType.Heal));
        }

        // ---- wolf rider ----

        [Test]
        public void WolfRiderPicksTheBackRowFirst()
        {
            Combatant wolf = CombatFixtures.Combatant(0, Side.Enemy, Rider(), Vector2.Zero);
            Combatant front = Unit(1, Side.Player, 1, 0, new Vector2(0f, -1f));
            Combatant back = Unit(2, Side.Player, 3, 3, new Vector2(5f, -5f));
            Assert.AreSame(back, Targeting.PickTarget(wolf, new[] { wolf, front, back }, 0.5f));
        }

        [Test]
        public void WolfRiderFallsBackToTheNextRowWhenTheBackRowIsDead()
        {
            Combatant wolf = CombatFixtures.Combatant(0, Side.Enemy, Rider(), Vector2.Zero);
            Combatant front = Unit(1, Side.Player, 1, 0, new Vector2(0f, -1f));
            Combatant back = Unit(2, Side.Player, 3, 3, new Vector2(5f, -5f));
            back.Hp = 0;
            Assert.AreSame(front, Targeting.PickTarget(wolf, new[] { wolf, front, back }, 0.5f));
        }

        [Test]
        public void WolfRiderTiesGoToTheNearerUnit()
        {
            Combatant wolf = CombatFixtures.Combatant(0, Side.Enemy, Rider(), Vector2.Zero);
            Combatant far = Unit(1, Side.Player, 0, 2, new Vector2(-4f, -4f));
            Combatant near = Unit(2, Side.Player, 3, 2, new Vector2(1f, -2f));
            Assert.AreSame(near, Targeting.PickTarget(wolf, new[] { wolf, far, near }, 0.5f));
        }

        // ---- bomber ----

        [Test]
        public void BomberSplashesUnitsAroundTheTarget()
        {
            BattleSimulator sim = Sim(
                Place((0, 0, CombatFixtures.Dummy()), (1, 0, CombatFixtures.Dummy()), (2, 0, CombatFixtures.Dummy()), (3, 0, CombatFixtures.Dummy())),
                Place((1, 0, Bomber())));
            List<BattleEvent> hits = Run(sim, 1).Where(e => e.Type == BattleEventType.Attack).ToList();
            Assert.AreEqual(20, hits.Single(e => e.TargetId == 1).Amount); // the target takes the full hit
            Assert.AreEqual(10, hits.Single(e => e.TargetId == 0).Amount); // neighbours take half
            Assert.AreEqual(10, hits.Single(e => e.TargetId == 2).Amount);
            Assert.IsFalse(hits.Any(e => e.TargetId == 3)); // 2.4 cells away: outside the radius
        }

        [Test]
        public void SplashRespectsArmor()
        {
            UnitDef armored = CombatFixtures.Dummy();
            armored.Armor = 0.5f;
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Dummy()), (2, 0, armored)),
                Place((1, 0, Bomber())));
            BattleEvent splash = Run(sim, 1).Single(e => e.Type == BattleEventType.Attack && e.TargetId == 1);
            Assert.AreEqual(5, splash.Amount); // 20 * 0.5 splash * 0.5 armor
        }

        [Test]
        public void NormalUnitsDoNotSplash()
        {
            BattleSimulator sim = Sim(
                Place((0, 0, CombatFixtures.Dummy()), (1, 0, CombatFixtures.Dummy())),
                Place((1, 0, CombatFixtures.Archer())));
            Assert.AreEqual(1, Run(sim, 1).Count(e => e.Type == BattleEventType.Attack));
        }

        // ---- data ----

        [Test]
        public void ScaledKeepsTheBehaviourFields()
        {
            UnitDef def = Bomber();
            def.HealAmount = 7;
            def.RangedDamageReduction = 0.3f;
            def.TargetsBackRow = true;
            UnitDef copy = def.Scaled(2f, 2f);
            Assert.AreEqual(7, copy.HealAmount);
            Assert.AreEqual(0.3f, copy.RangedDamageReduction);
            Assert.AreEqual(1.5f, copy.SplashRadius);
            Assert.IsTrue(copy.TargetsBackRow);
        }

        [Test]
        public void BattlesWithBehavioursAreDeterministic()
        {
            BattleSimulator Make() => Sim(
                Place((0, 0, CombatFixtures.Archer()), (1, 0, CombatFixtures.Archer())),
                Place((0, 0, Shield()), (1, 1, Shaman()), (2, 0, Bomber())));
            BattleResult a = Make().RunToEnd();
            BattleResult b = Make().RunToEnd();
            Assert.AreEqual(a.Ticks, b.Ticks);
            Assert.AreEqual(a.Survivors, b.Survivors);
        }
    }
}
