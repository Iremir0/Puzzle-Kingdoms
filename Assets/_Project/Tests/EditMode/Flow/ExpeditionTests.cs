using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Flow
{
    /// <summary>PB-26 (D-062): a run of battles with the army and its wounds carried over, rewards, loss, save.</summary>
    public class ExpeditionTests
    {
        private static ExpeditionRules Rules(int playerHp = 900, int enemyHp = 60, int enemyDamage = 8)
        {
            var rules = new ExpeditionRules { Steps = 3 };
            rules.PlayerClasses.Add(ArmyFixtures.UnitDef("warrior", playerHp, 40, 1f, 1f, 1.2f));
            rules.PlayerClasses.Add(ArmyFixtures.UnitDef("archer", playerHp / 2, 30, 0.8f, 4f, 1f));
            rules.PlayerClasses.Add(ArmyFixtures.UnitDef("mage", playerHp / 2, 50, 1.6f, 3.5f, 0.9f));
            rules.EnemyPool.Add(ArmyFixtures.UnitDef("goblin_grunt", enemyHp, enemyDamage, 1f, 1f, 1.2f));
            rules.EnemyPool.Add(ArmyFixtures.UnitDef("goblin_archer", enemyHp, enemyDamage, 1f, 4f, 1f));
            rules.Boss = ArmyFixtures.UnitDef("goblin_chief", enemyHp * 4, enemyDamage * 2, 1f, 1f, 1f);
            return rules;
        }

        private static string Describe(StageDef stage) =>
            stage.Moves + "/" + stage.BoardSeed + "/" + stage.EnemyHpMultiplier + ":" +
            string.Join(",", stage.Enemies.Select(e => e.Unit.Id + e.Tier + "@" + e.Cell.X + e.Cell.Y));

        /// <summary>An expedition at a given step; with offers it waits for a pick, without it waits for a battle.</summary>
        private static Expedition At(ExpeditionRules rules, ulong seed, int step, params ExpeditionReward[] offers)
        {
            var save = new ExpeditionSave { seed = seed.ToString(), step = step, phase = offers.Length > 0 ? (int)ExpeditionPhase.Reward : 0 };
            foreach (ExpeditionReward offer in offers)
            {
                save.offerKinds.Add((int)offer.Kind);
                save.offerUnits.Add(offer.UnitId);
                save.offerAmounts.Add(offer.Amount);
            }
            return Expedition.FromSave(save, rules);
        }

        /// <summary>Plays one battle the way the screens do (puzzle, deploy everything, battle) and returns the run.</summary>
        private static StageRun Fight(Expedition expedition)
        {
            StageDef stage = expedition.StageForStep();
            var run = new StageRun(stage, expedition.Rules.PlayerClasses, new PuzzleRules(), new ArmyRules(), new BattleRules(),
                new ProgressionRules(), new SeededRandom(stage.BoardSeed), null, null, expedition.Army);
            run.BonusRally = expedition.BonusRally;
            while (!run.Puzzle.IsFinished)
            {
                Assert.IsTrue(run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                run.Puzzle.PlaySwap(a, b);
            }
            AutoFormation.Fill(run.Roster);
            Assert.IsTrue(run.StartBattle());
            while (!run.Battle.IsFinished)
            {
                if (run.Battle.Rally.CanActivate)
                {
                    run.Battle.ActivateRally();
                }
                run.Battle.Step();
            }
            return run;
        }

        [Test]
        public void TheSameSeedGivesTheSameExpeditionAndAnotherSeedAnotherOne()
        {
            string a = Describe(new Expedition(Rules(), 42).StageForStep());
            Assert.AreEqual(a, Describe(new Expedition(Rules(), 42).StageForStep()));
            Assert.AreNotEqual(a, Describe(new Expedition(Rules(), 43).StageForStep()));
        }

        [Test]
        public void EnemiesGrowStrongerAndTheLastStepHasTheBoss()
        {
            var expedition = new Expedition(Rules(), 7);
            StageDef first = expedition.StageForStep();
            Assert.AreEqual(12, first.Moves);
            Assert.AreEqual(1f, first.EnemyHpMultiplier);
            Assert.IsFalse(first.Enemies.Any(e => e.Unit.Id == "goblin_chief"));
            Assert.IsFalse(expedition.IsBossStep);

            Expedition late = At(Rules(), 7, 2);
            StageDef last = late.StageForStep();
            int expectedBossTier = late.Rules.BossTier;
            Assert.IsTrue(late.IsBossStep);
            Assert.IsTrue(last.Enemies.Any(e => e.Unit.Id == "goblin_chief" && e.Tier == expectedBossTier), "the boss");
            Assert.Greater(last.EnemyHpMultiplier, first.EnemyHpMultiplier);
            Assert.AreEqual(8, last.Moves, "fewer moves after the first battle");
            Assert.AreEqual(last.Enemies.Count, last.Enemies.Select(e => e.Cell.X * Formation.Size + e.Cell.Y).Distinct().Count());
        }

        [Test]
        public void WoundsAndSurvivorsCarryOverToTheNextBattle()
        {
            var expedition = new Expedition(Rules(playerHp: 160, enemyHp: 160, enemyDamage: 20), 5);
            StageRun run = Fight(expedition);
            Assert.IsTrue(run.Battle.Result.Victory, "this setup is meant to be won");
            int survivors = run.Battle.Combatants.Count(c => c.Side == Side.Player && c.IsAlive);

            expedition.Finish(run);

            Assert.AreEqual(ExpeditionPhase.Reward, expedition.Phase);
            Assert.AreEqual(1, expedition.Step);
            Assert.AreEqual(survivors + run.Roster.Bench.Count, expedition.Army.Count, "survivors and the bench go on, the dead stay");
            Assert.IsTrue(expedition.Army.All(u => u.HpFraction > 0f && u.HpFraction <= 1f));
            Assert.AreEqual(3, expedition.Offers.Count);

            expedition.Pick(expedition.Offers.FindIndex(o => o.Kind == ExpeditionRewardKind.Rally) >= 0
                ? expedition.Offers.FindIndex(o => o.Kind == ExpeditionRewardKind.Rally)
                : 0);
            StageDef stage = expedition.StageForStep();
            var next = new StageRun(stage, expedition.Rules.PlayerClasses, new PuzzleRules(), new ArmyRules(), new BattleRules(),
                new ProgressionRules(), new SeededRandom(1), null, null, expedition.Army);
            Assert.AreEqual(expedition.Army.Count, next.Roster.Bench.Count, "the carried army starts on the bench");
            Assert.IsTrue(next.Roster.Bench.Select(u => u.Tier).OrderBy(t => t).SequenceEqual(expedition.Army.Select(u => u.Tier).OrderBy(t => t)));
        }

        [Test]
        public void AWoundedUnitStartsTheBattleWithLessHp()
        {
            UnitDef def = ArmyFixtures.UnitDef("warrior", 400, 10);
            var healthy = new Combatant(0, Side.Player, new ArmyUnit(def), new GridPos(0, 0), new ArmyRules());
            var hurt = new Combatant(1, Side.Player, new ArmyUnit(def) { HpFraction = 0.25f }, new GridPos(1, 0), new ArmyRules());
            var enemy = new Combatant(2, Side.Enemy, new ArmyUnit(def) { HpFraction = 0.25f }, new GridPos(0, 0), new ArmyRules());

            Assert.AreEqual(400, healthy.Hp);
            Assert.AreEqual(100, hurt.Hp);
            Assert.AreEqual(400, hurt.MaxHp, "max HP is unchanged");
            Assert.AreEqual(400, enemy.Hp, "only the player's units carry wounds");
        }

        [Test]
        public void RewardsApply()
        {
            ExpeditionRules rules = Rules();
            Expedition WithArmy(ExpeditionReward reward)
            {
                Expedition e = At(rules, 9, 1, reward, new ExpeditionReward { Kind = ExpeditionRewardKind.Rally, Amount = 1 });
                e.Army.Add(new ArmyUnit(rules.PlayerClasses[0]) { HpFraction = 0.3f });
                e.Army.Add(new ArmyUnit(rules.PlayerClasses[1], 2) { HpFraction = 0.5f });
                return e;
            }

            Expedition heal = WithArmy(new ExpeditionReward { Kind = ExpeditionRewardKind.HealAll });
            Assert.IsTrue(heal.Pick(0));
            Assert.IsTrue(heal.Army.All(u => u.HpFraction == 1f));
            Assert.AreEqual(ExpeditionPhase.Battle, heal.Phase);
            Assert.IsFalse(heal.Pick(0), "no second pick");

            Expedition recruit = WithArmy(new ExpeditionReward { Kind = ExpeditionRewardKind.Recruit, UnitId = "mage", Amount = 3 });
            recruit.Pick(0);
            Assert.AreEqual(5, recruit.Army.Count);
            Assert.AreEqual(3, recruit.Army.Count(u => u.Def.Id == "mage"));

            Expedition promote = WithArmy(new ExpeditionReward { Kind = ExpeditionRewardKind.Promote });
            promote.Pick(0);
            Assert.AreEqual(2, promote.Army.Count(u => u.Tier == 2), "the tier 1 warrior became tier 2 next to the tier 2 archer");
            Assert.AreEqual(1f, promote.Army.First(u => u.Def.Id == "warrior").HpFraction, "a promoted unit is fresh");

            Expedition rally = WithArmy(new ExpeditionReward { Kind = ExpeditionRewardKind.Rally, Amount = 2 });
            rally.Pick(0);
            Assert.AreEqual(2, rally.BonusRally);
        }

        [Test]
        public void ALostBattleEndsTheRunWithoutCarryingAnything()
        {
            var expedition = new Expedition(Rules(playerHp: 20, enemyHp: 4000, enemyDamage: 200), 3);
            StageRun run = Fight(expedition);
            Assert.IsFalse(run.Battle.Result.Victory);

            expedition.Finish(run);

            Assert.AreEqual(ExpeditionPhase.Lost, expedition.Phase);
            Assert.IsTrue(expedition.IsOver);
            Assert.AreEqual(0, expedition.Step);
        }

        [Test]
        public void AStrongArmyWinsEveryBattleAndTheRun()
        {
            var expedition = new Expedition(Rules(playerHp: 5000, enemyHp: 40, enemyDamage: 2), 11);
            int guard = 0;
            while (!expedition.IsOver && guard++ < 10)
            {
                expedition.Finish(Fight(expedition));
                if (expedition.Phase == ExpeditionPhase.Reward)
                {
                    Assert.IsTrue(expedition.Pick(0));
                }
            }

            Assert.AreEqual(ExpeditionPhase.Won, expedition.Phase);
            Assert.AreEqual(3, expedition.Step);
            Assert.AreEqual(3 * expedition.Rules.GoldPerStep + expedition.Rules.GoldForWinning, expedition.GoldEarned);
        }

        [Test]
        public void ARunSavesAndComesBackTheSame()
        {
            ExpeditionRules rules = Rules();
            Expedition expedition = At(rules, 18446744073709551000UL, 1,
                new ExpeditionReward { Kind = ExpeditionRewardKind.Recruit, UnitId = "archer", Amount = 3 },
                new ExpeditionReward { Kind = ExpeditionRewardKind.HealAll });
            expedition.Army.Add(new ArmyUnit(rules.PlayerClasses[0], 2) { HpFraction = 0.4f });
            expedition.Army.Add(new ArmyUnit(rules.PlayerClasses[2]));

            var profile = new PlayerProfile();
            profile.SaveExpedition(expedition.ToSave());
            PlayerProfile loadedProfile = PlayerProfile.FromSnapshot(profile.ToSnapshot());
            Expedition loaded = Expedition.FromSave(loadedProfile.ActiveExpedition, rules);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(expedition.Seed, loaded.Seed);
            Assert.AreEqual(1, loaded.Step);
            Assert.AreEqual(ExpeditionPhase.Reward, loaded.Phase);
            Assert.AreEqual(2, loaded.Army.Count);
            Assert.AreEqual(2, loaded.Army[0].Tier);
            Assert.AreEqual(0.4f, loaded.Army[0].HpFraction, 0.001f);
            Assert.AreEqual(2, loaded.Offers.Count);
            Assert.AreEqual("archer", loaded.Offers[0].UnitId);
            Assert.AreEqual(Describe(expedition.StageForStep()), Describe(loaded.StageForStep()), "the same battle comes next");
        }

        [Test]
        public void ASaveWithAnUnknownClassIsRefused()
        {
            ExpeditionRules rules = Rules();
            var save = new Expedition(rules, 4).ToSave();
            save.armyIds.Add("dragon");
            save.armyTiers.Add(1);
            save.armyHp.Add(1000);
            Assert.IsNull(Expedition.FromSave(save, rules));
            Assert.IsNull(Expedition.FromSave(new ExpeditionSave { seed = "not a number" }, rules));
            Assert.IsNull(Expedition.FromSave(null, rules));
        }

        [Test]
        public void EndingARunKeepsTheRecordPaysGoldAndClearsTheSave()
        {
            var profile = new PlayerProfile();
            profile.SaveExpedition(new ExpeditionSave { seed = "1" });

            profile.EndExpedition(4, false, 240);
            profile.EndExpedition(2, false, 120);
            profile.EndExpedition(5, true, 560);

            Assert.IsNull(profile.ActiveExpedition);
            Assert.AreEqual(5, profile.BestExpeditionDepth);
            Assert.AreEqual(1, profile.ExpeditionsWon);
            Assert.AreEqual(920, profile.Gold);
            PlayerProfile loaded = PlayerProfile.FromSnapshot(profile.ToSnapshot());
            Assert.AreEqual(5, loaded.BestExpeditionDepth);
            Assert.AreEqual(1, loaded.ExpeditionsWon);
            Assert.IsNull(loaded.ActiveExpedition);
        }
    }
}
