using System.Collections;
using System.IO;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Battle;
using PuzzleKingdoms.Views.Common;
using PuzzleKingdoms.Views.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-14: the settings panel, where settings are saved, and resetting progress.</summary>
    public class SettingsTests
    {
        private string _folder;
        private GameApp _app;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-settings-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(LoadContent());
            _app.TutorialEnabled = false; // PB-13 prompts are tested in TutorialFlowTests
            _app.UseStore(new ProfileStore(_folder));
            _app.UsePlaytestLog(new PlaytestLog(_folder));
            yield return null;
            Assert.IsInstanceOf<StageMapScreen>(_app.CurrentScreen);
        }

        [TearDown]
        public void TearDown()
        {
            Sfx.EffectsOn = true;
            Sfx.MusicOn = true;
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

        private GameSettings SavedSettings() => new SettingsStore(_folder).Load();

        private static void Press(Component root, string name)
        {
            Transform found = Find(root.transform, name);
            Assert.IsNotNull(found, name);
            Button button = found.GetComponent<Button>();
            Assert.IsTrue(button.isActiveAndEnabled && button.interactable, name + " can be pressed");
            button.onClick.Invoke();
        }

        private static Transform Find(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }

        [UnityTest]
        public IEnumerator TogglesAreSavedAndApplied()
        {
            Press(_app.CurrentScreen, "Button Settings");
            SettingsPanel panel = _app.CurrentScreen.GetComponentInChildren<SettingsPanel>();
            Assert.IsNotNull(panel, "the gear opens the settings");

            Press(panel, "Sound Toggle");
            Assert.IsFalse(Sfx.EffectsOn);
            Assert.IsTrue(Sfx.MusicOn);
            Assert.IsFalse(SavedSettings().sound, "saved at once");

            Press(panel, "Music Toggle");
            Press(panel, "Speed 2x");
            GameSettings saved = SavedSettings();
            Assert.IsFalse(saved.music);
            Assert.AreEqual(2, saved.BattleSpeed);
            Assert.IsFalse(Sfx.MusicOn);

            Press(panel, "Close");
            yield return null;
            Assert.IsTrue(panel == null, "Close removes the panel");
        }

        [UnityTest]
        public IEnumerator PuzzleSoundButtonIsRemembered()
        {
            Sfx.Muted = true; // what the puzzle screen's sound button does

            GameSettings saved = SavedSettings();
            Assert.IsFalse(saved.sound);
            Assert.IsFalse(saved.music);
            Assert.IsFalse(_app.Settings.sound);
            yield break;
        }

        [UnityTest]
        public IEnumerator BattleStartsAtTheChosenSpeed()
        {
            Press(_app.CurrentScreen, "Button Settings");
            Press(_app.CurrentScreen.GetComponentInChildren<SettingsPanel>(), "Speed 2x");

            _app.StartStage(0);
            while (!_app.Run.Puzzle.IsFinished)
            {
                Assert.IsTrue(_app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                _app.Run.Puzzle.PlaySwap(a, b);
            }
            _app.ShowWarCamp();
            AutoFormation.Fill(_app.Run.Roster);
            Assert.IsTrue(_app.Run.StartBattle());
            _app.ShowBattle();
            yield return null;

            var battle = (BattleScreen)_app.CurrentScreen;
            Assert.AreEqual(2, battle.SpeedFactor);
        }

        [UnityTest]
        public IEnumerator ResetProgressAsksTwiceAndKeepsSettings()
        {
            _app.Profile.Apply("stage_1_1", new StageReward { Stars = 2, VictoryGold = 30 });
            new ProfileStore(_folder).Save(_app.Profile);
            Press(_app.CurrentScreen, "Button Settings");
            SettingsPanel panel = _app.CurrentScreen.GetComponentInChildren<SettingsPanel>();
            Press(panel, "Music Toggle");

            Press(panel, "Reset Progress");
            Assert.IsTrue(Find(panel.transform, "Confirm Reset").gameObject.activeSelf, "asks first");
            Assert.AreEqual(30, _app.Profile.Gold, "nothing deleted yet");

            Press(panel, "Cancel Reset");
            Assert.IsFalse(Find(panel.transform, "Confirm Reset").gameObject.activeSelf);
            Assert.AreEqual(30, _app.Profile.Gold);

            Press(panel, "Reset Progress");
            Press(panel, "Yes Delete");
            yield return null;

            Assert.AreEqual(0, _app.Profile.Gold);
            Assert.AreEqual(0, new ProfileStore(_folder).Load().Gold, "the reset is saved");
            Assert.AreEqual(0, new ProfileStore(_folder).Load().BestStars("stage_1_1"));
            Assert.IsFalse(SavedSettings().music, "settings stay");
            Assert.IsInstanceOf<StageMapScreen>(_app.CurrentScreen);
            Assert.IsNull(_app.CurrentScreen.GetComponentInChildren<SettingsPanel>(), "panel closed");
        }
    }
}
