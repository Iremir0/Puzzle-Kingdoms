using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Kingdom;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-22 (D-052): buildings, their cost curve, the bonuses in battle and the save.</summary>
    public class KingdomTests
    {
        private static KingdomRules Rules()
        {
            var rules = new KingdomRules { MaxLevel = 5, BaseCost = 100, CostGrowth = 2f };
            rules.Buildings.Add(new BuildingRule { Id = "barracks", Stat = BuildingStat.UnitHp, UnitId = "warrior", BonusPerLevel = 0.1f });
            rules.Buildings.Add(new BuildingRule { Id = "archer_tower", Stat = BuildingStat.UnitAttackSpeed, UnitId = "archer", BonusPerLevel = 0.1f });
            rules.Buildings.Add(new BuildingRule { Id = "mage_tower", Stat = BuildingStat.UnitDamage, UnitId = "mage", BonusPerLevel = 0.1f, CostFactor = 1.5f });
            rules.Buildings.Add(new BuildingRule { Id = "treasury", Stat = BuildingStat.StageGold, BonusPerLevel = 0.1f });
            return rules;
        }

        private static PlayerProfile RichProfile(int gold)
        {
            var profile = new PlayerProfile();
            profile.Apply("s", new StageReward { Stars = 1, VictoryGold = gold });
            return profile;
        }

        // ---- cost curve ----

        [Test]
        public void CostGrowsWithEachLevelAndStopsAtTheCap()
        {
            KingdomRules rules = Rules();
            Assert.AreEqual(100, rules.CostToUpgrade("barracks", 0));
            Assert.AreEqual(200, rules.CostToUpgrade("barracks", 1));
            Assert.AreEqual(1600, rules.CostToUpgrade("barracks", 4));
            Assert.AreEqual(-1, rules.CostToUpgrade("barracks", 5), "at the cap");
            Assert.AreEqual(-1, rules.CostToUpgrade("nope", 0), "unknown building");
        }

        [Test]
        public void CostFactorMakesABuildingDearer()
        {
            Assert.AreEqual(150, Rules().CostToUpgrade("mage_tower", 0));
        }

        [Test]
        public void CostsRoundToFive()
        {
            var rules = new KingdomRules { BaseCost = 60, CostGrowth = 1.4f };
            rules.Buildings.Add(new BuildingRule { Id = "b" });
            Assert.IsTrue(Enumerable.Range(0, 10).All(l => rules.CostToUpgrade("b", l) % 5 == 0));
            Assert.AreEqual(60, rules.CostToUpgrade("b", 0));
            Assert.AreEqual(85, rules.CostToUpgrade("b", 1)); // 84 rounded to 5
        }

        [Test]
        public void TotalCostAddsTheLevels()
        {
            Assert.AreEqual(100 + 200 + 400, Rules().TotalCostTo("barracks", 3));
        }

        // ---- upgrading ----

        [Test]
        public void UpgradeSpendsGoldAndRaisesTheLevel()
        {
            PlayerProfile profile = RichProfile(400);
            Assert.IsTrue(profile.TryUpgradeBuilding(Rules(), "barracks"));
            Assert.AreEqual(1, profile.BuildingLevel("barracks"));
            Assert.AreEqual(300, profile.Gold); // level 1 cost 100
            Assert.IsTrue(profile.TryUpgradeBuilding(Rules(), "barracks"));
            Assert.AreEqual(2, profile.BuildingLevel("barracks"));
            Assert.AreEqual(100, profile.Gold); // level 2 cost 200
            Assert.IsFalse(profile.TryUpgradeBuilding(Rules(), "barracks"), "level 3 costs 400");
        }

        [Test]
        public void UpgradeFailsWithoutGoldAndChangesNothing()
        {
            PlayerProfile profile = RichProfile(99);
            int changes = 0;
            profile.Changed += () => changes++;
            Assert.IsFalse(profile.TryUpgradeBuilding(Rules(), "barracks"));
            Assert.AreEqual(0, profile.BuildingLevel("barracks"));
            Assert.AreEqual(99, profile.Gold);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void UpgradeFailsAtTheCapAndForUnknownBuildings()
        {
            PlayerProfile profile = RichProfile(100000);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(profile.TryUpgradeBuilding(Rules(), "treasury"));
            }
            int gold = profile.Gold;
            Assert.IsFalse(profile.TryUpgradeBuilding(Rules(), "treasury"));
            Assert.IsFalse(profile.TryUpgradeBuilding(Rules(), "castle"));
            Assert.AreEqual(gold, profile.Gold);
            Assert.AreEqual(5, profile.BuildingLevel("treasury"));
        }

        // ---- bonuses ----

        [Test]
        public void BonusesFollowTheLevels()
        {
            PlayerProfile profile = RichProfile(100000);
            profile.TryUpgradeBuilding(Rules(), "barracks");
            profile.TryUpgradeBuilding(Rules(), "barracks");
            profile.TryUpgradeBuilding(Rules(), "treasury");
            KingdomBonuses bonuses = profile.Bonuses(Rules());
            Assert.AreEqual(1.2f, bonuses.HpMultiplier("warrior"), 0.0001f);
            Assert.AreEqual(1f, bonuses.HpMultiplier("archer"), "other classes are untouched");
            Assert.AreEqual(1f, bonuses.DamageMultiplier("warrior"));
            Assert.AreEqual(1.1f, bonuses.GoldMultiplier, 0.0001f);
        }

        [Test]
        public void BonusesAtLevelGiveEveryBuildingThatLevel()
        {
            KingdomBonuses bonuses = Rules().BonusesAtLevel(3);
            Assert.AreEqual(1.3f, bonuses.HpMultiplier("warrior"), 0.0001f);
            Assert.AreEqual(1.3f, bonuses.AttackSpeedMultiplier("archer"), 0.0001f);
            Assert.AreEqual(1.3f, bonuses.DamageMultiplier("mage"), 0.0001f);
            Assert.AreEqual(1.3f, bonuses.GoldMultiplier, 0.0001f);
        }

        [Test]
        public void LevelsAboveTheCapAreClamped()
        {
            Assert.AreEqual(1.5f, Rules().BonusesAtLevel(99).HpMultiplier("warrior"), 0.0001f);
        }

        // ---- in battle ----

        private static Combatant Make(Side side, UnitDef def, KingdomBonuses bonuses) =>
            new Combatant(0, side, new ArmyUnit(def), new GridPos(0, 0), new ArmyRules(), bonuses);

        [Test]
        public void PlayerUnitsGetTheirClassBonuses()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 500, 12, 1f);
            UnitDef archer = ArmyFixtures.UnitDef("archer", 250, 10, 0.8f);
            KingdomBonuses bonuses = Rules().BonusesAtLevel(2); // +20 % everywhere
            Combatant w = Make(Side.Player, warrior, bonuses);
            Assert.AreEqual(600, w.MaxHp);
            Assert.AreEqual(600, w.Hp);
            Assert.AreEqual(12, w.Damage, "the Barracks only gives HP");
            Combatant a = Make(Side.Player, archer, bonuses);
            Assert.AreEqual(250, a.MaxHp);
            Assert.AreEqual(0.8f / 1.2f, a.AttackInterval, 0.0001f);
        }

        [Test]
        public void EnemiesNeverGetKingdomBonuses()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 500, 12, 1f);
            Combatant enemy = Make(Side.Enemy, warrior, Rules().BonusesAtLevel(5));
            Assert.AreEqual(500, enemy.MaxHp);
            Assert.AreEqual(1f, enemy.AttackInterval);
        }

        [Test]
        public void NoBonusesChangeNothing()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 500, 12, 1f);
            Combatant a = Make(Side.Player, warrior, null);
            Combatant b = Make(Side.Player, warrior, KingdomBonuses.None);
            Assert.AreEqual(500, a.MaxHp);
            Assert.AreEqual(a.MaxHp, b.MaxHp);
            Assert.AreEqual(12, b.Damage);
        }

        [Test]
        public void BattleUsesTheBonuses()
        {
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 500, 12, 1f);
            Formation player = Combat.CombatFixtures.Single(warrior);
            Formation enemy = Combat.CombatFixtures.Single(Combat.CombatFixtures.Dummy());
            var sim = new BattleSimulator(Combat.CombatFixtures.Rules(), new ArmyRules(), player, enemy, 0, Rules().BonusesAtLevel(5));
            Assert.AreEqual(750, sim.Combatants[0].MaxHp);
            Assert.AreEqual(100000, sim.Combatants[1].MaxHp);
        }

        // ---- gold ----

        [Test]
        public void TreasuryRaisesOnlyTheVictoryGold()
        {
            var stage = new StageDef { GoldReward = 100 };
            var result = new BattleResult { Victory = true, Deployed = 4, Survivors = 4 };
            StageReward plain = StageReward.Compute(stage, result, 2, 3, new ProgressionRules());
            StageReward rich = StageReward.Compute(stage, result, 2, 3, new ProgressionRules(), true, Rules().BonusesAtLevel(3));
            Assert.AreEqual(100, plain.VictoryGold);
            Assert.AreEqual(130, rich.VictoryGold);
            Assert.AreEqual(plain.DismissGold, rich.DismissGold);
            Assert.AreEqual(plain.OverflowGold, rich.OverflowGold);
        }

        [Test]
        public void ADefeatStaysUnpaidWithTreasury()
        {
            var stage = new StageDef { GoldReward = 100 };
            var result = new BattleResult { Victory = false, Deployed = 4, Survivors = 0 };
            Assert.AreEqual(0, StageReward.Compute(stage, result, 0, 0, new ProgressionRules(), true, Rules().BonusesAtLevel(5)).VictoryGold);
        }

        // ---- save version 3 ----

        [Test]
        public void BuildingLevelsSurviveASaveRoundTrip()
        {
            PlayerProfile profile = RichProfile(100000);
            profile.TryUpgradeBuilding(Rules(), "barracks");
            profile.TryUpgradeBuilding(Rules(), "barracks");
            profile.TryUpgradeBuilding(Rules(), "mage_tower");
            PlayerProfile.Snapshot snapshot = profile.ToSnapshot();
            Assert.AreEqual(PlayerProfile.CurrentVersion, snapshot.version);
            PlayerProfile loaded = PlayerProfile.FromSnapshot(snapshot);
            Assert.AreEqual(2, loaded.BuildingLevel("barracks"));
            Assert.AreEqual(1, loaded.BuildingLevel("mage_tower"));
            Assert.AreEqual(0, loaded.BuildingLevel("treasury"));
            Assert.AreEqual(profile.Gold, loaded.Gold);
        }

        [Test]
        public void VersionTwoSavesLoadWithNoBuildings()
        {
            var old = new PlayerProfile.Snapshot { version = 2, gold = 321, buildingIds = null, buildingLevels = null };
            PlayerProfile loaded = PlayerProfile.FromSnapshot(old);
            Assert.AreEqual(321, loaded.Gold);
            Assert.AreEqual(0, loaded.BuildingLevel("barracks"));
            Assert.AreEqual(PlayerProfile.CurrentVersion, loaded.ToSnapshot().version, "saved as the current version from now on");
        }

        [Test]
        public void VersionOneSavesStillLoad()
        {
            var old = new PlayerProfile.Snapshot { version = 1, gold = 5 };
            Assert.AreEqual(5, PlayerProfile.FromSnapshot(old).Gold);
        }

        [Test]
        public void DamagedBuildingDataIsTolerated()
        {
            var snapshot = new PlayerProfile.Snapshot();
            snapshot.buildingIds.AddRange(new[] { "barracks", "", "mage_tower" });
            snapshot.buildingLevels.AddRange(new[] { -4, 7, 3 });
            PlayerProfile loaded = PlayerProfile.FromSnapshot(snapshot);
            Assert.AreEqual(0, loaded.BuildingLevel("barracks"), "a negative level becomes 0");
            Assert.AreEqual(3, loaded.BuildingLevel("mage_tower"));
        }

        [Test]
        public void ANewerSaveStillThrows()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                PlayerProfile.FromSnapshot(new PlayerProfile.Snapshot { version = PlayerProfile.CurrentVersion + 1 }));
        }
    }
}
