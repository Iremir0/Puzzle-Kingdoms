using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Views.Battle;
using PuzzleKingdoms.Views.Menus;
using PuzzleKingdoms.Views.Puzzle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-25: the Daily Challenge card on the map, the screen, one attempt a day and the saved result.</summary>
    public class DailyScreenTests
    {
        private string _folder;
        private GameApp _app;
        private DateTime _now = new DateTime(2026, 11, 19, 12, 0, 0);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _now = new DateTime(2026, 11, 19, 12, 0, 0);
            _folder = Path.Combine(Path.GetTempPath(), "pk-daily-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _app = new GameObject("GameApp").AddComponent<GameApp>();
#if UNITY_EDITOR
            _app.SetContent(UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset"));
#else
            Assert.Ignore("needs the editor to load GameContent");
#endif
            _app.TutorialEnabled = false;
            _app.ConsentEnabled = false;
            _app.UseStore(new ProfileStore(_folder));
            _app.UsePlaytestLog(new PlaytestLog(_folder));
            _app.UseLanguage(Language.English);
            _app.UseClock(() => _now);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            TabBar.QuestBadge = 0;
            UnityEngine.Object.Destroy(_app.gameObject);
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                UnityEngine.Object.Destroy(canvas.gameObject);
            }
            foreach (EventSystem system in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                UnityEngine.Object.Destroy(system.gameObject);
            }
            Directory.Delete(_folder, true);
        }

        private static Button FindButton(Component screen, string name) =>
            screen.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);

        [UnityTest]
        public IEnumerator TheMapHasADailyButtonThatOpensTheScreen()
        {
            // PB-37: the button shows once its stage is won (Daily after 1-4, Expedition after 2-3).
            foreach (string stage in new[] { "stage_1_1", "stage_1_2", "stage_1_3", "stage_1_4", "stage_2_1", "stage_2_2", "stage_2_3" })
            {
                _app.Profile.Apply(stage, new PuzzleKingdoms.Logic.Progression.StageReward { Stars = 1 });
            }
            _app.ShowStageSelect();
            Button daily = FindButton(_app.CurrentScreen, "Button Daily");
            Assert.IsNotNull(daily, "the map has the Daily Challenge button");
            daily.onClick.Invoke();
            yield return null;

            Assert.IsInstanceOf<DailyScreen>(_app.CurrentScreen);
            var screen = (DailyScreen)_app.CurrentScreen;
            Assert.IsNotNull(screen.PlayButton, "not played yet today");
            Assert.IsNotNull(screen.transform.Find("Daily Week/Day 2026-11-19"), "today is in the week");
        }

        [UnityTest]
        public IEnumerator PlayingTheDailyStartsItsStageAndKeepsTheStarsAlone()
        {
            _app.ShowDaily();
            yield return null;
            ((DailyScreen)_app.CurrentScreen).PlayButton.onClick.Invoke();
            yield return null;

            Assert.IsInstanceOf<PuzzleScreen>(_app.CurrentScreen);
            Assert.AreEqual("daily-2026-11-19", _app.Run.Stage.Id);
            Assert.AreEqual(DailyChallenge.BoardSeedFor("2026-11-19"), _app.Run.Stage.BoardSeed);

            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            while (!_app.Run.Puzzle.IsFinished)
            {
                Assert.IsTrue(_app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                Assert.IsTrue(puzzle.PlaySwap(a, b));
                yield return new WaitUntil(() => !puzzle.IsBusy);
            }
            _app.ShowWarCamp();
            yield return null;
            AutoFormation.Fill(_app.Run.Roster);
            Assert.IsTrue(_app.Run.StartBattle());
            _app.ShowBattle();
            yield return null;
            var battle = (BattleScreen)_app.CurrentScreen;
            if (battle.Simulator.Rally.CanActivate)
            {
                battle.Simulator.ActivateRally();
            }
            float deadline = Time.realtimeSinceStartup + 60f;
            yield return new WaitUntil(() => _app.CurrentScreen is ResultsScreen || Time.realtimeSinceStartup > deadline);

            Assert.IsInstanceOf<ResultsScreen>(_app.CurrentScreen);
            Assert.IsTrue(_app.Profile.Daily.HasPlayed("2026-11-19"), "the result is in the daily log");
            Assert.AreEqual(0, _app.Profile.TotalStars, "a daily day gives no campaign stars");
            Assert.IsTrue(new ProfileStore(_folder).Load().Daily.HasPlayed("2026-11-19"), "saved");

            _app.ShowDaily();
            yield return null;
            var screen = (DailyScreen)_app.CurrentScreen;
            Assert.IsNull(screen.PlayButton, "one attempt a day");
            Assert.IsNotNull(screen.transform.Find("Daily Result"));
            _app.StartDaily();
            yield return null;
            Assert.IsInstanceOf<DailyScreen>(_app.CurrentScreen, "StartDaily does nothing a second time");
        }

        [UnityTest]
        public IEnumerator ANewDayOffersANewAttempt()
        {
            _app.Profile.RecordDaily(new DailyResult { Day = "2026-11-19", Won = true, HpPercent = 70, Ticks = 500 }, 0);
            _app.ShowDaily();
            yield return null;
            Assert.IsNull(((DailyScreen)_app.CurrentScreen).PlayButton);

            _now = _now.AddDays(1);
            _app.ShowDaily();
            yield return null;
            Assert.IsNotNull(((DailyScreen)_app.CurrentScreen).PlayButton, "tomorrow is a new challenge");
        }
    }
}
