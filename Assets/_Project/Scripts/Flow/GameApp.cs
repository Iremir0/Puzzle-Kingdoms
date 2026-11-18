using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Battle;
using PuzzleKingdoms.Views.Battle3D;
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
            UnitModels.Register(content.BattleModels);
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
            // The playtest log is for testers (Windows playtest build, editor, development builds); a store build
            // (Android, not a development build) writes none (D-037, PB-17).
            if (_playtestLog == null && (Debug.isDebugBuild || Application.platform != RuntimePlatform.Android))
            {
                _playtestLog = new PlaytestLog();
            }
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
            Show(StageMapScreen.Create(_screenRoot, content, Profile, StartStage, () => OpenSettings()));
        }

        /// <summary>Opens the settings over the stage map (PB-14).</summary>
        public SettingsPanel OpenSettings()
        {
            string version = Application.version + (Debug.isDebugBuild ? " (dev)" : "");
            return SettingsPanel.Create(_screen.transform, Settings, version, ApplySettings, ResetProgress, ResetTutorial);
        }

        /// <summary>Shows every tutorial prompt again; stars and gold stay (PB-13).</summary>
        public void ResetTutorial()
        {
            Profile.ResetTutorial();
            _store.Save(Profile);
        }

        // ---- Tutorial (PB-13, D-040) ----

        /// <summary>False turns the tutorial prompts off (the play-through tests do).</summary>
        public bool TutorialEnabled { get; set; } = true;

        /// <summary>The prompt on screen, or null.</summary>
        public TutorialOverlay Tutorial { get; private set; }

        private List<TutorialStep> _stageSteps = new List<TutorialStep>();

        private void OnTrigger(TutorialTrigger trigger)
        {
            if (!TutorialEnabled || Run == null)
            {
                return;
            }
            List<TutorialStep> steps = TutorialGuide.StepsFor(_stageSteps, trigger, Profile);
            if (steps.Count == 0)
            {
                return;
            }
            if (Tutorial != null)
            {
                Tutorial.Enqueue(steps);
                return;
            }
            if (CurrentScreen is BattleScreen battle)
            {
                battle.Paused = true;
            }
            Tutorial = TutorialOverlay.Show(_screenRoot, steps, FindOnScreen, OnStepShown, OnTutorialSkipped,
                OnTutorialClosed);
        }

        private RectTransform FindOnScreen(string name)
        {
            if (_screen == null)
            {
                return null;
            }
            foreach (RectTransform rect in _screen.GetComponentsInChildren<RectTransform>())
            {
                if (rect.name == name)
                {
                    return rect;
                }
            }
            return null;
        }

        private void OnStepShown(TutorialStep step)
        {
            Profile.MarkStepSeen(step.Id);
            _store.Save(Profile);
        }

        private void OnTutorialSkipped()
        {
            Profile.SkipTutorial();
            _store.Save(Profile);
        }

        private void OnTutorialClosed()
        {
            Tutorial = null;
            if (CurrentScreen is BattleScreen battle)
            {
                battle.Paused = false;
            }
        }

        private void OnUnitRecruited(ArmyUnit unit) => OnTrigger(TutorialTrigger.UnitRecruited);

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
            if (Run != null)
            {
                Run.UnitRecruited -= OnUnitRecruited;
            }
            Run = content.CreateRun(content.Campaign[index]);
            Run.UnitRecruited += OnUnitRecruited;
            _stageSteps = content.Campaign[index].TutorialSteps();
            _puzzleStarted = Time.realtimeSinceStartup;
            _attemptLogged = false;
            Show(PuzzleScreen.Create(_screenRoot, Run, ShowWarCamp));
            OnTrigger(TutorialTrigger.PuzzleStarted);
        }

        public void ShowWarCamp()
        {
            _campStarted = Time.realtimeSinceStartup;
            Show(WarCampScreen.Create(_screenRoot, Run, ShowBattle));
            OnTrigger(TutorialTrigger.WarCampOpened);
        }

        public void ShowBattle()
        {
            _battleStarted = Time.realtimeSinceStartup;
            Show(BattleScreen.Create(_screenRoot, Run, ShowResults, Settings.BattleSpeed));
            OnTrigger(TutorialTrigger.BattleStarted);
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
            if (!Run.Battle.Result.Victory)
            {
                OnTrigger(TutorialTrigger.BattleLost);
            }
        }

        private void Show(Component screen)
        {
            if (Tutorial != null)
            {
                // A new screen: prompts for the old one no longer make sense (unseen ones come back later).
                Destroy(Tutorial.gameObject);
                Tutorial = null;
            }
            if (_screen != null)
            {
                Destroy(_screen);
            }
            _screen = screen.gameObject;
            CurrentScreen = screen;
        }
    }
}
