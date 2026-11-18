using System.Collections;
using System.IO;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic.Analytics;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-29: the consent question on first launch, its answers, and asking again from Settings.</summary>
    public class ConsentFlowTests
    {
        private string _folder;
        private GameApp _app;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-consent-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            Cleanup();
            Directory.Delete(_folder, true);
        }

        private void Cleanup()
        {
            if (_app != null)
            {
                Object.DestroyImmediate(_app.gameObject);
            }
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(canvas.gameObject);
            }
            foreach (EventSystem system in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(system.gameObject);
            }
        }

        private IEnumerator Launch()
        {
            _app = new GameObject("GameApp").AddComponent<GameApp>();
#if UNITY_EDITOR
            _app.SetContent(UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset"));
#else
            Assert.Ignore("needs the editor to load GameContent");
#endif
            _app.TutorialEnabled = false;
            _app.UseStore(new ProfileStore(_folder));
            _app.UsePlaytestLog(new PlaytestLog(_folder));
            _app.UseLanguage(Language.English);
            yield return null;
        }

        private static void Press(Component root, string name)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                {
                    button.onClick.Invoke();
                    return;
                }
            }
            Assert.Fail(name + " not found");
        }

        private GameSettings Saved() => new SettingsStore(_folder).Load();

        private string EventLog => Path.Combine(_folder, LocalEventLog.FileName);

        [UnityTest]
        public IEnumerator FirstLaunchAsksAndNothingIsRecordedBeforeTheAnswer()
        {
            yield return Launch();
            ConsentPanel panel = _app.ConsentPrompt;
            Assert.IsNotNull(panel, "asked on the first launch");
            Assert.IsTrue(panel.transform.IsChildOf(_app.CurrentScreen.transform), "over the stage map");
            Assert.IsFalse(File.Exists(EventLog), "nothing recorded before the answer");

            Press(panel, "Consent Analytics"); // switch statistics on, leave the rest off
            Press(panel, "Save Choice");
            yield return null;
            Assert.IsTrue(panel == null, "closed");
            GameSettings saved = Saved();
            Assert.IsFalse(Consent.NeedsAsking(saved));
            Assert.IsTrue(saved.analytics);
            Assert.IsFalse(saved.crashReports);
            Assert.IsFalse(saved.personalisedAds);

            _app.StartStage(0);
            yield return null;
            string log = File.ReadAllText(EventLog);
            StringAssert.Contains("\"event\":\"app_started\"", log, "the session counts once the player said yes");
            StringAssert.Contains("\"event\":\"stage_started\",", log);
        }

        [UnityTest]
        public IEnumerator SayingNoRecordsNothingAndIsNotAskedAgain()
        {
            yield return Launch();
            Press(_app.ConsentPrompt, "Save Choice"); // every switch still off
            yield return null;
            _app.StartStage(0);
            yield return null;
            Assert.IsFalse(File.Exists(EventLog));

            Cleanup();
            yield return Launch();
            Assert.IsNull(_app.ConsentPrompt, "answered: not asked on the next launch");
        }

        [UnityTest]
        public IEnumerator SettingsOpenTheQuestionWithTheSavedAnswer()
        {
            yield return Launch();
            Press(_app.ConsentPrompt, "Allow All");
            yield return null;
            Assert.IsTrue(Saved().analytics && Saved().crashReports && Saved().personalisedAds);

            SettingsPanel settings = _app.OpenSettings();
            Press(settings, "Privacy");
            ConsentPanel again = _app.ConsentPrompt;
            Assert.IsNotNull(again);
            Text analyticsCaption = FindButton(again, "Consent Analytics").GetComponentInChildren<Text>();
            Assert.AreEqual("ON", analyticsCaption.text, "shows the saved answer");

            Press(again, "Consent Ads");
            Press(again, "Save Choice");
            yield return null;
            Assert.IsFalse(Saved().personalisedAds);

            Press(settings, "Speed 2x"); // the settings panel's older copy must not undo the new answer
            Assert.IsFalse(Saved().personalisedAds);
            Assert.IsTrue(Saved().analytics);
            Assert.AreEqual(2, Saved().BattleSpeed);
        }

        private static Button FindButton(Component root, string name)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                {
                    return button;
                }
            }
            return null;
        }
    }
}
