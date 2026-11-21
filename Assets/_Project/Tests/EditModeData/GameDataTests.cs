using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
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

        /// <summary>PB-19: every stage asset, in the campaign or not, has a usable goal and start board.</summary>
        [Test]
        public void EveryStageAssetHasAValidGoalAndStartBoard()
        {
            var content = Load<GameContent>("GameContent.asset");
            string[] guids = AssetDatabase.FindAssets("t:StageDefinition", new[] { Root + "Stages" });
            Assert.Greater(guids.Length, 2);
            foreach (string guid in guids)
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                int gemTypes = (stage.Units.Count > 0 ? stage.Units.Count : content.RecruitableUnits.Count) + 1;
                StageDef def = stage.ToStageDef();
                Assert.IsTrue(def.Goal.IsValid, $"{stage.name}: a goal that needs a target has one");
                if (!Logic.Puzzle.BoardLayout.IsEmpty(def.Layout))
                {
                    Assert.IsTrue(Logic.Puzzle.BoardLayout.TryParse(def.Layout, 6, 6, gemTypes, out _, out _, out string error),
                        $"{stage.name}: {error}");
                }
                Assert.DoesNotThrow(() => content.CreateRun(stage, new SeededRandom(7)), stage.name);
            }
        }

        [Test]
        public void AllUnitsHaveSaneValues()
        {
            List<UnitDefinition> units = AllUnits().ToList();
            Assert.AreEqual(17, units.Count, "3 player classes + Defender and Support + 4 Chapter 1 goblins + 4 Chapter 2 and 4 Chapter 3 enemies");
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
            Assert.AreEqual(4f, rules.TierMultiplier(2), 0.0001f); // PB-34, D-068
            Assert.AreEqual(13f, rules.TierMultiplier(3), 0.0001f);
            Assert.AreEqual(3.2f, rules.ForEnemies().TierMultiplier(3), 0.0001f, "enemies keep their curve (D-068)");
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

        /// <summary>PB-13, D-040: every stage sits on the map and has tutorial prompts that can be shown.</summary>
        [Test]
        public void StagesHaveMapPositionsAndValidTutorialPrompts()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            var ids = new HashSet<string>();
            var positions = new List<UnityEngine.Vector2>();
            int shownChapter = 0;
            foreach (StageDefinition stage in content.Campaign)
            {
                UnityEngine.Vector2 at = stage.MapPosition;
                if (stage.Chapter != shownChapter) // every chapter has its own map
                {
                    shownChapter = stage.Chapter;
                    positions.Clear();
                }
                Assert.That(at.x, Is.InRange(0.1f, 0.9f), stage.Id + " map x");
                Assert.That(at.y, Is.InRange(0.1f, 0.85f), stage.Id + " map y (room for the chapter title)");
                Assert.IsFalse(positions.Any(p => UnityEngine.Vector2.Distance(p, at) < 0.15f), stage.Id + " overlaps a node");
                positions.Add(at);

                List<Logic.Progression.TutorialStep> steps = stage.TutorialSteps();
                Assert.IsNotEmpty(steps, stage.Id + " teaches its lesson (GDD §11)");
                foreach (Logic.Progression.TutorialStep step in steps)
                {
                    Assert.IsNotEmpty(step.Id, stage.Id);
                    Assert.IsTrue(ids.Add(step.Id), "duplicate tutorial id " + step.Id);
                    Assert.IsNotEmpty(step.Text, step.Id);
                    Assert.LessOrEqual(step.Text.Length, 120, step.Id + " is short");
                }
            }
            Assert.IsTrue(ids.Contains("1-3.rally"), "Rally timing is taught in 1-3");
        }

        /// <summary>PB-18, D-051: Chapter 2 is ten stages in campaign order, each enemy behaviour appears, the boss stage has a Chief.</summary>
        [Test]
        public void ChapterTwoUsesTheFourNewEnemies()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            var chapter2 = content.Campaign.Where(s => s.Chapter == 2).Select(s => s.ToStageDef()).ToList();
            Assert.AreEqual(10, chapter2.Count);
            var used = new HashSet<string>(chapter2.SelectMany(s => s.Enemies).Select(e => e.Unit.Id));
            foreach (string id in new[] { "shield_goblin", "goblin_shaman", "wolf_rider", "goblin_bomber" })
            {
                Assert.IsTrue(used.Contains(id), id + " appears in Chapter 2");
            }
            Assert.IsTrue(chapter2[chapter2.Count - 1].Enemies.Any(e => e.Unit.Id == "goblin_chief"), "boss stage has the Chief");
            Assert.Greater(chapter2[chapter2.Count - 1].BossStonesEvery, 0, "the boss throws stones");
        }

        /// <summary>PB-24: the quest pool has unique ids, real targets, enough daily quests of different kinds.</summary>
        [Test]
        public void QuestPoolIsValid()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            Assert.IsNotNull(content.Quests, "GameContent has the quest pool");
            Logic.Quests.QuestRules rules = content.Quests.Rules;
            var ids = new HashSet<string>();
            foreach (Logic.Quests.QuestDef quest in rules.Daily.Concat(rules.Achievements))
            {
                Assert.IsNotEmpty(quest.Id);
                Assert.IsTrue(ids.Add(quest.Id), "duplicate quest " + quest.Id);
                Assert.Greater(quest.Target, 0, quest.Id);
                Assert.Greater(quest.RewardGold, 0, quest.Id);
            }
            Assert.GreaterOrEqual(rules.Daily.Count, rules.DailyCount * 2, "a pool bigger than a day");
            Assert.GreaterOrEqual(rules.Daily.Select(q => q.Kind).Distinct().Count(), rules.DailyCount, "enough kinds for a day");
            Assert.GreaterOrEqual(rules.Achievements.Count, 5);
            Assert.Greater(rules.StarsPerChest, 0);
        }

        /// <summary>PB-25: the daily setup builds valid challenges for many days and they are neither hopeless nor trivial.</summary>
        [Test]
        public void DailyChallengesAreValidAndWinnableButNotTrivial()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            Assert.IsNotNull(content.DailyChallenge, "GameContent has the daily challenge");
            DailyChallengeRules rules = content.DailyChallenge.Rules();
            Assert.GreaterOrEqual(rules.EnemyPool.Count, 5);
            Assert.IsFalse(rules.EnemyPool.Any(u => u.Id.Contains("chief")), "no boss in the pool");

            double wins = 0;
            double runs = 0;
            for (int d = 1; d <= 20; d++)
            {
                StageDef def = DailyChallenge.For("2026-11-" + d.ToString("00"), rules);
                BalanceStats stats = BalanceRunner.Run(seed => content.CreateRun(def, new SeededRandom(seed)),
                    PuzzleBot.Greedy, RallyBot.Eager, 10);
                wins += stats.Wins;
                runs += stats.Runs;
            }
            double rate = wins / runs;
            UnityEngine.Debug.Log($"Daily challenge: greedy bot wins {100 * rate:0}% over 20 days");
            Assert.That(rate, Is.InRange(0.1, 0.9), $"win rate {rate:0.00}");
        }

        /// <summary>PB-26: the expedition asset is complete, and a simple bot gets part of the way but not always to the end.</summary>
        [Test]
        public void ExpeditionIsCompleteAndNeitherHopelessNorTrivial()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            Assert.IsNotNull(content.Expedition, "GameContent has the expedition");
            ExpeditionRules rules = content.Expedition.Rules();
            Assert.GreaterOrEqual(rules.EnemyPool.Count, 5);
            Assert.IsNotNull(rules.Boss);
            Assert.AreEqual(content.RecruitableUnits.Count, rules.PlayerClasses.Count);
            Assert.GreaterOrEqual(rules.Steps, 3);

            double depth = 0;
            int won = 0;
            const int runs = 20;
            for (ulong seed = 1; seed <= runs; seed++)
            {
                var expedition = new Expedition(rules, seed);
                while (!expedition.IsOver)
                {
                    StageDef stage = expedition.StageForStep();
                    StageRun run = content.CreateRun(stage, new SeededRandom(seed * 31 + (ulong)expedition.Step), null, null, expedition.Army);
                    run.BonusRally = expedition.BonusRally;
                    while (!run.Puzzle.IsFinished)
                    {
                        (GridPos a, GridPos b) = BalanceRunner.GreedyMove(run.Puzzle.Board);
                        run.Puzzle.PlaySwap(a, b);
                    }
                    AutoFormation.Fill(run.Roster);
                    if (!run.StartBattle())
                    {
                        break;
                    }
                    while (!run.Battle.IsFinished)
                    {
                        if (run.Battle.Rally.CanActivate)
                        {
                            run.Battle.ActivateRally();
                        }
                        run.Battle.Step();
                    }
                    expedition.Finish(run);
                    if (expedition.Phase == ExpeditionPhase.Reward)
                    {
                        expedition.Pick(0);
                    }
                }
                depth += expedition.Step;
                won += expedition.Phase == ExpeditionPhase.Won ? 1 : 0;
            }
            UnityEngine.Debug.Log($"Expedition: greedy bot clears {depth / runs:0.0} of {rules.Steps} battles on average, wins {won}/{runs}");
            Assert.That(depth / runs, Is.InRange(1.0, rules.Steps - 0.3), "average depth");
        }

        /// <summary>PB-30: the monetization numbers are sane (small rewards, a daily limit).</summary>
        [Test]
        public void MonetizationRulesAreSmallAndLimited()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            Assert.IsNotNull(content.Monetization, "GameContent has the monetization numbers");
            Logic.Progression.MonetizationRules rules = content.Monetization.Rules();
            Assert.That(rules.ExtraMoves, Is.InRange(1, 10));
            Assert.That(rules.ExtraRally, Is.InRange(1, 3));
            Assert.That(rules.GoldAdsPerDay, Is.InRange(1, 10));
            Assert.LessOrEqual(rules.BonusGold, 300, "an ad is a small gift, not the main income");
        }

        /// <summary>PB-22: the Kingdom asset is valid: unique ids, buildings that exist in the game, small steps, rising costs.</summary>
        [Test]
        public void KingdomRulesAreValid()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            Assert.IsNotNull(content.KingdomRules, "GameContent has the Kingdom rules");
            Logic.Kingdom.KingdomRules rules = content.KingdomRules.Rules;
            Assert.AreEqual(4, rules.Buildings.Count);
            var recruitable = content.RecruitableUnits.Select(u => u.Def.Id).ToList();
            var ids = new HashSet<string>();
            foreach (Logic.Kingdom.BuildingRule building in rules.Buildings)
            {
                Assert.IsTrue(ids.Add(building.Id), "duplicate building " + building.Id);
                Assert.IsNotEmpty(building.DisplayName, building.Id);
                if (building.Stat != Logic.Kingdom.BuildingStat.StageGold)
                {
                    Assert.Contains(building.UnitId, recruitable, building.Id + " improves a class the player can recruit");
                }
                Assert.LessOrEqual(building.BonusPerLevel * rules.MaxLevel, 0.5f, building.Id + " stays a small bonus");
                for (int level = 1; level < rules.MaxLevel; level++)
                {
                    Assert.Greater(rules.CostToUpgrade(building.Id, level), rules.CostToUpgrade(building.Id, level - 1), building.Id + " gets dearer");
                }
            }
        }

        /// <summary>PB-23: commanders are valid: unique ids, unlock stages that exist, the numbers their Rally kind needs.</summary>
        [Test]
        public void CommandersAreValid()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            var stageIds = content.CampaignOrder();
            var ids = new HashSet<string>();
            Assert.AreEqual(3, content.Commanders.Count);
            foreach (CommanderDefinition definition in content.Commanders)
            {
                Logic.Progression.Commander commander = definition.Commander;
                Assert.IsNotEmpty(commander.Id);
                Assert.IsTrue(ids.Add(commander.Id), "duplicate commander " + commander.Id);
                Assert.Contains(commander.UnlockStageId, stageIds, commander.Id + " unlocks on a campaign stage");
                Logic.Combat.RallyVariant v = commander.Variant;
                Assert.Greater(v.DurationSeconds, 0f, commander.Id);
                switch (v.Kind)
                {
                    case Logic.Combat.RallyKind.ShieldWall: Assert.Greater(v.FrontRowDamageReduction, 0f, commander.Id); break;
                    case Logic.Combat.RallyKind.ArrowRain: Assert.Greater(v.VolleyCount, 0, commander.Id); Assert.Greater(v.VolleyPercentMaxHp, 0f, commander.Id); break;
                    case Logic.Combat.RallyKind.Fireball: Assert.Greater(v.BlastRadius, 0f, commander.Id); Assert.Greater(v.BlastPercentMaxHp, 0f, commander.Id); break;
                    default: Assert.Fail(commander.Id + " must be a variant, not the classic Rally"); break;
                }
            }
        }

        /// <summary>PB-21: stages pick their class sets; a new class is only used after the stage that unlocks it.</summary>
        [Test]
        public void StageClassSetsAreValidAndRespectUnlocks()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            var known = content.RecruitableUnits.Concat(content.ExtraUnits).ToList();
            List<string> order = content.CampaignOrder();
            CollectionAssert.AreEqual(new[] { "defender", "support" }, content.ExtraUnits.Select(u => u.Def.Id).ToArray());
            foreach (StageDefinition stage in content.Campaign)
            {
                if (stage.Units.Count == 0)
                {
                    continue;
                }
                Assert.LessOrEqual(stage.Units.Count, 4, stage.Id + ": at most 4 classes plus the Banner gem");
                CollectionAssert.AllItemsAreUnique(stage.Units, stage.Id);
                foreach (UnitDefinition unit in stage.Units)
                {
                    Assert.Contains(unit, known, stage.Id + ": " + unit.name + " is a known class");
                    if (!string.IsNullOrEmpty(unit.UnlockStageId))
                    {
                        int unlockAt = order.IndexOf(unit.UnlockStageId);
                        Assert.GreaterOrEqual(unlockAt, 0, unit.name + " unlocks at a campaign stage");
                        Assert.Less(unlockAt, order.IndexOf(stage.Id), stage.Id + " only uses " + unit.name + " after it is unlocked");
                    }
                }
            }
            Assert.IsTrue(content.Campaign.Any(s => s.Units.Any(u => u.Def.Id == "defender")), "the Defender is used");
            Assert.IsTrue(content.Campaign.Any(s => s.Units.Any(u => u.Def.Id == "support")), "the Support is used");
        }

        [Test]
        public void GameContentIsComplete()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            CollectionAssert.AreEqual(new[] { "warrior", "archer", "mage" },
                content.RecruitableUnits.Select(u => u.Def.Id).ToArray(), "gem order");
            CollectionAssert.AreEqual(
                new[]
                {
                    "stage_1_1", "stage_1_2", "stage_1_3", "stage_1_4",
                    "stage_2_1", "stage_2_2", "stage_2_3", "stage_2_4", "stage_2_5",
                    "stage_2_6", "stage_2_7", "stage_2_8", "stage_2_9", "stage_2_10",
                    "stage_3_1", "stage_3_2", "stage_3_3", "stage_3_4", "stage_3_5", "stage_3_6", "stage_3_7", "stage_3_8", "stage_3_9", "stage_3_10",
                },
                content.CampaignOrder());
            Assert.IsNotNull(content.PuzzleRules);
            Assert.IsNotNull(content.ArmyRules);
            Assert.IsNotNull(content.BattleRules);
            Assert.IsNotNull(content.ProgressionRules);
        }

        [Test]
        public void SoundBankHasEveryClip()
        {
            SoundBank sounds = Load<GameContent>("GameContent.asset").Sounds;
            Assert.IsNotNull(sounds, "GameContent links the SoundBank");
            var clips = new Dictionary<string, UnityEngine.AudioClip>
            {
                ["match"] = sounds.Match,
                ["blastCreated"] = sounds.BlastCreated,
                ["blastFired"] = sounds.BlastFired,
                ["prismCreated"] = sounds.PrismCreated,
                ["prismFired"] = sounds.PrismFired,
                ["stoneCrack"] = sounds.StoneCrack,
                ["invalidSwap"] = sounds.InvalidSwap,
                ["unitRecruited"] = sounds.UnitRecruited,
                ["rallyCharge"] = sounds.RallyCharge,
                ["music"] = sounds.Music,
            };
            foreach (KeyValuePair<string, UnityEngine.AudioClip> clip in clips)
            {
                Assert.IsNotNull(clip.Value, clip.Key);
                Assert.Greater(clip.Value.length, 0.05f, clip.Key);
            }
            Assert.Less(sounds.Match.length, 0.35f, "effects stay short (PB-8: ≤ 0.3 s feel)");
            Assert.AreEqual(1f, sounds.CascadePitch(0));
            Assert.Greater(sounds.CascadePitch(1), sounds.CascadePitch(0), "pitch rises with cascades");
            Assert.LessOrEqual(sounds.CascadePitch(50), 2f, "pitch is capped");
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
        /// <summary>PB-37 (D-070): 1-4 is in the campaign and its painted board teaches Blast, then Prism, on any seed.</summary>
        [Test]
        public void TheQuarryTeachesBlastAndPrism()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            StageDefinition quarry = Load<StageDefinition>("Stages/Stage_1_4.asset");
            List<string> order = content.CampaignOrder();
            Assert.AreEqual(order.IndexOf("stage_1_3") + 1, order.IndexOf("stage_1_4"), "1-4 follows 1-3");
            for (ulong seed = 1; seed <= 5; seed++)
            {
                StageRun run = content.CreateRun(quarry, new SeededRandom(seed));
                BoardModel board = run.Puzzle.Board;
                Assert.IsTrue(board.WouldSwapMatch(new GridPos(3, 2), new GridPos(2, 2)), $"seed {seed}: the square move");
                Assert.IsTrue(board.WouldSwapMatch(new GridPos(2, 4), new GridPos(2, 5)), $"seed {seed}: the five-in-a-row move");
                MoveResult square = run.Puzzle.PlaySwap(new GridPos(3, 2), new GridPos(2, 2));
                Assert.IsTrue(square.Steps[0].Created.Any(c => c.Kind == SpecialKind.Blast), $"seed {seed}: the square makes a Blast");
                StageRun prism = content.CreateRun(quarry, new SeededRandom(seed));
                MoveResult five = prism.Puzzle.PlaySwap(new GridPos(2, 4), new GridPos(2, 5));
                Assert.IsTrue(five.Steps[0].Created.Any(c => c.Kind == SpecialKind.Prism), $"seed {seed}: five in a row make a Prism");
            }
        }

        [Test]
        public void EveryUnlockStageIsInTheCampaign()
        {
            GameContent content = Load<GameContent>("GameContent.asset");
            List<string> order = content.CampaignOrder();
            Logic.Progression.FeatureUnlockRules unlocks = content.ProgressionRules.Rules.Unlocks;
            Assert.AreEqual(6, unlocks.Unlocks.Count);
            foreach (Logic.Progression.FeatureUnlock unlock in unlocks.Unlocks)
            {
                CollectionAssert.Contains(order, unlock.StageId, unlock.Feature.ToString());
            }
        }
    }
}
