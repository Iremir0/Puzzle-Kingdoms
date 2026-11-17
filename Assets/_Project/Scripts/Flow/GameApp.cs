using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Battle;
using PuzzleKingdoms.Views.Common;
using PuzzleKingdoms.Views.Menus;
using PuzzleKingdoms.Views.Puzzle;
using PuzzleKingdoms.Views.WarCamp;
using UnityEngine;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// Grey-box entry point (D-025): one scene, one canvas, screens swapped as
    /// panels. Owns the profile and the current <see cref="StageRun"/>, and moves
    /// between Stage map → Puzzle → War Camp → Battle → Results.
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        [SerializeField] private GameContent content;

        private ProfileStore _store;
        private SettingsStore _settingsStore;
        private PlaytestLog _playtestLog;
        private Transform _screenRoot;
        private GameObject _screen;
        private int _stageIndex;

        // Wall-clock marks of the current attempt for the playtest log.
        private float _puzzleStarted;
        private float _campStarted;
        private float _battleStarted;
        private bool _attemptLogged;

        public PlayerProfile Profile { get; private set; }
        public GameSettings Settings { get; private set; }
        public StageRun Run { get; private set; }

        /// <summary>The screen component currently shown (PuzzleScreen, BattleScreen, ...).</summary>
        public Component CurrentScreen { get; private set; }

        public GameContent Content => content;

        /// <summary>Lets tests point the save file somewhere harmless before Start runs.</summary>
        public void UseStore(ProfileStore store) => _store = store;

        /// <summary>Lets tests point the playtest log somewhere harmless before Start runs.</summary>
        public void UsePlaytestLog(PlaytestLog log) => _playtestLog = log;

        public void SetContent(GameContent gameContent) => content = gameContent;

        private void Start()
        {
            Application.targetFrameRate = 60;
            if (content == null)
            {
                Debug.LogError("[GameApp] GameContent is not assigned.");
                enabled = false;
                return;
            }
            foreach (UnitDefinition unit in content.RecruitableUnits)
            {
                Palette.Register(unit);
            }
            foreach (StageDefinition stage in content.Campaign)
            {
                foreach (StageDefinition.EnemySlotData slot in stage.Enemies)
                {
                    Palette.Register(slot.unit);
                }
            }

            Sfx.Setup(gameObject, content.Sounds);
            _store ??= new ProfileStore();
            _settingsStore = new SettingsStore(_store.Folder); // settings live next to the profile (D-039)
            _playtestLog ??= new PlaytestLog();
            Profile = _store.Load();
            Settings = _settingsStore.Load();
            Sfx.EffectsOn = Settings.sound;
            Sfx.MusicOn = Settings.music;
            Sfx.Changed += OnSoundSwitched;
            Ui.EnsureEventSystem();
            _screenRoot = Ui.CreateCanvas("Canvas").transform;
            ShowStageSelect();
        }

        private void OnDestroy() => Sfx.Changed -= OnSoundSwitched;

        public void ShowStageSelect()
        {
            Run = null;
            Show(StageSelectScreen.Create(_screenRoot, content, Profile, StartStage, () => OpenSettings()));
        }

        /// <summary>Opens the settings over the stage map (PB-14).</summary>
        public SettingsPanel OpenSettings()
        {
            string version = Application.version + (Debug.isDebugBuild ? " (dev)" : "");
            return SettingsPanel.Create(_screen.transform, Settings, version, ApplySettings, ResetProgress);
        }

        private void ApplySettings(GameSettings settings)
        {
            Settings = settings.Copy().Normalize();
            _settingsStore.Save(Settings);
            Sfx.Changed -= OnSoundSwitched; // already saved; do not save twice
            Sfx.EffectsOn = Settings.sound;
            Sfx.MusicOn = Settings.music;
            Sfx.Changed += OnSoundSwitched;
        }

        /// <summary>The puzzle screen's sound button changed the sound: remember it.</summary>
        private void OnSoundSwitched()
        {
            Settings.sound = Sfx.EffectsOn;
            Settings.music = Sfx.MusicOn;
            _settingsStore.Save(Settings);
        }

        /// <summary>Deletes stars and gold (settings stay) and redraws the stage map.</summary>
        public void ResetProgress()
        {
            Profile = new PlayerProfile();
            _store.Save(Profile);
            ShowStageSelect();
        }

        /// <summary>Starts a fresh attempt at campaign stage <paramref name="index"/>.</summary>
        public void StartStage(int index)
        {
            _stageIndex = index;
            Run = content.CreateRun(content.Campaign[index]);
            _puzzleStarted = Time.realtimeSinceStartup;
            _attemptLogged = false;
            Show(PuzzleScreen.Create(_screenRoot, Run, ShowWarCamp));
        }

        public void ShowWarCamp()
        {
            _campStarted = Time.realtimeSinceStartup;
            Show(WarCampScreen.Create(_screenRoot, Run, ShowBattle));
        }

        public void ShowBattle()
        {
            _battleStarted = Time.realtimeSinceStartup;
            Show(BattleScreen.Create(_screenRoot, Run, ShowResults, Settings.BattleSpeed));
        }

        public void ShowResults()
        {
            StageReward reward = Run.Finish() ?? Run.Reward;
            if (reward == null)
            {
                return;
            }
            Profile.Apply(Run.Stage.Id, reward);
            _store.Save(Profile);
            if (!_attemptLogged)
            {
                _attemptLogged = true;
                _playtestLog?.Write(StageAttempt.From(Run),
                    _campStarted - _puzzleStarted, _battleStarted - _campStarted);
            }

            bool hasNext = _stageIndex + 1 < content.Campaign.Count
                           && Profile.IsUnlocked(content.CampaignOrder(), _stageIndex + 1);
            int retryIndex = _stageIndex;
            var actions = new ResultsScreen.Actions
            {
                Retry = () => StartStage(retryIndex),
                Next = Run.Battle.Result.Victory && hasNext ? () => StartStage(retryIndex + 1) : (System.Action)null,
                StageSelect = ShowStageSelect,
            };
            float seconds = (float)Run.Battle.Result.Ticks / Run.Battle.Rules.TicksPerSecond;
            Show(ResultsScreen.Create(_screenRoot, Run.Stage.DisplayName, Run.Battle.Result, seconds, reward,
                Profile.Gold, actions));
        }

        private void Show(Component screen)
        {
            if (_screen != null)
            {
                Destroy(_screen);
            }
            _screen = screen.gameObject;
            CurrentScreen = screen;
        }
    }
}
