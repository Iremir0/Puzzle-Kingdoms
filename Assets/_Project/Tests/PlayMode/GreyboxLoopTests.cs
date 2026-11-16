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
        }

        /// <summary>
        /// PB-7: after every animated move (and an invalid one) the board on screen
        /// equals the logic board and every gem image is back in its cell.
        /// Stage 1-3 has stone gems, so stones are checked too.
        /// </summary>
        [UnityTest]
        public IEnumerator PuzzleBoardMatchesLogicAfterEveryAnimation()
        {
            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(LoadContent());
            _app.UseStore(new ProfileStore(_saveFolder));
            yield return null;
            _app.StartStage(2);
            yield return null;
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            var board = _app.Run.Puzzle.Board;

            Assert.IsTrue(TryFindInvalidSwap(board, out GridPos badA, out GridPos badB), "board has a non-matching swap");
            int moves = _app.Run.Puzzle.MovesLeft;
            Assert.IsFalse(puzzle.PlaySwap(badA, badB));
            Assert.IsTrue(puzzle.IsBusy, "the invalid swap is animated");
            yield return new WaitUntil(() => !puzzle.IsBusy);
            Assert.AreEqual(moves, _app.Run.Puzzle.MovesLeft, "an invalid swap uses no move");
            AssertScreenMatchesLogic(puzzle, board, "after the invalid swap");

            for (int move = 1; move <= 20 && !_app.Run.Puzzle.IsFinished; move++)
            {
                Assert.IsTrue(board.TryFindValidMove(out GridPos a, out GridPos b));
                Assert.IsTrue(puzzle.PlaySwap(a, b));
                Assert.IsFalse(puzzle.PlaySwap(a, b), "input is blocked while animating");
                yield return new WaitUntil(() => !puzzle.IsBusy);
                AssertScreenMatchesLogic(puzzle, board, $"after move {move}");
            }
        }

        private static bool TryFindInvalidSwap(Logic.Puzzle.BoardModel board, out GridPos a, out GridPos b)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x + 1 < board.Width; x++)
                {
                    a = new GridPos(x, y);
                    b = new GridPos(x + 1, y);
                    if (!board.WouldSwapMatch(a, b))
                    {
                        return true;
                    }
                }
            }
            a = b = default;
            return false;
        }

        private static void AssertScreenMatchesLogic(PuzzleScreen puzzle, Logic.Puzzle.BoardModel board, string when)
        {
            CollectionAssert.AreEqual(board.ToGrid(), puzzle.ShownGrid, "gems " + when);
            CollectionAssert.AreEqual(board.ToSpecialGrid(), puzzle.ShownSpecials, "special gems " + when);
            CollectionAssert.AreEqual(board.ToStoneGrid(), puzzle.ShownStones, "stones " + when);
            Assert.IsTrue(puzzle.GemsAtRest, "every gem back in its cell " + when);
        }
    }
}
