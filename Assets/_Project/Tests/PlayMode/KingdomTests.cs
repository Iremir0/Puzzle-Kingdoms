using System.Collections;
using System.IO;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using PuzzleKingdoms.Views.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-22: the tab bar, the Kingdom screen, and that upgrades are paid, saved and used in battle.</summary>
    public class KingdomScreenTests
    {
        private string _folder;
        private GameApp _app;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-kingdom-" + System.Guid.NewGuid().ToString("N"));
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

        private static Button FindButton(Component screen, string name)
        {
            foreach (Button button in screen.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                {
                    return button;
                }
            }
            return null;
        }

        /// <summary>Wins the stages that open every system and marks their intros as seen (PB-37).</summary>
        private void OpenEverything()
        {
            foreach (string stage in new[] { "stage_1_1", "stage_1_2", "stage_1_3", "stage_1_4", "stage_2_1", "stage_2_2", "stage_2_3" })
            {
                _app.Profile.Apply(stage, new StageReward { Stars = 1, VictoryGold = 0 });
            }
            foreach (Feature feature in System.Enum.GetValues(typeof(Feature)))
            {
                _app.Profile.MarkStepSeen(FeatureUnlockRules.IntroId(feature));
            }
            _app.ShowStageSelect();
        }

        [UnityTest]
        public IEnumerator ANewPlayerSeesLockedTabsThenOneIntroPerSystem()
        {
            // PB-37 (D-070): nothing but the map is open at first; each system opens after its stage, with an intro.
            Assert.IsFalse(FindButton(_app.CurrentScreen, "Tab Kingdom").interactable);
            StringAssert.Contains("1-2", FindButton(_app.CurrentScreen, "Tab Kingdom").GetComponentInChildren<Text>().text);
            Assert.IsNull(_app.IntroPanel);

            _app.Profile.Apply("stage_1_1", new StageReward { Stars = 1, VictoryGold = 10 });
            _app.Profile.Apply("stage_1_2", new StageReward { Stars = 1, VictoryGold = 10 });
            _app.ShowStageSelect();
            yield return null;
            Assert.IsNotNull(_app.IntroPanel, "Quests opened");
            Assert.AreEqual("Intro Quests", _app.IntroPanel.gameObject.name);
            FindButton(_app.IntroPanel, "Close").onClick.Invoke();
            yield return null;
            Assert.IsNotNull(_app.IntroPanel, "then the Kingdom");
            Assert.AreEqual("Intro Kingdom", _app.IntroPanel.gameObject.name);
            FindButton(_app.IntroPanel, "Close").onClick.Invoke();
            yield return null;
            Assert.IsNull(_app.IntroPanel);
            Assert.IsTrue(FindButton(_app.CurrentScreen, "Tab Kingdom").interactable);
            Assert.IsFalse(FindButton(_app.CurrentScreen, "Tab Shop").interactable, "the Shop opens after 2-1");

            _app.ShowStageSelect();
            yield return null;
            Assert.IsNull(_app.IntroPanel, "an intro is shown once");
        }

        [UnityTest]
        public IEnumerator TheTabBarOpensTheKingdomAndGoesBack()
        {
            OpenEverything();
            yield return null;
            Assert.IsInstanceOf<StageMapScreen>(_app.CurrentScreen);
            FindButton(_app.CurrentScreen, "Tab Kingdom").onClick.Invoke();
            yield return null;
            Assert.IsInstanceOf<KingdomScreen>(_app.CurrentScreen);
            Assert.IsTrue(FindButton(_app.CurrentScreen, "Tab Commanders").interactable, "Commanders opens since PB-23");
            Assert.IsTrue(FindButton(_app.CurrentScreen, "Tab Shop").interactable, "the Shop is open (PB-30)");
            FindButton(_app.CurrentScreen, "Tab Map").onClick.Invoke();
            yield return null;
            Assert.IsInstanceOf<StageMapScreen>(_app.CurrentScreen);
        }

        [UnityTest]
        public IEnumerator UpgradesNeedGoldThenSpendAndSaveIt()
        {
            _app.ShowKingdom();
            yield return null;
            var screen = (KingdomScreen)_app.CurrentScreen;
            Assert.IsFalse(screen.UpgradeButton("barracks").interactable, "no gold, no upgrade");

            _app.Profile.Apply("stage_1_1", new StageReward { Stars = 1, VictoryGold = 100 });
            _app.ShowKingdom();
            yield return null;
            screen = (KingdomScreen)_app.CurrentScreen;
            Button barracks = screen.UpgradeButton("barracks");
            Assert.IsTrue(barracks.interactable);
            barracks.onClick.Invoke();
            yield return null;

            Assert.AreEqual(1, _app.Profile.BuildingLevel("barracks"));
            Assert.AreEqual(40, _app.Profile.Gold, "level 1 costs 60");
            PlayerProfile saved = new ProfileStore(_folder).Load();
            Assert.AreEqual(1, saved.BuildingLevel("barracks"), "saved at once");
            Assert.AreEqual(40, saved.Gold);
            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(40, screen.ShownGold, "the gold counter settled");
        }


        [UnityTest]
        public IEnumerator CommandersUnlockByStagesAndTheChoiceIsSaved()
        {
            _app.ShowCommanders();
            yield return null;
            var screen = (CommandersScreen)_app.CurrentScreen;
            Assert.IsNull(screen.ChooseButton("shield_wall"), "locked: no Choose button");
            Assert.IsNull(screen.ChooseButton(""), "with no commander chosen the classic Rally is the one leading");

            _app.Profile.Apply("stage_1_3", new StageReward { Stars = 1 });
            _app.ShowCommanders();
            yield return null;
            screen = (CommandersScreen)_app.CurrentScreen;
            screen.ChooseButton("shield_wall").onClick.Invoke();
            yield return null;

            Assert.AreEqual("shield_wall", _app.Profile.CommanderId);
            Assert.AreEqual("shield_wall", new ProfileStore(_folder).Load().CommanderId, "saved at once");
            Assert.IsNull(screen.ChooseButton("shield_wall"), "the leading commander has no Choose button");
            Assert.IsNotNull(screen.ChooseButton(""), "the classic Rally can be chosen again");
        }
    }
}
