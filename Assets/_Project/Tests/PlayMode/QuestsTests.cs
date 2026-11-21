using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Quests;
using PuzzleKingdoms.Views.Common;
using PuzzleKingdoms.Views.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-24: the Quests tab, claiming, the badge on the tab bar, the day change and the star chest.</summary>
    public class QuestsScreenTests
    {
        private string _folder;
        private GameApp _app;
        private DateTime _now = new DateTime(2026, 11, 18, 12, 0, 0);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-quests-" + Guid.NewGuid().ToString("N"));
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

        private static QuestEvent Big() =>
            new QuestEvent { Won = true, RallyUsed = 9, Recruited = 40, Merges = 12, Gold = 900, MaxTier = 3 };

        [UnityTest]
        public IEnumerator TheQuestsTabShowsTodaysThreeQuests()
        {
            FindButton(_app.CurrentScreen, "Tab Quests").onClick.Invoke();
            yield return null;

            Assert.IsInstanceOf<QuestsScreen>(_app.CurrentScreen);
            Assert.AreEqual(3, _app.Quests.Daily().Count);
            foreach (QuestStatus quest in _app.Quests.Daily())
            {
                Assert.IsNotNull(_app.CurrentScreen.transform.Find("Quest List/Quest " + quest.Def.Id), quest.Def.Id + " has a row");
            }
        }

        [UnityTest]
        public IEnumerator AFinishedQuestIsClaimedPaidAndSaved()
        {
            _app.ShowQuests();
            yield return null;
            _app.Quests.Record(Big());
            _app.ShowQuests();
            yield return null;
            var screen = (QuestsScreen)_app.CurrentScreen;
            string id = _app.Quests.Daily().First().Def.Id;
            int reward = _app.Quests.Daily().First().Def.RewardGold;

            Button claim = screen.ClaimButton(id);
            Assert.IsNotNull(claim, "a finished quest has a Claim button");
            claim.onClick.Invoke();
            yield return null;

            Assert.AreEqual(reward, _app.Profile.Gold);
            Assert.AreEqual(reward, new ProfileStore(_folder).Load().Gold, "saved at once");
            Assert.IsTrue(_app.Quests.Daily().First().Claimed);
            Assert.IsNull(screen.ClaimButton(id), "no second claim");
        }

        [UnityTest]
        public IEnumerator TheTabBarShowsABadgeWhileSomethingWaits()
        {
            _app.ShowQuests();
            yield return null;
            Assert.IsNull(_app.CurrentScreen.transform.Find("Tab Bar/Tab Quests/Badge"), "nothing to claim yet");

            _app.Quests.Record(Big());
            _app.ShowKingdom();
            yield return null;
            Assert.IsNotNull(_app.CurrentScreen.transform.Find("Tab Bar/Tab Quests/Badge"), "a reward waits");
        }

        [UnityTest]
        public IEnumerator ANewDayDrawsNewQuestsWithNoProgress()
        {
            _app.ShowQuests();
            yield return null;
            _app.Quests.Record(Big());
            Assert.IsTrue(_app.Quests.Daily().All(q => q.Complete));

            _now = _now.AddDays(1);
            _app.ShowQuests();
            yield return null;

            Assert.AreEqual("2026-11-19", _app.Profile.Quests.Day);
            Assert.IsTrue(_app.Quests.Daily().All(q => q.Progress == 0), "a fresh day");
            Assert.IsTrue(_app.Quests.Achievements().Any(q => q.Progress > 0), "achievements stay");
        }

        [UnityTest]
        public IEnumerator AStarChestOpensAtTwelveStarsAndPaysOnce()
        {
            for (int i = 0; i < 4; i++) // PB-38: a chest every 12 stars
            {
                _app.Profile.Apply("fake_" + i, new StageReward { Stars = 3 });
            }
            _app.ShowQuests();
            yield return null;
            var screen = (QuestsScreen)_app.CurrentScreen;
            Assert.IsNotNull(screen.ChestButton, "12 stars make a chest");

            int before = _app.Profile.Gold;
            screen.ChestButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual(before + _app.Content.Quests.Rules.ChestGold, _app.Profile.Gold);
            Assert.IsNull(screen.ChestButton, "the chest is open");
            Assert.AreEqual(1, new ProfileStore(_folder).Load().Quests.ChestsClaimed, "saved");
        }
    }
}
