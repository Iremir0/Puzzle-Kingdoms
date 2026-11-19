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
using PuzzleKingdoms.Views.Menus;
using PuzzleKingdoms.Views.Puzzle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-30: the Shop tab, gold ads, the Supporter pack and the extra-moves / extra-Rally offers.</summary>
    public class ShopScreenTests
    {
        private string _folder;
        private GameApp _app;
        private TestAds _ads;
        private TestBilling _billing;
        private DateTime _now = new DateTime(2026, 11, 19, 12, 0, 0);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _now = new DateTime(2026, 11, 19, 12, 0, 0);
            _folder = Path.Combine(Path.GetTempPath(), "pk-shop-" + Guid.NewGuid().ToString("N"));
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
            _ads = new TestAds();
            _billing = new TestBilling();
            _app.UseAds(_ads);
            _app.UseBilling(_billing);
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

        private IEnumerator FinishPuzzle(PuzzleScreen puzzle)
        {
            while (!_app.Run.Puzzle.IsFinished)
            {
                Assert.IsTrue(_app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                Assert.IsTrue(puzzle.PlaySwap(a, b));
                yield return new WaitUntil(() => !puzzle.IsBusy);
            }
        }

        [UnityTest]
        public IEnumerator TheShopTabGivesGoldForAnAdAFewTimesADay()
        {
            FindButton(_app.CurrentScreen, "Tab Shop").onClick.Invoke();
            yield return null;
            var shop = (ShopScreen)_app.CurrentScreen;
            int perAd = _app.AdRules.BonusGold;
            int perDay = _app.AdRules.GoldAdsPerDay;

            for (int i = 0; i < perDay; i++)
            {
                Assert.IsNotNull(shop.GoldButton, "an ad is offered");
                shop.GoldButton.onClick.Invoke();
                yield return null;
                shop = (ShopScreen)_app.CurrentScreen;
            }

            Assert.AreEqual(perAd * perDay, _app.Profile.Gold);
            Assert.AreEqual(perDay, _ads.Shown);
            Assert.IsNull(shop.GoldButton, "the day's limit is reached");
            Assert.AreEqual(perAd * perDay, new ProfileStore(_folder).Load().Gold, "saved");

            _now = _now.AddDays(1);
            _app.ShowShop();
            yield return null;
            Assert.IsNotNull(((ShopScreen)_app.CurrentScreen).GoldButton, "a new day, new ads");
        }

        [UnityTest]
        public IEnumerator AFailedAdGivesNothing()
        {
            _ads.Succeeds = false;
            _app.ShowShop();
            yield return null;
            ((ShopScreen)_app.CurrentScreen).GoldButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(0, _app.Profile.Gold);
        }

        [UnityTest]
        public IEnumerator TheSupporterPackIsBoughtOnceAndSkipsTheAds()
        {
            _app.ShowShop();
            yield return null;
            Assert.IsNotNull(((ShopScreen)_app.CurrentScreen).BuyButton);
            ((ShopScreen)_app.CurrentScreen).BuyButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(_app.Profile.AdLedger.Supporter);
            Assert.IsTrue(new ProfileStore(_folder).Load().AdLedger.Supporter, "saved");
            var shop = (ShopScreen)_app.CurrentScreen;
            Assert.IsNull(shop.BuyButton, "bought");
            shop.GoldButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(_app.AdRules.BonusGold, _app.Profile.Gold);
            Assert.AreEqual(0, _ads.Shown, "a Supporter sees no ad");
        }

        [UnityTest]
        public IEnumerator ExtraMovesAfterAnAdReopenThePuzzleOnce()
        {
            _app.StartStage(0);
            yield return null;
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            yield return FinishPuzzle(puzzle);
            Transform button = puzzle.transform.Find("Button Extra moves");
            Assert.IsNotNull(button, "the offer shows when the moves have run out");
            Assert.IsTrue(button.gameObject.activeSelf);

            button.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.AreEqual(1, _ads.Shown);
            Assert.Greater(_app.Run.Puzzle.MovesLeft, 0);
            Assert.AreEqual(StagePhase.Puzzle, _app.Run.Phase);
            Assert.IsFalse(button.gameObject.activeSelf);
            yield return FinishPuzzle(puzzle);
            Assert.IsFalse(_app.Run.CanGrantExtraMoves, "once per attempt");
        }

        [UnityTest]
        public IEnumerator ExtraRallyAfterAnAdAddsAChargeInTheWarCamp()
        {
            _app.StartStage(0);
            yield return null;
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            yield return FinishPuzzle(puzzle);
            _app.ShowWarCamp();
            yield return null;
            int before = _app.Run.RallyCharges;
            Button button = FindButton(_app.CurrentScreen, "Button Extra Rally");
            Assert.IsNotNull(button, "the offer shows in the War Camp");

            button.onClick.Invoke();
            yield return null;

            Assert.AreEqual(1, _ads.Shown);
            AutoFormation.Fill(_app.Run.Roster);
            Assert.IsTrue(_app.Run.StartBattle());
            Assert.AreEqual(before + _app.AdRules.ExtraRally, _app.Run.Battle.Rally.Charges);
        }

        [UnityTest]
        public IEnumerator TheDailyChallengeAndNoAdsHaveNoOffers()
        {
            _app.StartDaily();
            yield return null;
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            yield return FinishPuzzle(puzzle);
            Assert.IsNull(puzzle.transform.Find("Button Extra moves"), "the daily challenge stays comparable");

            _app.UseAds(new NoAds());
            _app.StartStage(0);
            yield return null;
            var campaign = (PuzzleScreen)_app.CurrentScreen;
            yield return FinishPuzzle(campaign);
            Assert.IsNull(campaign.transform.Find("Button Extra moves"), "no ad available, no button");
        }
    }
}
