using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Tests.Army
{
    /// <summary>Stage-level enemy strength (D-033): HP and damage multipliers from stage data.</summary>
    public class EnemyScalingTests
    {
        private static readonly UnitDef Grunt = ArmyFixtures.UnitDef("goblin_grunt", hp: 100, damage: 10);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("goblin_archer", hp: 60, damage: 8, range: 4f);

        private static StageDef Stage(float hp = 1f, float damage = 1f)
        {
            var stage = new StageDef { Id = "stage_test", Moves = 12, EnemyHpMultiplier = hp, EnemyDamageMultiplier = damage };
            stage.Enemies.Add(new EnemySlot { Unit = Grunt, Cell = new GridPos(0, 0) });
            stage.Enemies.Add(new EnemySlot { Unit = Grunt, Cell = new GridPos(1, 0) });
            stage.Enemies.Add(new EnemySlot { Unit = Archer, Cell = new GridPos(0, 3) });
            return stage;
        }

        [Test]
        public void ScaledCopiesHpAndDamageAndKeepsTheRest()
        {
            UnitDef brute = ArmyFixtures.UnitDef("brute", hp: 160, damage: 16, interval: 1.4f, range: 1f, speed: 0.9f);
            brute.Armor = 0.5f;
            brute.Ward = 0.1f;

            UnitDef scaled = brute.Scaled(1.5f, 1.25f);

            Assert.AreEqual(240, scaled.MaxHp);
            Assert.AreEqual(20, scaled.Damage);
            Assert.AreEqual("brute", scaled.Id);
            Assert.AreEqual(1.4f, scaled.AttackInterval);
            Assert.AreEqual(0.9f, scaled.MoveSpeed);
            Assert.AreEqual(0.5f, scaled.Armor);
            Assert.AreEqual(0.1f, scaled.Ward);
            Assert.AreEqual(160, brute.MaxHp, "the original is untouched");
        }

        [Test]
        public void ScalingNeverDropsBelowOne()
        {
            UnitDef tiny = ArmyFixtures.UnitDef("tiny", hp: 1, damage: 1);
            UnitDef scaled = tiny.Scaled(0.1f, 0.1f);
            Assert.AreEqual(1, scaled.MaxHp);
            Assert.AreEqual(1, scaled.Damage);
        }

        [Test]
        public void NoMultiplierKeepsTheSameUnitDefinitions()
        {
            StageDef stage = Stage();
            Formation formation = Formation.ForStage(stage);
            Assert.AreSame(Grunt, formation.GetUnit(new GridPos(0, 0)).Def);
            Assert.AreSame(Archer, formation.GetUnit(new GridPos(0, 3)).Def);
        }

        [Test]
        public void StageMultipliersScaleEveryEnemy()
        {
            Formation formation = Formation.ForStage(Stage(hp: 1.5f, damage: 2f));
            var rules = new ArmyRules();

            ArmyUnit grunt = formation.GetUnit(new GridPos(0, 0));
            ArmyUnit archer = formation.GetUnit(new GridPos(0, 3));
            Assert.AreEqual(150, grunt.MaxHp(rules));
            Assert.AreEqual(20, grunt.Damage(rules));
            Assert.AreEqual(90, archer.MaxHp(rules));
            Assert.AreEqual(16, archer.Damage(rules));
            Assert.AreSame(formation.GetUnit(new GridPos(0, 0)).Def, formation.GetUnit(new GridPos(1, 0)).Def,
                "enemies of one type share one scaled definition");
        }

        [Test]
        public void TierStillMultipliesOnTopOfTheStageStrength()
        {
            var stage = new StageDef { Id = "t", EnemyHpMultiplier = 2f };
            stage.Enemies.Add(new EnemySlot { Unit = Grunt, Tier = 2, Cell = new GridPos(0, 0) });
            ArmyUnit unit = Formation.ForStage(stage).GetUnit(new GridPos(0, 0));
            Assert.AreEqual(360, unit.MaxHp(new ArmyRules()), "100 x 2 (stage) x 1.8 (tier 2)");
        }

        [Test]
        public void TheBattleUsesTheScaledEnemies()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
            UnitDef archer = ArmyFixtures.UnitDef("archer", 60, 10, 0.8f, 4f, 1f);
            UnitDef mage = ArmyFixtures.UnitDef("mage", 55, 22, 1.6f, 3.5f, 0.9f);
            var run = new StageRun(Stage(hp: 3f), new[] { warrior, archer, mage }, new PuzzleRules(),
                new ArmyRules(), new BattleRules(), new ProgressionRules(), new SeededRandom(3));
            while (!run.Puzzle.IsFinished)
            {
                run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b);
                run.Puzzle.PlaySwap(a, b);
            }
            AutoFormation.Fill(run.Roster);
            Assert.IsTrue(run.StartBattle());

            List<Combatant> enemies = run.Battle.Combatants.Where(c => c.Side == Side.Enemy).ToList();
            Assert.AreEqual(3, enemies.Count);
            Assert.AreEqual(300, enemies.First(c => c.Unit.Def.Id == "goblin_grunt").MaxHp);
            Assert.AreEqual(180, enemies.First(c => c.Unit.Def.Id == "goblin_archer").MaxHp);
        }

        [Test]
        public void StrongerEnemiesMakeALongerBattleWithTheSameArmies()
        {
            int ticks(float hp)
            {
                var player = new Formation();
                UnitDef warrior = ArmyFixtures.UnitDef("warrior", 400, 12, 1f, 1f, 1.2f);
                for (int x = 0; x < 4; x++)
                {
                    player.Place(new ArmyUnit(warrior), new GridPos(x, 0));
                }
                var sim = new BattleSimulator(new BattleRules(), new ArmyRules(), player, Formation.ForStage(Stage(hp: hp)), 0);
                return sim.RunToEnd().Ticks;
            }
            Assert.Greater(ticks(2f), ticks(1f));
        }
    }
}
