using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
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
    /// <summary>PB-26: the Expedition button on the map, the road screen, a battle with the carried army, resuming and giving up.</summary>
    public class ExpeditionScreenTests
    {
        private string _folder;
        private GameApp _app;
        private DateTime _now = new DateTime(2026, 11, 19, 12, 0, 0);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _now = new DateTime(2026, 11, 19, 12, 0, 0);
            _folder = Path.Combine(Path.GetTempPath(), "pk-expedition-" + Guid.NewGuid().ToString("N"));
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
        public IEnumerator TheMapHasAnExpeditionButtonThatOpensTheRoad()
        {
            Button button = FindButton(_app.CurrentScreen, "Button Expedition");
            Assert.IsNotNull(button, "the map has the Expedition button");
            button.onClick.Invoke();
            yield return null;

            Assert.IsInstanceOf<ExpeditionScreen>(_app.CurrentScreen);
            var screen = (ExpeditionScreen)_app.CurrentScreen;
            Assert.IsNotNull(screen.StartButton);
            Assert.IsNotNull(_app.Profile.ActiveExpedition, "a new run is saved at once");
            Assert.AreEqual(5, screen.transform.Find("Expedition Path").childCount, "five steps on the road");
        }

        [UnityTest]
        public IEnumerator ABattleCarriesTheArmyOnAndTheRunCanBeResumedOrGivenUp()
        {
            _app.ShowExpedition();
            yield return null;
            ulong seed = _app.CurrentExpedition.Seed;
            ((ExpeditionScreen)_app.CurrentScreen).StartButton.onClick.Invoke();
            yield return null;

            Assert.IsInstanceOf<PuzzleScreen>(_app.CurrentScreen);
            Assert.AreEqual("expedition-1", _app.Run.Stage.Id);
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
            battle.OnSkip(); // resolve the rest at once: a long battle must not decide the test (the screen waits a moment, then leaves)
            float deadline = Time.realtimeSinceStartup + 20f;
            yield return new WaitUntil(() => _app.CurrentScreen is ExpeditionScreen || Time.realtimeSinceStartup > deadline);

            Assert.IsInstanceOf<ExpeditionScreen>(_app.CurrentScreen, "the road again after the battle");
            Expedition run = _app.CurrentExpedition;
            Assert.IsNotNull(_app.Profile.ActiveExpedition);
            Assert.AreEqual(seed, run.Seed);
            if (run.Phase == ExpeditionPhase.Reward)
            {
                Assert.AreEqual(1, run.Step);
                Assert.AreEqual(3, run.Offers.Count);
                Assert.IsNotNull(((ExpeditionScreen)_app.CurrentScreen).OfferButton(0));
                ((ExpeditionScreen)_app.CurrentScreen).OfferButton(0).onClick.Invoke();
                yield return null;
                Assert.AreEqual(ExpeditionPhase.Battle, _app.CurrentExpedition.Phase, "after a pick the next battle waits");
                Assert.IsNotNull(((ExpeditionScreen)_app.CurrentScreen).StartButton);
            }
            else
            {
                Assert.AreEqual(ExpeditionPhase.Lost, run.Phase);
                Assert.IsNotNull(((ExpeditionScreen)_app.CurrentScreen).CollectButton);
            }

            // A stored run comes back the same after the game forgets it from memory.
            Assert.IsNotNull(Expedition.FromSave(new ProfileStore(_folder).Load().ActiveExpedition, _app.Content.Expedition.Rules()));

            ((Button)FindButton(_app.CurrentScreen, "Button Give up") ?? FindButton(_app.CurrentScreen, "Button Collect")).onClick.Invoke();
            yield return null;
            Assert.IsInstanceOf<StageMapScreen>(_app.CurrentScreen);
            Assert.IsNull(_app.Profile.ActiveExpedition, "the run is over");
            Assert.AreEqual(0, _app.Profile.TotalStars, "an expedition gives no campaign stars");
        }
    }
}
