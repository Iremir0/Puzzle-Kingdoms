using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-31: the cloud row in Settings, uploading, taking the cloud save on a new phone and the conflict question.</summary>
    public class CloudScreenTests
    {
        private string _folder;
        private GameApp _app;
        private TestCloud _cloud;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-cloud-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _cloud = new TestCloud();
            yield return StartApp();
        }

        private IEnumerator StartApp()
        {
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
            _app.UseCloud(_cloud);
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

        private static string SnapshotJson(Action<PlayerProfile> setUp)
        {
            var profile = new PlayerProfile();
            setUp(profile);
            return JsonUtility.ToJson(profile.ToSnapshot());
        }

        private static StageReward Stars(int stars) => new StageReward { Stars = stars };

        [UnityTest]
        public IEnumerator SettingsHasACloudRowThatSignsInAndOutAndUploadsTheSave()
        {
            _app.Profile.Apply("1-1", Stars(3));
            SettingsPanel panel = _app.OpenSettings();
            yield return null;
            Button button = FindButton(panel, "Cloud Button");
            Assert.IsNotNull(button, "the cloud row shows when a cloud exists");
            Assert.IsFalse(_cloud.IsSignedIn);

            button.onClick.Invoke();
            yield return null;
            Assert.IsTrue(_cloud.IsSignedIn);
            Assert.IsNotNull(_cloud.Stored, "the first sign-in uploads this phone's save");
            Assert.AreEqual(1, JsonUtility.FromJson<PlayerProfile.Snapshot>(_cloud.Stored.Json).stageIds.Count);

            button.onClick.Invoke();
            yield return null;
            Assert.IsFalse(_cloud.IsSignedIn, "signed out again");
        }

        [UnityTest]
        public IEnumerator ANewPhoneTakesTheCloudSave()
        {
            _cloud.SignIn(null);
            _cloud.Stored = new CloudData
            {
                Json = SnapshotJson(p => { p.Apply("1-1", Stars(3)); p.Apply("1-2", Stars(2)); }),
                SavedUtc = DateTime.UtcNow,
            };

            _app.SyncCloud();
            yield return null;

            Assert.AreEqual(5, _app.Profile.TotalStars, "the fresh phone now has the cloud progress");
            Assert.AreEqual(5, new ProfileStore(_folder).Load().TotalStars, "and wrote it to disk");
            Assert.IsInstanceOf<StageMapScreen>(_app.CurrentScreen);
            Assert.IsNull(_app.ConflictPanel, "nothing to ask");
        }

        [UnityTest]
        public IEnumerator TwoSavesEachAheadMakeThePlayerChooseAndBothChoicesWork()
        {
            _cloud.SignIn(null);
            _app.Profile.Apply("1-1", Stars(3)); // this phone: stage 1-1 with three stars
            _cloud.Stored = new CloudData
            {
                Json = SnapshotJson(p => { p.Apply("1-1", Stars(1)); p.Apply("1-2", Stars(2)); }), // the cloud: another stage
                SavedUtc = DateTime.UtcNow,
            };

            _app.SyncCloud();
            yield return null;
            Assert.IsNotNull(_app.ConflictPanel, "each side is ahead somewhere: ask");
            Assert.AreEqual(3, _app.Profile.TotalStars, "nothing changed yet");

            FindButton(_app.ConflictPanel, "Button Use cloud save").onClick.Invoke();
            yield return null;
            Assert.AreEqual(3, _app.Profile.TotalStars, "1 + 2 stars from the cloud");
            Assert.AreEqual(2, _app.Profile.BestStars("1-2"));
            Assert.IsNull(_app.ConflictPanel);

            // Ask again the other way: now keep this phone and the cloud copy is replaced.
            _app.Profile.Apply("1-3", Stars(3));
            _cloud.Stored = new CloudData
            {
                Json = SnapshotJson(p => { p.Apply("1-1", Stars(3)); p.Apply("1-4", Stars(1)); }),
                SavedUtc = DateTime.UtcNow,
            };
            _app.SyncCloud();
            yield return null;
            Assert.IsNotNull(_app.ConflictPanel);
            FindButton(_app.ConflictPanel, "Button Keep this phone").onClick.Invoke();
            yield return null;
            PlayerProfile.Snapshot uploaded = JsonUtility.FromJson<PlayerProfile.Snapshot>(_cloud.Stored.Json);
            Assert.IsTrue(uploaded.stageIds.Contains("1-3"), "this phone's save went up");
        }

        [UnityTest]
        public IEnumerator ChangesGoUpWhenFlushedAndNothingGoesUpSignedOut()
        {
            _app.ShowShop();
            yield return null;
            ((ShopScreen)_app.CurrentScreen).BuyButton.onClick.Invoke(); // a save: the Supporter pack
            yield return null;
            _app.FlushCloud();
            Assert.AreEqual(0, _cloud.Writes, "signed out: nothing is uploaded");

            _cloud.SignIn(null);
            _app.SyncCloud();
            yield return null;
            int writes = _cloud.Writes;
            Assert.Greater(writes, 0, "signing in uploads");
            Assert.IsTrue(JsonUtility.FromJson<PlayerProfile.Snapshot>(_cloud.Stored.Json).supporter);

            _app.ShowShop();
            yield return null;
            _app.Profile.AddGold(50);
            _app.ResetProgress(); // a save, followed at once by an upload of the empty profile
            yield return null;
            Assert.Greater(_cloud.Writes, writes);
            Assert.AreEqual(0, JsonUtility.FromJson<PlayerProfile.Snapshot>(_cloud.Stored.Json).gold, "the reset reached the cloud too");
        }
    }
}
