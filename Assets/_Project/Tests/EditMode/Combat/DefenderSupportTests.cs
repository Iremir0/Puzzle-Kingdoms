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
    /// <summary>PB-21: the Defender's taunt and the Support's heal on the player's side.</summary>
    public class DefenderSupportTests
    {
        private static UnitDef Defender(float radius = 4f)
        {
            UnitDef def = CombatFixtures.Dummy();
            def.Id = "defender";
            def.TauntRadius = radius;
            return def;
        }

        private static Combatant Unit(int id, Side side, UnitDef def, Vector2 position) =>
            new Combatant(id, side, new ArmyUnit(def), new GridPos(0, 0), new ArmyRules()) { Position = position };

        [Test]
        public void EnemiesTargetTheDefenderEvenWhenAnotherUnitIsNearer()
        {
            Combatant attacker = Unit(0, Side.Enemy, CombatFixtures.Dummy(), new Vector2(0f, 0f));
            Combatant near = Unit(1, Side.Player, CombatFixtures.Dummy(), new Vector2(0f, 1f));
            Combatant defender = Unit(2, Side.Player, Defender(), new Vector2(1f, 3f));
            var all = new List<Combatant> { attacker, near, defender };

            Assert.AreSame(defender, Targeting.PickTarget(attacker, all, 0.5f));
        }

        [Test]
        public void ADefenderBeyondItsRadiusDoesNotTaunt()
        {
            Combatant attacker = Unit(0, Side.Enemy, CombatFixtures.Dummy(), new Vector2(0f, 0f));
            Combatant near = Unit(1, Side.Player, CombatFixtures.Dummy(), new Vector2(0f, 1f));
            Combatant far = Unit(2, Side.Player, Defender(2f), new Vector2(0f, 5f));
            var all = new List<Combatant> { attacker, near, far };

            Assert.AreSame(near, Targeting.PickTarget(attacker, all, 0.5f));
        }

        [Test]
        public void TheWolfRiderIgnoresTheTaunt()
        {
            UnitDef rider = CombatFixtures.Dummy();
            rider.TargetsBackRow = true;
            Combatant attacker = Unit(0, Side.Enemy, rider, new Vector2(0f, 0f));
            Combatant defender = Unit(1, Side.Player, Defender(), new Vector2(0f, 1f));
            Combatant back = new Combatant(2, Side.Player, new ArmyUnit(CombatFixtures.Dummy()), new GridPos(0, 2), new ArmyRules())
            {
                Position = new Vector2(0f, 3f),
            };
            var all = new List<Combatant> { attacker, defender, back };

            Assert.AreSame(back, Targeting.PickTarget(attacker, all, 0.5f));
        }

        [Test]
        public void TwoDefendersAreTauntedByTheNearerOne()
        {
            Combatant attacker = Unit(0, Side.Enemy, CombatFixtures.Dummy(), new Vector2(0f, 0f));
            Combatant a = Unit(1, Side.Player, Defender(), new Vector2(2f, 2f));
            Combatant b = Unit(2, Side.Player, Defender(), new Vector2(0f, 1.5f));
            var all = new List<Combatant> { attacker, a, b };

            Assert.AreSame(b, Targeting.PickTarget(attacker, all, 0.5f));
        }

        [Test]
        public void TheSupportHealsTheMostHurtPlayerUnit()
        {
            UnitDef support = CombatFixtures.Dummy();
            support.Id = "support";
            support.HealAmount = 40;
            support.HealInterval = 1f;
            support.HealRange = 5f;
            var player = new Formation();
            player.Place(new ArmyUnit(support), new GridPos(0, 2));
            player.Place(new ArmyUnit(CombatFixtures.Dummy(500)), new GridPos(1, 0));
            var enemy = new Formation();
            enemy.Place(new ArmyUnit(CombatFixtures.Dummy(500)), new GridPos(1, 0));
            var sim = new BattleSimulator(CombatFixtures.CloseRules(), new ArmyRules(), player, enemy, 0);

            Combatant hurt = sim.Combatants.First(c => c.Side == Side.Player && c.Def.Id != "support");
            hurt.Hp = 100;
            var events = new List<BattleEvent>();
            for (int i = 0; i < 60; i++)
            {
                events.AddRange(sim.Step());
            }

            Assert.That(events.Any(e => e.Type == BattleEventType.Heal && e.TargetId == hurt.Id), "a heal event for the hurt unit");
            Assert.Greater(hurt.Hp, 100);
        }
    }
}
