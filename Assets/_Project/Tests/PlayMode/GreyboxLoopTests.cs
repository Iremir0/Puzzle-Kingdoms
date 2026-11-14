using System.Collections;
using System.IO;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Views.Battle;
using PuzzleKingdoms.Views.Menus;
using PuzzleKingdoms.Views.Puzzle;
using PuzzleKingdoms.Views.WarCamp;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>Plays stage 1-1 through every grey-box screen, as a player would, at high speed.</summary>
    public class GreyboxLoopTests
    {
        private string _saveFolder;
        private GameApp _app;
        private float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale;
            Time.timeScale = 8f;
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-playmode-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _timeScale;
            if (_app != null)
            {
                Object.Destroy(_app.gameObject);
            }
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                Object.Destroy(canvas.gameObject);
            }
            foreach (EventSystem system in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                Object.Destroy(system.gameObject);
            }
            Directory.Delete(_saveFolder, true);
        }

        private static GameContent LoadContent()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            Assert.Ignore("needs the editor to load GameContent");
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator Stage11PlaysFromStageMapToResults()
        {
            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(LoadContent());
            _app.UseStore(new ProfileStore(_saveFolder));
            _app.UsePlaytestLog(new PlaytestLog(_saveFolder));
            yield return null;
            Assert.IsInstanceOf<StageSelectScreen>(_app.CurrentScreen);

            _app.StartStage(0);
            yield return null;
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            while (!_app.Run.Puzzle.IsFinished)
            {
                Assert.IsTrue(_app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                Assert.IsTrue(puzzle.PlaySwap(a, b));
                yield return new WaitUntil(() => !puzzle.IsBusy);
            }
            Assert.Greater(_app.Run.Roster.Bench.Count, 0, "the puzzle recruits units");

            _app.ShowWarCamp();
            yield return null;
            Assert.IsInstanceOf<WarCampScreen>(_app.CurrentScreen);
            AutoFormation.Fill(_app.Run.Roster);
            Assert.IsTrue(_app.Run.StartBattle());

            _app.ShowBattle();
            yield return null;
            var battle = (BattleScreen)_app.CurrentScreen;
            if (battle.Simulator.Rally.CanActivate)
            {
                Assert.IsTrue(battle.Simulator.ActivateRally());
            }
            float deadline = Time.realtimeSinceStartup + 30f;
            yield return new WaitUntil(() => _app.CurrentScreen is ResultsScreen || Time.realtimeSinceStartup > deadline);

            Assert.IsInstanceOf<ResultsScreen>(_app.CurrentScreen, "battle ends and results show");
            Assert.IsNotNull(_app.Run.Reward);
            Assert.IsTrue(File.Exists(Path.Combine(_saveFolder, ProfileStore.FileName)), "profile saved");
            Assert.AreEqual(_app.Run.Reward.TotalGold, _app.Profile.Gold);

            string[] log = File.ReadAllLines(Path.Combine(_saveFolder, PlaytestLog.FileName));
            Assert.AreEqual(1, log.Length, "one playtest log line per finished attempt");
            var attempt = JsonUtility.FromJson<PuzzleKingdoms.Logic.Flow.StageAttempt>(log[0]);
            Assert.AreEqual(_app.Run.Stage.Id, attempt.stage);
            Assert.AreEqual(_app.Run.Battle.Result.Victory, attempt.victory);
            Assert.AreEqual(1, attempt.attempt);
            Assert.GreaterOrEqual(attempt.puzzleSeconds, 0f);
        }
    }
}
