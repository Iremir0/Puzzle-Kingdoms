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
    /// <summary>PB-41 (D-074): bog troll (regenerates), berserker (faster when hurt), sapper (explodes on death).</summary>
    public class Chapter3EnemyTests
    {
        private static BattleSimulator Sim(Formation player, Formation enemy) =>
            new BattleSimulator(CombatFixtures.CloseRules(), new ArmyRules(), player, enemy, 0);

        private static List<BattleEvent> Run(BattleSimulator sim, int ticks)
        {
            var all = new List<BattleEvent>();
            for (int i = 0; i < ticks && !sim.IsFinished; i++)
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

        private static int TicksPerSecond => CombatFixtures.CloseRules().TicksPerSecond;

        private static UnitDef Troll(float regen = 0.02f)
        {
            UnitDef def = CombatFixtures.Dummy(1000);
            def.Id = "troll";
            def.RegenPercent = regen;
            return def;
        }

        private static UnitDef Berserker()
        {
            UnitDef def = ArmyFixtures.UnitDef("berserker", 100, 1, 1f, 4f, 0f);
            def.EnrageBelow = 0.5f;
            def.EnrageSpeed = 2f;
            return def;
        }

        private static UnitDef Sapper()
        {
            UnitDef def = ArmyFixtures.UnitDef("sapper", 10, 10, 1f, 0.1f, 0f);
            def.DeathBurstRadius = 1.5f;
            def.DeathBurstDamage = 3f;
            return def;
        }

        // ---- bog troll ----

        [Test]
        public void ATrollRestoresAShareOfItsHpEverySecond()
        {
            BattleSimulator sim = Sim(Place((1, 0, CombatFixtures.Dummy())), Place((1, 0, Troll())));
            Combatant troll = sim.Combatants[1];
            troll.Hp = 500;
            List<BattleEvent> heals = Run(sim, 3 * TicksPerSecond).Where(e => e.Type == BattleEventType.Heal).ToList();
            Assert.AreEqual(3, heals.Count, "one per second");
            Assert.IsTrue(heals.All(h => h.SourceId == troll.Id && h.TargetId == troll.Id && h.Amount == 20), "2% of 1000");
            Assert.AreEqual(560, troll.Hp);
        }

        [Test]
        public void AHealthyTrollDoesNotRegenerateAndNeverPassesItsMax()
        {
            BattleSimulator sim = Sim(Place((1, 0, CombatFixtures.Dummy())), Place((1, 0, Troll(0.5f))));
            Assert.IsFalse(Run(sim, 2 * TicksPerSecond).Any(e => e.Type == BattleEventType.Heal), "nothing to restore");
            sim.Combatants[1].Hp = 900;
            Run(sim, 2 * TicksPerSecond);
            Assert.AreEqual(1000, sim.Combatants[1].Hp);
        }

        [Test]
        public void ATrollKilledThisTickIsNotSavedByItsRegeneration()
        {
            UnitDef troll = Troll(0.5f);
            troll.MaxHp = 10;
            BattleSimulator sim = Sim(Place((1, 0, CombatFixtures.Archer())), Place((1, 0, troll)));
            sim.Combatants[1].Hp = 5;
            sim.Combatants[1].RegenCooldown = 0f; // regenerates on the tick the arrow lands
            List<BattleEvent> events = Run(sim, 1);
            Assert.IsTrue(events.Any(e => e.Type == BattleEventType.Died && e.SourceId == 1));
            Assert.IsFalse(sim.Combatants[1].IsAlive);
        }

        // ---- berserker ----

        private static int AttacksIn(BattleSimulator sim, int seconds) =>
            Run(sim, seconds * TicksPerSecond).Count(e => e.Type == BattleEventType.Attack && e.SourceId == 1);

        [Test]
        public void ABerserkerAttacksTwiceAsFastBelowHalfHp()
        {
            BattleSimulator calm = Sim(Place((1, 0, CombatFixtures.Dummy())), Place((1, 0, Berserker())));
            int calmAttacks = AttacksIn(calm, 4);

            BattleSimulator angry = Sim(Place((1, 0, CombatFixtures.Dummy())), Place((1, 0, Berserker())));
            angry.Combatants[1].Hp = 50;
            List<BattleEvent> first = Run(angry, 1);
            Assert.AreEqual(1, first.Count(e => e.Type == BattleEventType.Enraged && e.SourceId == 1));
            int angryAttacks = 1 + AttacksIn(angry, 4);
            Assert.That(angryAttacks, Is.InRange(2 * calmAttacks - 1, 2 * calmAttacks + 1), $"{calmAttacks} calm, {angryAttacks} enraged");
            Assert.IsFalse(Run(angry, 20).Any(e => e.Type == BattleEventType.Enraged), "it is enraged only once");
        }

        [Test]
        public void AHealthyBerserkerStaysCalm()
        {
            BattleSimulator sim = Sim(Place((1, 0, CombatFixtures.Dummy())), Place((1, 0, Berserker())));
            sim.Combatants[1].Hp = 51;
            Assert.IsFalse(Run(sim, 40).Any(e => e.Type == BattleEventType.Enraged));
            Assert.IsFalse(sim.Combatants[1].Enraged);
        }

        // ---- sapper ----

        [Test]
        public void ASapperHurtsTheEnemiesNearWhereItFalls()
        {
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Archer()), (2, 0, CombatFixtures.Dummy(100))),
                Place((1, 0, Sapper())));
            Combatant archer = sim.Combatants[0];
            Combatant near = sim.Combatants[1];
            Combatant sapper = sim.Combatants[2];
            sapper.Position = new Vector2(0f, 1f);
            near.Position = new Vector2(0.6f, 0f); // about 1.2 cells away
            archer.Position = new Vector2(-1f, -2.5f); // about 3.6 cells away: safe
            List<BattleEvent> events = Run(sim, 1);

            List<BattleEvent> blasts = events.Where(e => e.Type == BattleEventType.Exploded).ToList();
            Assert.AreEqual(1, blasts.Count, "only the unit nearby");
            Assert.AreEqual(sapper.Id, blasts[0].SourceId);
            Assert.AreEqual(near.Id, blasts[0].TargetId);
            Assert.AreEqual(30, blasts[0].Amount, "three times its damage");
            Assert.AreEqual(70, near.Hp);
            Assert.AreEqual(archer.MaxHp, archer.Hp);
            int died = events.FindIndex(e => e.Type == BattleEventType.Died && e.SourceId == sapper.Id);
            Assert.Less(died, events.IndexOf(blasts[0]), "the blast follows the death");
        }

        [Test]
        public void AUnitKilledByTheBlastDiesInTheSameTick()
        {
            BattleSimulator sim = Sim(
                Place((1, 0, CombatFixtures.Archer()), (2, 0, CombatFixtures.Dummy(20))),
                Place((1, 0, Sapper())));
            sim.Combatants[2].Position = new Vector2(0f, 1f);
            sim.Combatants[1].Position = new Vector2(0.6f, 0f);
            sim.Combatants[0].Position = new Vector2(-1f, -2.5f);
            List<BattleEvent> events = Run(sim, 1);
            Assert.IsTrue(events.Any(e => e.Type == BattleEventType.Died && e.SourceId == 1));
            Assert.AreEqual(0, sim.Combatants[1].Hp);
            Assert.IsTrue(sim.IsFinished, "the sapper was the last enemy");
            Assert.IsTrue(sim.Result.Victory);
        }

        [Test]
        public void ScaledEnemiesKeepTheirChapter3Behaviours()
        {
            UnitDef def = Troll();
            def.EnrageBelow = 0.4f;
            def.EnrageSpeed = 1.5f;
            def.DeathBurstRadius = 2f;
            def.DeathBurstDamage = 4f;
            UnitDef scaled = def.Scaled(2f, 2f);
            Assert.AreEqual(def.RegenPercent, scaled.RegenPercent);
            Assert.AreEqual(def.EnrageBelow, scaled.EnrageBelow);
            Assert.AreEqual(def.EnrageSpeed, scaled.EnrageSpeed);
            Assert.AreEqual(def.DeathBurstRadius, scaled.DeathBurstRadius);
            Assert.AreEqual(def.DeathBurstDamage, scaled.DeathBurstDamage);
        }
    }
}
