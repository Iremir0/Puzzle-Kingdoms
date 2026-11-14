using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Stages;
using UnityEditor;

namespace PuzzleKingdoms.Tests.Data
{
    /// <summary>Validates the shipped assets against docs/GDD.md and basic invariants. Runs in Unity only.</summary>
    public class GameDataTests
    {
        private const string Root = "Assets/_Project/Data/";

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Root + path);
            Assert.IsNotNull(asset, $"missing asset {path}");
            return asset;
        }

        private static IEnumerable<UnitDefinition> AllUnits() =>
            AssetDatabase.FindAssets("t:UnitDefinition", new[] { Root })
                .Select(guid => AssetDatabase.LoadAssetAtPath<UnitDefinition>(AssetDatabase.GUIDToAssetPath(guid)));

        [Test]
        public void AllUnitsHaveSaneValues()
        {
            List<UnitDefinition> units = AllUnits().ToList();
            Assert.AreEqual(7, units.Count, "3 player classes + 4 goblins");
            foreach (UnitDefinition asset in units)
            {
                UnitDef def = asset.Def;
                Assert.IsNotEmpty(def.Id, asset.name);
                Assert.Greater(def.MaxHp, 0, def.Id);
                Assert.Greater(def.Damage, 0, def.Id);
                Assert.Greater(def.AttackInterval, 0f, def.Id);
                Assert.Less(def.Armor, 1f, def.Id);
                Assert.Less(def.Ward, 1f, def.Id);
            }
        }

        [Test]
        public void PlayerUnitsMatchGdd()
        {
            UnitDef warrior = Load<UnitDefinition>("Units/Warrior.asset").Def;
            UnitDef archer = Load<UnitDefinition>("Units/Archer.asset").Def;
            UnitDef mage = Load<UnitDefinition>("Units/Mage.asset").Def;
            Assert.AreEqual(new[] { 504, 12 }, new[] { warrior.MaxHp, warrior.Damage });
            Assert.AreEqual(new[] { 252, 10 }, new[] { archer.MaxHp, archer.Damage });
            Assert.AreEqual(4f, archer.AttackRange);
            Assert.AreEqual(new[] { 231, 22 }, new[] { mage.MaxHp, mage.Damage });
            Assert.AreEqual(DamageType.Magic, mage.DamageType);
        }

        [Test]
        public void ArmyRulesMatchGdd()
        {
            ArmyRules rules = Load<ArmyRulesAsset>("ArmyRules.asset").Rules;
            Assert.AreEqual(3, rules.MaxTier);
            Assert.AreEqual(3, rules.MergeCount);
            Assert.AreEqual(1.8f, rules.TierMultiplier(2), 0.0001f);
            Assert.AreEqual(3.2f, rules.TierMultiplier(3), 0.0001f);
        }

        [TestCase("Stages/Stage_1_1.asset", "goblin_grunt:6")]
        [TestCase("Stages/Stage_1_2.asset", "goblin_archer:3,goblin_grunt:5")]
        [TestCase("Stages/Stage_1_3.asset", "goblin_archer:2,goblin_brute:2,goblin_chief:1")]
        public void StagesHaveValidFormationsMatchingGdd(string path, string expectedCounts)
        {
            StageDef stage = Load<StageDefinition>(path).ToStageDef();
            Assert.Greater(stage.Moves, 0);
            var cells = new HashSet<GridPos>();
            foreach (EnemySlot slot in stage.Enemies)
            {
                Assert.IsNotNull(slot.Unit, $"{stage.Id} slot has a unit");
                Assert.IsTrue(Formation.IsInside(slot.Cell), $"{stage.Id} {slot.Cell} inside 3x3");
                Assert.IsTrue(cells.Add(slot.Cell), $"{stage.Id} {slot.Cell} used once");
            }
            string counts = string.Join(",", stage.Enemies
                .GroupBy(s => s.Unit.Id)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}:{g.Count()}"));
            Assert.AreEqual(expectedCounts, counts);
        }

        [TestCase("Stages/Stage_1_1.asset", 0)]
        [TestCase("Stages/Stage_1_2.asset", 3)]
        [TestCase("Stages/Stage_1_3.asset", 5)]
        public void StagesHaveTheStoneGemsOfTheGdd(string path, int expected)
        {
            Assert.AreEqual(expected, Load<StageDefinition>(path).ToStageDef().StoneGems);
        }

        [Test]
        public void EnemyStrengthRisesFromStageToStage()
        {
            var multipliers = new[] { "Stages/Stage_1_1.asset", "Stages/Stage_1_2.asset", "Stages/Stage_1_3.asset" }
                .Select(path => Load<StageDefinition>(path).ToStageDef().EnemyHpMultiplier).ToArray();
            CollectionAssert.AreEqual(new[] { 1.0f, 1.1f, 1.2f }, multipliers);
        }

        [Test]
        public void GameContentIsComplete()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            CollectionAssert.AreEqual(new[] { "warrior", "archer", "mage" },
                content.RecruitableUnits.Select(u => u.Def.Id).ToArray(), "gem order");
            CollectionAssert.AreEqual(new[] { "stage_1_1", "stage_1_2", "stage_1_3" }, content.CampaignOrder());
            Assert.IsNotNull(content.PuzzleRules);
            Assert.IsNotNull(content.ArmyRules);
            Assert.IsNotNull(content.BattleRules);
            Assert.IsNotNull(content.ProgressionRules);
        }

        [Test]
        public void Stage11CanBePlayedHeadlessFromGameContent()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            Logic.Flow.StageRun run = content.CreateRun(content.Campaign[0], new SeededRandom(5));
            while (!run.Puzzle.IsFinished)
            {
                run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b);
                run.Puzzle.PlaySwap(a, b);
            }
            AutoFormation.Fill(run.Roster);
            Assert.IsTrue(run.StartBattle());
            run.Battle.RunToEnd();
            Assert.IsNotNull(run.Finish());
        }

        [Test]
        public void Stage11IsWinnableWithABasicArmy()
        {
            UnitDef warrior = Load<UnitDefinition>("Units/Warrior.asset").Def;
            UnitDef archer = Load<UnitDefinition>("Units/Archer.asset").Def;
            var player = new Formation();
            player.Place(new ArmyUnit(warrior), new GridPos(0, 0));
            player.Place(new ArmyUnit(warrior), new GridPos(1, 0));
            player.Place(new ArmyUnit(warrior), new GridPos(2, 0));
            player.Place(new ArmyUnit(archer), new GridPos(0, 2));
            player.Place(new ArmyUnit(archer), new GridPos(2, 2));
            StageDef stage = Load<StageDefinition>("Stages/Stage_1_1.asset").ToStageDef();
            var sim = new BattleSimulator(
                Load<BattleRulesAsset>("BattleRules.asset").Rules,
                Load<ArmyRulesAsset>("ArmyRules.asset").Rules,
                player,
                Formation.FromEnemySlots(stage.Enemies),
                0);
            Assert.IsTrue(sim.RunToEnd().Victory);
        }
    }
}
