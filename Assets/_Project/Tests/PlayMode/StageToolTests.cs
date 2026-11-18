using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Views.Menus;
using PuzzleKingdoms.Views.Puzzle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>
    /// PB-19: "The Quarry" was made with the Stage Editor and is not in the campaign; it plays from the
    /// editor (<see cref="GameApp.StartStage(StageDefinition)"/>) with its painted board and its goal.
    /// </summary>
    public class StageToolTests
    {
        private const string QuarryPath = "Assets/_Project/Data/Stages/Stage_1_4.asset";
        private string _folder;
        private GameApp _app;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-stagetool-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _app = new GameObject("GameApp").AddComponent<GameApp>();
#if UNITY_EDITOR
            _app.SetContent(UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset"));
#else
            Assert.Ignore("needs the editor to load the assets");
#endif
            _app.TutorialEnabled = false;
            _app.ConsentEnabled = false;
            _app.UseStore(new ProfileStore(_folder));
            _app.UsePlaytestLog(new PlaytestLog(_folder));
            _app.UseLanguage(Language.English);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_app.gameObject);
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                Object.Destroy(canvas.gameObject);
            }
            foreach (EventSystem system in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                Object.Destroy(system.gameObject);
            }
            Directory.Delete(_folder, true);
        }

        [UnityTest]
        public IEnumerator TheQuarryPlaysWithItsBoardAndGoal()
        {
#if UNITY_EDITOR
            var quarry = UnityEditor.AssetDatabase.LoadAssetAtPath<StageDefinition>(QuarryPath);
#else
            StageDefinition quarry = null;
#endif
            Assert.IsNotNull(quarry);
            Assert.IsFalse(_app.Content.Campaign.Contains(quarry), "made with the tool, not in the campaign yet");

            _app.StartStage(quarry);
            yield return null;
            Assert.IsInstanceOf<PuzzleScreen>(_app.CurrentScreen);
            Assert.AreEqual(StageGoalKind.BreakStones, _app.Run.Goal.Kind);
            Assert.IsTrue(_app.Run.Puzzle.Board.IsStone(new GridPos(2, 0)), "the painted stone is there");
            Text goal = _app.CurrentScreen.GetComponentsInChildren<Text>().FirstOrDefault(t => t.name == "Goal");
            Assert.IsNotNull(goal, "the goal shows on the puzzle screen");
            StringAssert.StartsWith("Goal: break 5 stones", goal.text);

            while (!_app.Run.Puzzle.IsFinished)
            {
                Assert.IsTrue(_app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                _app.Run.Puzzle.PlaySwap(a, b);
            }
            AutoFormation.Fill(_app.Run.Roster);
            Assert.IsTrue(_app.Run.StartBattle());
            while (!_app.Run.Battle.IsFinished)
            {
                _app.Run.Battle.Step();
            }
            _app.ShowResults();
            yield return null;
            Assert.IsInstanceOf<ResultsScreen>(_app.CurrentScreen);
            Assert.IsFalse(_app.CurrentScreen.GetComponentsInChildren<Button>().Any(b => b.name == "Button Next stage"),
                "a stage outside the campaign has no next stage");
            Assert.AreEqual(_app.Run.Won, _app.Profile.BestStars("stage_1_4") > 0, "the result is saved like any stage");
        }
    }
}
