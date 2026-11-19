using System;
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

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-33: music per kind of screen, falling back to the default loop, and the cross-fade between themes.</summary>
    public class MusicTests
    {
        private GameObject _host;
        private SoundBank _bank;
        private AudioClip _default;
        private AudioClip _map;
        private AudioClip _battle;

        private static AudioClip Clip(string name) => AudioClip.Create(name, 4410, 1, 44100, false);

#if UNITY_EDITOR
        private static void Set(SoundBank bank, string field, object value)
        {
            var serialized = new UnityEditor.SerializedObject(bank);
            UnityEditor.SerializedProperty property = serialized.FindProperty(field);
            if (value is UnityEngine.Object asset)
            {
                property.objectReferenceValue = asset;
            }
            else
            {
                property.floatValue = (float)value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
#endif

        [SetUp]
        public void SetUp()
        {
            _default = Clip("default");
            _map = Clip("map");
            _battle = Clip("battle");
            _bank = ScriptableObject.CreateInstance<SoundBank>();
#if UNITY_EDITOR
            Set(_bank, "music", _default);
            Set(_bank, "mapMusic", _map);
            Set(_bank, "battleMusic", _battle);
            Set(_bank, "musicFadeSeconds", 0.3f);
#else
            Assert.Ignore("needs the editor to fill the bank");
#endif
            _host = new GameObject("Sfx host");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.Destroy(_host);
            UnityEngine.Object.Destroy(_bank);
        }

        [Test]
        public void AThemeWithoutAClipFallsBackToTheDefaultLoop()
        {
            Assert.AreSame(_map, _bank.ClipFor(MusicTheme.Map));
            Assert.AreSame(_battle, _bank.ClipFor(MusicTheme.Battle));
            Assert.AreSame(_default, _bank.ClipFor(MusicTheme.Puzzle), "no puzzle music: the default loop");
            Assert.AreSame(_default, _bank.ClipFor(MusicTheme.Results));
        }

        [UnityTest]
        public IEnumerator ThemesCrossFadeAndTheSameThemeChangesNothing()
        {
            Sfx.PlayTheme(MusicTheme.Map);
            Sfx.Setup(_host, _bank);
            yield return null;
            Assert.AreSame(_map, Sfx.CurrentMusic, "the map music starts at once");
            Assert.IsFalse(Sfx.IsFading);

            Sfx.PlayTheme(MusicTheme.Battle);
            Assert.AreSame(_battle, Sfx.CurrentMusic);
            Assert.IsTrue(Sfx.IsFading, "the old music fades out while the new one fades in");
            Assert.AreEqual(MusicTheme.Battle, Sfx.CurrentTheme);

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsFalse(Sfx.IsFading, "the fade is over");
            int playing = 0;
            foreach (AudioSource source in _host.GetComponents<AudioSource>())
            {
                playing += source.isPlaying && source.loop ? 1 : 0;
            }
            Assert.AreEqual(1, playing, "only one music stream is left");

            Sfx.PlayTheme(MusicTheme.Battle);
            Assert.IsFalse(Sfx.IsFading, "asking for the theme that plays changes nothing");

            Sfx.PlayTheme(MusicTheme.Puzzle);
            Assert.AreSame(_default, Sfx.CurrentMusic, "the puzzle has no music of its own yet");
        }

        [UnityTest]
        public IEnumerator TheMusicSwitchOffMutesBothPlayersAndTheResultsThemeDoesNotLoop()
        {
            Sfx.PlayTheme(MusicTheme.Map);
            Sfx.Setup(_host, _bank);
            yield return null;
            Sfx.MusicOn = false;
            foreach (AudioSource source in _host.GetComponents<AudioSource>())
            {
                if (source.loop)
                {
                    Assert.IsTrue(source.mute, "music off mutes the music players");
                }
            }
            Sfx.MusicOn = true;

            Sfx.PlayTheme(MusicTheme.Results);
            int looping = 0;
            foreach (AudioSource source in _host.GetComponents<AudioSource>())
            {
                looping += source.isPlaying && source.loop ? 1 : 0;
            }
            Assert.AreEqual(1, looping, "the results piece plays once; only the fading-out map music still loops");
        }
    }

    /// <summary>PB-33: the game picks the theme of the screen on show.</summary>
    public class ScreenThemeTests
    {
        private string _folder;
        private GameApp _app;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-theme-" + Guid.NewGuid().ToString("N"));
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

        [UnityTest]
        public IEnumerator EachKindOfScreenAsksForItsOwnTheme()
        {
            Assert.AreEqual(MusicTheme.Map, Sfx.CurrentTheme, "the map");

            _app.ShowKingdom();
            yield return null;
            Assert.AreEqual(MusicTheme.Map, Sfx.CurrentTheme, "the menus share the map music");

            _app.StartStage(0);
            yield return null;
            Assert.AreEqual(MusicTheme.Puzzle, Sfx.CurrentTheme);

            while (!_app.Run.Puzzle.IsFinished)
            {
                _app.Run.Puzzle.Board.TryFindValidMove(out var a, out var b);
                _app.Run.Puzzle.PlaySwap(a, b);
            }
            _app.ShowWarCamp();
            yield return null;
            Assert.AreEqual(MusicTheme.Puzzle, Sfx.CurrentTheme, "the War Camp keeps the puzzle music");

            PuzzleKingdoms.Logic.Army.AutoFormation.Fill(_app.Run.Roster);
            Assert.IsTrue(_app.Run.StartBattle());
            _app.ShowBattle();
            yield return null;
            Assert.AreEqual(MusicTheme.Battle, Sfx.CurrentTheme);

            _app.ShowStageSelect();
            yield return null;
            Assert.AreEqual(MusicTheme.Map, Sfx.CurrentTheme, "back on the map");
        }
    }
}
