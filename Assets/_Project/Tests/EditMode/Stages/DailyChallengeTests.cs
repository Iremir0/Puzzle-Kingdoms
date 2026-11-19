using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Stages
{
    /// <summary>PB-25 (D-061): the challenge of a day, the score order, one attempt a day and the save.</summary>
    public class DailyChallengeTests
    {
        private static DailyChallengeRules Rules()
        {
            var rules = new DailyChallengeRules();
            rules.EnemyPool.Add(ArmyFixtures.UnitDef("goblin_grunt", 100, 10, 1f, 1f, 1f));
            rules.EnemyPool.Add(ArmyFixtures.UnitDef("goblin_archer", 60, 8, 1f, 4f, 1f));
            rules.EnemyPool.Add(ArmyFixtures.UnitDef("goblin_brute", 220, 14, 1.4f, 1f, 1f));
            return rules;
        }

        private static string Describe(StageDef stage) =>
            stage.Moves + "/" + stage.StoneGems + "/" + stage.BoardSeed + "/" + stage.Goal.Kind + stage.Goal.Target + ":" +
            string.Join(",", stage.Enemies.Select(e => e.Unit.Id + e.Tier + "@" + e.Cell.X + e.Cell.Y));

        [Test]
        public void TheSameDayGivesTheSameChallengeAndAnotherDayAnotherOne()
        {
            Assert.AreEqual(Describe(DailyChallenge.For("2026-11-19", Rules())), Describe(DailyChallenge.For("2026-11-19", Rules())));

            var days = Enumerable.Range(1, 12).Select(d => Describe(DailyChallenge.For("2026-11-" + d.ToString("00"), Rules()))).Distinct();
            Assert.Greater(days.Count(), 10, "different days differ");
        }

        [Test]
        public void ADayStaysInsideTheRules()
        {
            DailyChallengeRules rules = Rules();
            for (int d = 1; d <= 28; d++)
            {
                StageDef stage = DailyChallenge.For("2026-02-" + d.ToString("00"), rules);
                Assert.AreEqual("daily-2026-02-" + d.ToString("00"), stage.Id);
                Assert.IsTrue(DailyChallenge.IsDailyId(stage.Id));
                Assert.That(stage.Moves, Is.InRange(rules.MinMoves, rules.MaxMoves));
                Assert.That(stage.Enemies.Count, Is.InRange(rules.MinEnemies, rules.MaxEnemies));
                Assert.That(stage.StoneGems, Is.InRange(0, rules.MaxStones));
                Assert.AreNotEqual(StageDef.NoSeed, stage.BoardSeed, "a fixed board");
                Assert.IsTrue(stage.Enemies.All(e => e.Tier >= 1 && e.Tier <= rules.MaxTier));
                Assert.AreEqual(stage.Enemies.Count, stage.Enemies.Select(e => e.Cell.X * Formation.Size + e.Cell.Y).Distinct().Count(), "one enemy per cell");
                Assert.IsTrue(stage.Enemies.All(e => e.Cell.X >= 0 && e.Cell.X < Formation.Size && e.Cell.Y >= 0 && e.Cell.Y < Formation.Size));
                Assert.IsTrue(stage.Goal.IsValid);
                if (stage.Goal.Kind == StageGoalKind.BreakStones)
                {
                    Assert.LessOrEqual(stage.Goal.Target, stage.StoneGems, "the goal can be reached");
                }
            }
        }

        [Test]
        public void AnEmptyPoolIsRefused()
        {
            Assert.Throws<System.ArgumentException>(() => DailyChallenge.For("2026-11-19", new DailyChallengeRules()));
        }

        [Test]
        public void TheResultsAreOrderedWonFirstThenHpThenSpeed()
        {
            var lost = new DailyResult { Won = false, HpPercent = 90, Ticks = 100 };
            var narrow = new DailyResult { Won = true, HpPercent = 20, Ticks = 900 };
            var strong = new DailyResult { Won = true, HpPercent = 80, Ticks = 900 };
            var strongFast = new DailyResult { Won = true, HpPercent = 80, Ticks = 400 };

            Assert.IsTrue(narrow.IsBetterThan(lost), "any win beats a loss");
            Assert.IsTrue(strong.IsBetterThan(narrow), "more HP left");
            Assert.IsTrue(strongFast.IsBetterThan(strong), "same HP, faster");
            Assert.IsFalse(strong.IsBetterThan(strongFast));
            Assert.IsFalse(strong.IsBetterThan(strong), "equal is not better");
            Assert.IsTrue(lost.IsBetterThan(null));
        }

        [Test]
        public void TheScoreComesFromTheArmyHpLeftAtTheEnd()
        {
            var player = new Formation();
            UnitDef warrior = ArmyFixtures.UnitDef("warrior", 400, 30, 1f, 1f, 1.2f);
            for (int x = 0; x < 3; x++)
            {
                player.Place(new ArmyUnit(warrior), new GridPos(x, 0));
            }
            var enemy = new Formation();
            enemy.Place(new ArmyUnit(ArmyFixtures.UnitDef("goblin", 150, 5, 1f, 1f, 1f)), new GridPos(1, 0));
            var sim = new BattleSimulator(new BattleRules(), new ArmyRules(), player, enemy, 0);
            BattleResult battle = sim.RunToEnd();

            DailyResult result = DailyResult.From("2026-11-19", sim, battle.Victory);

            Assert.IsTrue(result.Won);
            Assert.That(result.HpPercent, Is.InRange(1, 100));
            Assert.AreEqual(battle.Ticks, result.Ticks);
            int hp = sim.Combatants.Where(c => c.Side == Side.Player).Sum(c => c.Hp);
            int max = sim.Combatants.Where(c => c.Side == Side.Player).Sum(c => c.MaxHp);
            Assert.AreEqual(hp * 100 / max, result.HpPercent);
        }

        [Test]
        public void OnlyOneAttemptPerDayCountsAndOldDaysFallOff()
        {
            var log = new DailyLog();
            Assert.IsTrue(log.Record(new DailyResult { Day = "2026-11-18", Won = true, HpPercent = 60, Ticks = 500 }));
            Assert.IsFalse(log.Record(new DailyResult { Day = "2026-11-18", Won = true, HpPercent = 100, Ticks = 10 }), "no second try");
            Assert.AreEqual(60, log.Result("2026-11-18").HpPercent, "the first result stays");
            Assert.IsTrue(log.HasPlayed("2026-11-18"));
            Assert.IsFalse(log.HasPlayed("2026-11-19"));

            for (int i = 0; i < 40; i++)
            {
                log.Record(new DailyResult { Day = "2026-12-" + i.ToString("00"), Won = false });
            }
            Assert.AreEqual(DailyLog.MaxDays, log.Days.Count);
            Assert.IsFalse(log.HasPlayed("2026-11-18"), "the oldest days drop off");
        }

        [Test]
        public void TheLastSevenDaysShowGapsAndTheBestResult()
        {
            var log = new DailyLog();
            log.Record(new DailyResult { Day = "2026-11-15", Won = true, HpPercent = 40, Ticks = 700 });
            log.Record(new DailyResult { Day = "2026-11-17", Won = true, HpPercent = 75, Ticks = 650 });
            log.Record(new DailyResult { Day = "2026-11-18", Won = false, HpPercent = 0, Ticks = 300 });

            var week = log.Recent("2026-11-19", 7);

            Assert.AreEqual(7, week.Count);
            Assert.IsNull(week[6], "today is not played yet");
            Assert.IsNotNull(week[5]);
            Assert.IsNull(week[3], "the 16th was skipped");
            Assert.AreEqual(75, week[4].HpPercent);
            Assert.AreEqual(40, week[2].HpPercent);
            Assert.AreEqual(75, log.Best().HpPercent);
            Assert.AreEqual(2, log.WinCount);
        }

        [Test]
        public void ADailyResultIsSavedPaysGoldOnceAndKeepsTheStarsAlone()
        {
            var profile = new PlayerProfile();
            var result = new DailyResult { Day = "2026-11-19", Won = true, HpPercent = 55, Ticks = 640 };

            Assert.IsTrue(profile.RecordDaily(result, 120));
            Assert.IsFalse(profile.RecordDaily(result, 120), "paid once");
            Assert.AreEqual(120, profile.Gold);
            Assert.AreEqual(0, profile.TotalStars, "a daily day is not a campaign stage");

            PlayerProfile loaded = PlayerProfile.FromSnapshot(profile.ToSnapshot());
            Assert.AreEqual(PlayerProfile.CurrentVersion, profile.ToSnapshot().version);
            Assert.IsTrue(loaded.Daily.HasPlayed("2026-11-19"));
            Assert.AreEqual(55, loaded.Daily.Result("2026-11-19").HpPercent);
            Assert.AreEqual(640, loaded.Daily.Result("2026-11-19").Ticks);
        }

        [Test]
        public void AnOlderSaveLoadsWithNoDailyResults()
        {
            var snapshot = new PlayerProfile.Snapshot { version = 5, gold = 10, dailyDays = null, dailyWon = null, dailyHp = null, dailyTicks = null };
            PlayerProfile loaded = PlayerProfile.FromSnapshot(snapshot);
            Assert.AreEqual(0, loaded.Daily.Days.Count);
            Assert.AreEqual(10, loaded.Gold);
        }
    }
}
