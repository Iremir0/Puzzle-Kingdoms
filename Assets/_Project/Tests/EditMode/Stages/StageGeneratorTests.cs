using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Stages
{
    /// <summary>PB-40 (D-073): seeded stages from a blueprint, and the bot that tunes their enemy HP.</summary>
    public class StageGeneratorTests
    {
        private static readonly UnitDef Warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("archer", 60, 10, 0.8f, 4f, 1f);
        private static readonly UnitDef Mage = ArmyFixtures.UnitDef("mage", 55, 22, 1.6f, 3.5f, 0.9f);
        private static readonly UnitDef Grunt = ArmyFixtures.UnitDef("goblin_grunt", 90, 10, 1f, 1f, 1.2f);
        private static readonly UnitDef Spear = ArmyFixtures.UnitDef("goblin_archer", 50, 9, 0.9f, 4f, 1f);
        private static readonly UnitDef Brute = ArmyFixtures.UnitDef("goblin_brute", 4000, 40, 1f, 1f, 1f);
        private static readonly UnitDef Chief = ArmyFixtures.UnitDef("goblin_chief", 400, 20, 1.2f, 1.2f, 1f);

        private static StageBlueprint Blueprint()
        {
            var blueprint = new StageBlueprint
            {
                Id = "stage_gen", DisplayName = "Generated", Chapter = 3,
                MinEnemies = 4, MaxEnemies = 6, MinTier2 = 1, MaxTier2 = 2, MinMoves = 10, MaxMoves = 12,
                Boss = Chief, GoldReward = 50,
            };
            blueprint.Required.Add(Spear);
            blueprint.Pool.Add(Grunt);
            blueprint.Pool.Add(Spear);
            return blueprint;
        }

        private static string Describe(StageDef stage) =>
            stage.Moves + ":" + string.Join(",", stage.Enemies.Select(e => $"{e.Unit.Id}/{e.Tier}@{e.Cell.X}{e.Cell.Y}"));

        [Test]
        public void TheSameSeedGivesTheSameStage()
        {
            Assert.AreEqual(Describe(StageGenerator.Generate(Blueprint(), 7)), Describe(StageGenerator.Generate(Blueprint(), 7)));
            bool anyDifferent = Enumerable.Range(8, 6)
                .Any(seed => Describe(StageGenerator.Generate(Blueprint(), (ulong)seed)) != Describe(StageGenerator.Generate(Blueprint(), 7)));
            Assert.IsTrue(anyDifferent, "other seeds give other stages");
        }

        [Test]
        public void TheArmyFollowsTheBlueprint()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                StageDef stage = StageGenerator.Generate(Blueprint(), seed);
                EnemySlot[] army = stage.Enemies.Where(e => e.Unit != Chief).ToArray();
                Assert.That(army.Length, Is.InRange(4, 6), $"seed {seed}");
                Assert.That(army.Count(e => e.Tier == 2), Is.InRange(1, 2));
                Assert.IsTrue(army.Any(e => e.Unit == Spear), "the required enemy is there");
                Assert.That(stage.Moves, Is.InRange(10, 12));
                Assert.AreEqual(3, stage.Chapter);
                Assert.AreEqual(stage.Enemies.Count, stage.Enemies.Select(e => e.Cell).Distinct().Count(), "one enemy per cell");
                Assert.IsTrue(stage.Enemies.All(e => Formation.IsInside(e.Cell)));

                EnemySlot boss = stage.Enemies.Single(e => e.Unit == Chief);
                Assert.AreEqual(Formation.Size - 1, boss.Cell.Y, "the boss stands at the back");
                int lastMeleeRow = army.Where(e => e.Unit == Grunt).Select(e => e.Cell.Y).DefaultIfEmpty(-1).Max();
                int firstRangedRow = army.Where(e => e.Unit == Spear).Select(e => e.Cell.Y).Min();
                Assert.LessOrEqual(lastMeleeRow, firstRangedRow, "melee in front of the ranged enemies");
            }
        }

        private static StageRun Run(StageDef stage, ulong seed) => new StageRun(stage, new[] { Warrior, Archer, Mage },
            new PuzzleRules(), new ArmyRules(), new BattleRules(), new ProgressionRules(), new SeededRandom(seed));

        private static StageDef Small(UnitDef enemy)
        {
            var stage = new StageDef { Id = "stage_tune", Moves = 10, GoldReward = 10 };
            for (int x = 0; x < 3; x++)
            {
                stage.Enemies.Add(new EnemySlot { Unit = enemy, Cell = new GridPos(x, 0) });
            }
            return stage;
        }

        [Test]
        public void TheTunerFindsAnHpForTheBand()
        {
            StageDef stage = Small(Brute);
            TuneResult result = StageTuner.Tune(stage, seed => Run(stage, seed), 0.3, 0.8, 30);
            Assert.IsTrue(result.Reached, $"win {result.Stats.WinRate} at {result.HpMultiplier}");
            Assert.That(result.Stats.WinRate, Is.InRange(0.3, 0.8));
            Assert.AreEqual(result.HpMultiplier, stage.EnemyHpMultiplier, "the stage keeps the result");

            StageDef again = Small(Brute);
            Assert.AreEqual(result.HpMultiplier, StageTuner.Tune(again, seed => Run(again, seed), 0.3, 0.8, 30).HpMultiplier,
                "the same seeds give the same answer");
        }

        [Test]
        public void TheTunerGivesUpCleanlyOnAStageNobodyWins()
        {
            StageDef stage = Small(ArmyFixtures.UnitDef("giant", 50000, 5000, 1f, 6f, 1f));
            TuneResult result = StageTuner.Tune(stage, seed => Run(stage, seed), 0.5, 0.8, 10, maxTries: 6);
            Assert.IsFalse(result.Reached);
            Assert.LessOrEqual(result.Tries, 6);
            Assert.AreEqual(0, result.Stats.Wins);
            Assert.Less(result.HpMultiplier, 1f, "it went looking for easier enemies");
        }
    }
}
