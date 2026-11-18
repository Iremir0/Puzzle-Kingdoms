using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Views.Battle;
using PuzzleKingdoms.Views.Menus;
using PuzzleKingdoms.Views.Puzzle;
using PuzzleKingdoms.Views.WarCamp;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-13: the stage map and the tutorial prompts, from a fresh profile.</summary>
    public class TutorialFlowTests
    {
        private string _folder;
        private GameApp _app;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-tutorial-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(LoadContent());
            _app.UseStore(new ProfileStore(_folder));
            _app.UsePlaytestLog(new PlaytestLog(_folder));
            _app.UseLanguage(Language.English); // the prompts are checked by their English text
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

        private static GameContent LoadContent()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            Assert.Ignore("needs the editor to load GameContent");
            return null;
#endif
        }

        private void FinishPuzzle()
        {
            while (!_app.Run.Puzzle.IsFinished)
            {
                Assert.IsTrue(_app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                _app.Run.Puzzle.PlaySwap(a, b);
            }
        }

        /// <summary>Taps through every prompt on screen; returns the ids seen.</summary>
        private string DismissAll()
        {
            var ids = new System.Collections.Generic.List<string>();
            int guard = 0;
            while (_app.Tutorial != null && guard++ < 10)
            {
                ids.Add(_app.Tutorial.Current.Id);
                _app.Tutorial.Next();
            }
            return string.Join(",", ids);
        }

        [UnityTest]
        public IEnumerator MapShowsNodesAndOpensTheStageCard()
        {
            var map = (StageMapScreen)_app.CurrentScreen;
            int stages = 0; // Chapter 1 is the map a new player sees (PB-18: later chapters have their own map)
            foreach (var stage in _app.Content.Campaign)
            {
                stages += stage.Chapter == 1 ? 1 : 0;
            }

            Assert.AreEqual(0, map.CurrentStage, "a new player starts at stage 1");
            Assert.AreEqual(0, map.SelectedStage, "its card is open");
            for (int i = 0; i < stages; i++)
            {
                Assert.IsNotNull(map.Node(i));
            }
            Assert.IsNotNull(map.transform.Find("Stage Card/Play Button"));

            map.Node(stages - 1).GetComponent<Button>().onClick.Invoke();
            yield return null; // Ui.Clear destroys the old card content at the end of the frame
            Assert.AreEqual(stages - 1, map.SelectedStage);
            Assert.IsNull(map.transform.Find("Stage Card/Play Button"), "a locked stage has no Play");

            map.Node(0).GetComponent<Button>().onClick.Invoke();
            yield return null;
            map.transform.Find("Stage Card/Play Button").GetComponent<Button>().onClick.Invoke();
            Assert.IsInstanceOf<PuzzleScreen>(_app.CurrentScreen);
        }

        [UnityTest]
        public IEnumerator PromptsShowOnceAtTheRightMoments()
        {
            _app.StartStage(0);
            yield return null;
            Assert.IsNotNull(_app.Tutorial, "puzzle start prompt");
            Assert.AreEqual("1-1.puzzle", _app.Tutorial.Current.Id);
            StringAssert.Contains("Red gems recruit Warriors", _app.Tutorial.CurrentText);
            Assert.IsTrue(_app.Tutorial.transform.Find("Target Frame").gameObject.activeSelf, "points at the board");
            Assert.IsTrue(new ProfileStore(_folder).Load().HasSeenStep("1-1.puzzle"), "saved when shown");
            Assert.AreEqual("1-1.puzzle", DismissAll());

            FinishPuzzle();
            Assert.AreEqual("1-1.unit", DismissAll(), "first recruit prompt, once");

            _app.ShowWarCamp();
            yield return null;
            Assert.AreEqual("1-1.camp", DismissAll());

            AutoFormation.Fill(_app.Run.Roster);
            Assert.IsTrue(_app.Run.StartBattle());
            _app.ShowBattle();
            var battle = (BattleScreen)_app.CurrentScreen;
            Assert.IsNotNull(_app.Tutorial);
            Assert.IsTrue(battle.Paused, "the battle waits for the prompt");
            Assert.AreEqual("1-1.battle", DismissAll());
            Assert.IsFalse(battle.Paused, "and goes on after it");

            // Second attempt: nothing again.
            _app.StartStage(0);
            yield return null;
            Assert.IsNull(_app.Tutorial, "seen prompts never come back");
            _app.ShowWarCamp();
            Assert.IsNull(_app.Tutorial);
        }

        [UnityTest]
        public IEnumerator SkipTurnsTheTutorialOffUntilReset()
        {
            _app.StartStage(1);
            yield return null;
            Assert.AreEqual("1-2.puzzle", _app.Tutorial.Current.Id);

            _app.Tutorial.transform.Find("Bubble/Skip Tutorial").GetComponent<Button>().onClick.Invoke();
            Assert.IsNull(_app.Tutorial);
            Assert.IsTrue(new ProfileStore(_folder).Load().TutorialSkipped, "saved");

            FinishPuzzle();
            _app.ShowWarCamp();
            Assert.IsNull(_app.Tutorial, "no prompts after skipping");

            _app.ShowStageSelect();
            yield return null;
            SettingsPanel panel = _app.OpenSettings();
            Transform reset = panel.transform.Find("Box/Reset Tutorial");
            Assert.IsNotNull(reset);
            reset.GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(_app.Profile.TutorialSkipped);
            Assert.IsFalse(new ProfileStore(_folder).Load().TutorialSkipped);

            _app.StartStage(1);
            Assert.IsNotNull(_app.Tutorial, "prompts are back");
            Assert.AreEqual("1-2.puzzle", _app.Tutorial.Current.Id);
        }

        [UnityTest]
        public IEnumerator TwoPromptsForOneMomentComeOneAfterTheOther()
        {
            _app.StartStage(1);
            DismissAll();
            FinishPuzzle();
            _app.ShowWarCamp();
            yield return null;

            Assert.AreEqual("1-2.archers", _app.Tutorial.Current.Id);
            Assert.IsInstanceOf<WarCampScreen>(_app.CurrentScreen);
            _app.Tutorial.Next();
            Assert.AreEqual("1-2.merge", _app.Tutorial.Current.Id);
            Assert.IsTrue(_app.Tutorial.transform.Find("Target Frame").gameObject.activeSelf, "points at Merge all");
            _app.Tutorial.Next();
            Assert.IsNull(_app.Tutorial);
        }
    }
}
