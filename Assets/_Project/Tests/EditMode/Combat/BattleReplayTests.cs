using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Kingdom;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Combat
{
    /// <summary>PB-28 (D-063): a battle saved as text plays out the same, and the stamp notices changed rules.</summary>
    public class BattleReplayTests
    {
        private static readonly UnitDef Warrior = ArmyFixtures.UnitDef("warrior", 260, 24, 1f, 1f, 1.2f);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("archer", 90, 20, 0.8f, 4f, 1f);
        private static readonly UnitDef Mage = ArmyFixtures.UnitDef("mage", 80, 36, 1.6f, 3.5f, 0.9f);
        private static readonly UnitDef Grunt = ArmyFixtures.UnitDef("goblin_grunt", 140, 14, 1f, 1f, 1.2f);
        private static readonly UnitDef Shooter = ArmyFixtures.UnitDef("goblin_archer", 70, 12, 1f, 4f, 1f);

        private static UnitDef Find(string id) =>
            new[] { Warrior, Archer, Mage, Grunt, Shooter }.FirstOrDefault(u => u.Id == id);

        private static StageDef Stage()
        {
            var stage = new StageDef { Id = "replay_test", DisplayName = "Replay test", Moves = 10, EnemyHpMultiplier = 1.3f, EnemyDamageMultiplier = 1.1f };
            stage.Enemies.Add(new EnemySlot { Unit = Grunt, Tier = 1, Cell = new GridPos(0, 0) });
            stage.Enemies.Add(new EnemySlot { Unit = Grunt, Tier = 2, Cell = new GridPos(1, 0) });
            stage.Enemies.Add(new EnemySlot { Unit = Grunt, Tier = 1, Cell = new GridPos(2, 0) });
            stage.Enemies.Add(new EnemySlot { Unit = Shooter, Tier = 1, Cell = new GridPos(1, 2) });
            return stage;
        }

        /// <summary>A finished run with a wounded unit, building bonuses and a Rally pressed by the bot.</summary>
        private static StageRun FinishedRun(RallyVariant variant = null)
        {
            var bonuses = new KingdomBonuses();
            bonuses.Add(BuildingStat.UnitHp, "warrior", 0.1f);
            bonuses.Add(BuildingStat.UnitDamage, "mage", 0.05f);
            var carried = new List<ArmyUnit> { new ArmyUnit(Warrior, 2) { HpFraction = 0.6f } };
            var run = new StageRun(Stage(), new[] { Warrior, Archer, Mage }, new PuzzleRules(), new ArmyRules(), new BattleRules(),
                new ProgressionRules(), new SeededRandom(21), bonuses, variant, carried);
            while (!run.Puzzle.IsFinished)
            {
                run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b);
                run.Puzzle.PlaySwap(a, b);
            }
            AutoFormation.Fill(run.Roster);
            Assert.IsTrue(run.StartBattle());
            int guard = 0;
            while (!run.Battle.IsFinished && guard++ < 5000)
            {
                if (run.Battle.Tick > 40 && run.Battle.Rally.CanActivate)
                {
                    run.Battle.ActivateRally();
                }
                run.Battle.Step();
            }
            return run;
        }

        private static List<(int hp, int x, int y)> Final(BattleSimulator sim) =>
            sim.Combatants.Select(c => (c.Hp, c.Cell.X, c.Cell.Y)).ToList();

        [Test]
        public void ARecordedBattlePlaysOutTheSameFromItsText()
        {
            StageRun run = FinishedRun();
            Assert.Greater(run.Battle.InputLog.Count, 0, "the bot pressed Rally");
            BattleReplay saved = BattleReplay.From(run, "Replay test");

            BattleReplay loaded = BattleReplay.Parse(saved.ToText(), out string error);
            Assert.IsNotNull(loaded, error);
            Assert.AreEqual(saved.ToText(), loaded.ToText(), "the text round-trips");

            BattleSimulator again = loaded.Rebuild(run.Battle.Rules, run.ArmyRules, Find);
            Assert.IsNotNull(again);
            int guard = 0;
            while (!again.IsFinished && guard++ < 5000)
            {
                loaded.Advance(again);
            }
            Assert.AreEqual(run.Battle.Result.Ticks, again.Result.Ticks);
            Assert.AreEqual(run.Battle.Result.Victory, again.Result.Victory);
            Assert.AreEqual(run.Battle.Result.Survivors, again.Result.Survivors);
            CollectionAssert.AreEqual(Final(run.Battle), Final(again), "every unit ends with the same HP");
            CollectionAssert.AreEqual(run.Battle.InputLog, again.InputLog, "Rally was pressed on the same ticks");
        }

        [Test]
        public void AdvanceIntoABufferPlaysTheSameBattle()
        {
            StageRun run = FinishedRun();
            BattleReplay saved = BattleReplay.From(run, "Buffer test");
            BattleSimulator again = saved.Rebuild(run.Battle.Rules, run.ArmyRules, Find);
            var buffer = new List<BattleEvent>();
            int events = 0;
            int guard = 0;
            while (!again.IsFinished && guard++ < 5000)
            {
                saved.Advance(again, buffer);
                events += buffer.Count;
            }
            Assert.Greater(events, 0);
            Assert.AreEqual(run.Battle.Result.Ticks, again.Result.Ticks);
            CollectionAssert.AreEqual(Final(run.Battle), Final(again));
            CollectionAssert.AreEqual(run.Battle.InputLog, again.InputLog);
            saved.Advance(again, buffer);
            Assert.IsEmpty(buffer, "a finished battle clears the buffer");
        }

        [Test]
        public void ACommanderVariantIsKept()
        {
            var variant = new RallyVariant
            {
                Id = "arrow_rain", Kind = RallyKind.ArrowRain, DurationSeconds = 4f, DamageBonus = 0.1f, AttackSpeedBonus = 0.1f,
                VolleyCount = 4, VolleyPercentMaxHp = 0.12f,
            };
            StageRun run = FinishedRun(variant);
            BattleReplay loaded = BattleReplay.Parse(BattleReplay.From(run, "x").ToText(), out string error);

            Assert.IsNotNull(loaded, error);
            Assert.AreEqual(RallyKind.ArrowRain, loaded.Variant.Kind);
            Assert.AreEqual(4, loaded.Variant.VolleyCount);
            Assert.AreEqual(0.12f, loaded.Variant.VolleyPercentMaxHp, 0.0001f);
            BattleSimulator again = loaded.Rebuild(run.Battle.Rules, run.ArmyRules, Find);
            while (!again.IsFinished)
            {
                loaded.Advance(again);
            }
            CollectionAssert.AreEqual(Final(run.Battle), Final(again));
        }

        [Test]
        public void TheStampNoticesChangedRulesAndUnits()
        {
            StageRun run = FinishedRun();
            BattleReplay replay = BattleReplay.From(run, "x");
            Assert.IsTrue(replay.IsCurrent(run.Battle.Rules, run.ArmyRules, Find));

            Assert.IsFalse(replay.IsCurrent(new BattleRules { FrontGap = 5f }, run.ArmyRules, Find), "battle rules changed");
            Assert.IsFalse(replay.IsCurrent(run.Battle.Rules, new ArmyRules { MergeCount = 4 }, Find), "army rules changed");
            UnitDef buffed = ArmyFixtures.UnitDef("warrior", 300, 24, 1f, 1f, 1.2f);
            Assert.IsFalse(replay.IsCurrent(run.Battle.Rules, run.ArmyRules, id => id == "warrior" ? buffed : Find(id)), "a unit changed");
        }

        [Test]
        public void ADamagedOrForeignTextIsRefusedAndUnknownUnitsFailToRebuild()
        {
            StageRun run = FinishedRun();
            string text = BattleReplay.From(run, "x").ToText();

            Assert.IsNull(BattleReplay.Parse("", out string e1));
            Assert.IsNotEmpty(e1);
            Assert.IsNull(BattleReplay.Parse("hello", out string e2));
            Assert.IsNotEmpty(e2);
            Assert.IsNull(BattleReplay.Parse(text.Replace("pk-replay 1", "pk-replay 99"), out string e3));
            StringAssert.Contains("newer", e3);
            Assert.IsNull(BattleReplay.Parse(text.Replace("rally=", "rally=abc"), out string e4));
            StringAssert.Contains("damaged", e4);
            Assert.IsNull(BattleReplay.Parse(string.Join("\n", text.Split('\n').Where(l => !l.StartsWith("p="))), out string e5));
            StringAssert.Contains("incomplete", e5);

            BattleReplay replay = BattleReplay.Parse(text, out _);
            Assert.IsNull(replay.Rebuild(run.Battle.Rules, run.ArmyRules, id => null), "no units known");
        }
    }
}
