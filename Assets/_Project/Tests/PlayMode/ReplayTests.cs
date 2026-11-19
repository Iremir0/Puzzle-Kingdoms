using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
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
    public class ReplayScreenTests
    {
        private string _folder;
        private GameApp _app;
        private DateTime _now = new DateTime(2026, 11, 19, 12, 0, 0);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _now = new DateTime(2026, 11, 19, 12, 0, 0);
            _folder = Path.Combine(Path.GetTempPath(), "pk-replay-" + Guid.NewGuid().ToString("N"));
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

        /// <summary>Plays campaign stage 1 to the end the way a player would and waits for the results screen.</summary>
        private IEnumerator FightStageOne()
        {
            _app.StartStage(0);
            yield return null;
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
        }

        [UnityTest]
        public IEnumerator ABattleIsSavedAndPlaysBackToTheSameEnd()
        {
            yield return FightStageOne();
            BattleResult original = _app.Run.Battle.Result;
            Assert.AreEqual(1, _app.Profile.Replays.Entries.Count, "the battle is saved as a replay");
            Assert.AreEqual(1, new ProfileStore(_folder).Load().Replays.Entries.Count, "and written to the save");

            _app.ShowReplays();
            yield return null;
            var list = (ReplaysScreen)_app.CurrentScreen;
            Assert.IsNotNull(list.WatchButton(0));
            Assert.IsTrue(list.WatchButton(0).interactable, "made with these rules");
            list.WatchButton(0).onClick.Invoke();
            yield return null;

            var replay = (BattleScreen)_app.CurrentScreen;
            Assert.IsTrue(replay.IsReplay);
            Assert.AreEqual(0, replay.Simulator.Tick);
            replay.OnSkip();
            Assert.AreEqual(original.Ticks, replay.Simulator.Result.Ticks);
            Assert.AreEqual(original.Victory, replay.Simulator.Result.Victory);
            Assert.AreEqual(original.Survivors, replay.Simulator.Result.Survivors);
            Assert.AreEqual(1f, replay.Timeline.FillFraction, 0.01f, "the timeline is full at the end");
        }

        [UnityTest]
        public IEnumerator TappingTheTimelineJumpsAndStarringKeepsAReplay()
        {
            yield return FightStageOne();
            _app.ShowReplays();
            yield return null;
            ((ReplaysScreen)_app.CurrentScreen).WatchButton(0).onClick.Invoke();
            yield return null;

            ((BattleScreen)_app.CurrentScreen).Timeline.SeekTo(0.5f);
            yield return null;
            var jumped = (BattleScreen)_app.CurrentScreen;
            Assert.IsTrue(jumped.IsReplay);
            Assert.Greater(jumped.Simulator.Tick, 0, "the new screen starts part-way");
            Assert.AreEqual(0.5f, jumped.Timeline.FillFraction, 0.05f);

            _app.ShowReplays();
            yield return null;
            ((ReplaysScreen)_app.CurrentScreen).StarButton(0).onClick.Invoke();
            yield return null;
            Assert.IsTrue(_app.Profile.Replays.Entries[0].Starred);
            Assert.IsTrue(new ProfileStore(_folder).Load().Replays.Entries[0].Starred, "saved");
        }

        [UnityTest]
        public IEnumerator SettingsHasAReplaysButton()
        {
            SettingsPanel panel = _app.OpenSettings();
            yield return null;
            Button replays = panel.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "Replays");
            Assert.IsNotNull(replays);
            replays.onClick.Invoke();
            yield return null;
            Assert.IsInstanceOf<ReplaysScreen>(_app.CurrentScreen);
        }
    }
}
