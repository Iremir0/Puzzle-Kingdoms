using System.Numerics;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Combat
{
    public class TargetingTests
    {
        private readonly UnitDef _def = ArmyFixtures.UnitDef("dummy", 100, 10);

        private Combatant Enemy(int id, float y, int lane, float x = 0f) =>
            CombatFixtures.Combatant(id, Side.Enemy, _def, new Vector2(x, y), lane);

        private Combatant Attacker(int lane = 1) =>
            CombatFixtures.Combatant(0, Side.Player, _def, Vector2.Zero, lane);

        [Test]
        public void PicksNearestEnemy()
        {
            Combatant near = Enemy(1, 2f, 0);
            Combatant far = Enemy(2, 5f, 1);
            Assert.AreSame(near, Targeting.PickTarget(Attacker(), new[] { far, near }, 0.5f));
        }

        [Test]
        public void SameLaneWinsTiesWithinTolerance()
        {
            Combatant otherLane = Enemy(1, 3.0f, 0);
            Combatant sameLane = Enemy(2, 3.4f, 1);
            Assert.AreSame(sameLane, Targeting.PickTarget(Attacker(1), new[] { otherLane, sameLane }, 0.5f));
        }

        [Test]
        public void SameLaneLosesBeyondTolerance()
        {
            Combatant otherLane = Enemy(1, 3.0f, 0);
            Combatant sameLane = Enemy(2, 3.6f, 1);
            Assert.AreSame(otherLane, Targeting.PickTarget(Attacker(1), new[] { otherLane, sameLane }, 0.5f));
        }

        [Test]
        public void LowerIdBreaksExactTies()
        {
            Combatant a = Enemy(5, 2f, 0, -1f);
            Combatant b = Enemy(3, 2f, 2, 1f);
            Assert.AreSame(b, Targeting.PickTarget(Attacker(1), new[] { a, b }, 0.5f));
        }

        [Test]
        public void IgnoresDeadAndReturnsNullWhenNone()
        {
            Combatant dead = Enemy(1, 1f, 1);
            dead.Hp = 0;
            Combatant alive = Enemy(2, 4f, 1);
            Assert.AreSame(alive, Targeting.PickTarget(Attacker(), new[] { dead, alive }, 0.5f));
            alive.Hp = 0;
            Assert.IsNull(Targeting.PickTarget(Attacker(), new[] { dead, alive }, 0.5f));
        }
    }
}
