using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Analytics;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Quests;
using PuzzleKingdoms.Logic.Stages;
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

        /// <summary>
        /// Lets tests fix the language before Start runs, whatever the settings file and the computer's
        /// language say (a choice made later in Settings still wins).
        /// </summary>
        public void UseLanguage(Language language) => _languageOverride = language;

        private Language? _languageOverride;

        // ---- Daily challenge (PB-25, D-061) ----

        private string _dailyDay;

        /// <summary>Today's date key, "yyyy-MM-dd".</summary>
        public string DailyDay => Today;

        /// <summary>True when the daily challenge exists in the content.</summary>
        public bool HasDaily => content.DailyChallenge != null && content.DailyChallenge.Rules().EnemyPool.Count > 0;

        /// <summary>The challenge of today, the same for everyone who plays it today.</summary>
        public StageDef TodaysChallenge() => DailyChallenge.For(Today, content.DailyChallenge.Rules());

        public void ShowDaily()
        {
            Run = null;
            RefreshQuests();
            Show(DailyScreen.Create(_screenRoot, content, Profile, Today, TodaysChallenge(), StartDaily, ShowStageSelect));
        }

        /// <summary>Starts today's attempt: no commander and no buildings, so results compare. One attempt a day.</summary>
        public void StartDaily()
        {
            if (!HasDaily || Profile.Daily.HasPlayed(Today))
            {
                return;
            }
            StageDef def = TodaysChallenge();
            _dailyDay = Today;
            _stageIndex = -1;
            _stageDefinition = null;
            if (Run != null)
            {
                Run.UnitRecruited -= OnUnitRecruited;
            }
            Run = content.CreateRun(def);
            Run.UnitRecruited += OnUnitRecruited;
            _stageSteps = new List<TutorialStep>();
            _puzzleStarted = Time.realtimeSinceStartup;
            _attemptLogged = false;
            _analytics.Send(GameEvents.StageStarted(def.Id, 1));
            Show(PuzzleScreen.Create(_screenRoot, Run, ShowWarCamp));
        }

        // ---- Quests (PB-24, D-060) ----

        private System.Func<System.DateTime> _clock = () => System.DateTime.Now;
        private QuestBoard _quests;

        /// <summary>Lets tests set the date the daily quests are drawn for.</summary>
        public void UseClock(System.Func<System.DateTime> clock) => _clock = clock;

        /// <summary>Daily quests, achievements and star chests of the player.</summary>
        public QuestBoard Quests => _quests ??= new QuestBoard(content.Quests != null ? content.Quests.Rules : new QuestRules(), Profile.Quests);

        private string Today => _clock().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>Draws today's quests if the day changed and updates the badge on the tab bar.</summary>
        private void RefreshQuests()
        {
            if (Quests.Refresh(Today))
            {
                _store.Save(Profile);
            }
            TabBar.QuestBadge = Quests.ClaimableCount(Profile.TotalStars);
        }

        // ---- Statistics, error reports, remote config (PB-29, D-047) ----

        private AnalyticsHub _analytics;
        private ErrorReporter _errors;
        private RemoteConfig _config = RemoteConfig.Empty;
        private readonly Dictionary<string, int> _attemptsPerStage = new Dictionary<string, int>();

        /// <summary>Where game events go; they are dropped unless the player allowed statistics.</summary>
        public AnalyticsHub Analytics => _analytics;

        /// <summary>False keeps the consent question away (screenshots); the question is never answered for the player.</summary>
        public bool ConsentEnabled { get; set; } = true;

        /// <summary>The consent question on screen, or null.</summary>
        public ConsentPanel ConsentPrompt { get; private set; }

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
            Loc.Use(_languageOverride ?? Settings.LanguageFor(Loc.SystemIsTurkish));
            Sfx.EffectsOn = Settings.sound;
            Sfx.MusicOn = Settings.music;
            Sfx.Changed += OnSoundSwitched;

            // Events go nowhere without consent. Tester builds keep them, the errors and the remote-config
            // overrides in local files next to the save; a store build gets the online service later (PB-29).
            _analytics = new AnalyticsHub(() => Settings);
            if (Debug.isDebugBuild || Application.platform != RuntimePlatform.Android)
            {
                _analytics.Add(new LocalEventLog(_store.Folder));
                _errors = new ErrorReporter(_store.Folder, () => Settings, Application.version);
                _config = RemoteConfigFile.Load(_store.Folder);
            }
            _analytics.Send(GameEvents.AppStarted(Application.version, Languages.Code(Loc.Language)));

            Ui.EnsureEventSystem();
            _screenRoot = Ui.CreateCanvas("Canvas").transform;
            ShowStageSelect();
#if UNITY_EDITOR
            // Stage Editor → Play in editor (PB-19): go straight into that stage, once.
            string playPath = UnityEditor.SessionState.GetString(PlayStageKey, "");
            if (playPath.Length > 0)
            {
                UnityEditor.SessionState.EraseString(PlayStageKey);
                var stage = UnityEditor.AssetDatabase.LoadAssetAtPath<StageDefinition>(playPath);
                if (stage != null)
                {
                    StartStage(stage);
                }
            }
#endif
        }

        /// <summary>Editor only: the Stage Editor stores a stage asset path here to play it at once (PB-19).</summary>
        public const string PlayStageKey = "PuzzleKingdoms.PlayStage";

        private void OnDestroy()
        {
            Sfx.Changed -= OnSoundSwitched;
            _errors?.Dispose();
        }

        public void ShowStageSelect()
        {
            Run = null;
            RefreshQuests();
            DailyResult dailyDone = HasDaily ? Profile.Daily.Result(Today) : null;
            string dailyCaption = Loc.T("Daily Challenge") + "  ·  " + (dailyDone == null ? Loc.T("Play") : dailyDone.Won ? Loc.T("Won") + " " + dailyDone.HpPercent + "%" : Loc.T("Lost"));
            Show(StageMapScreen.Create(_screenRoot, content, Profile, StartStage, () => OpenSettings(), OnTab,
                HasDaily ? ShowDaily : (System.Action)null, dailyCaption));
            if (ConsentEnabled && Consent.NeedsAsking(Settings))
            {
                AskConsent();
            }
        }

        /// <summary>The Kingdom: buildings that cost gold and give permanent bonuses (PB-22, D-052).</summary>
        public void ShowKingdom()
        {
            Run = null;
            RefreshQuests();
            Show(KingdomScreen.Create(_screenRoot, content, Profile, OnBuildingUpgraded, OnTab));
        }

        /// <summary>The commanders: earned by stages, one leads the army and decides what Rally does (PB-23, D-053).</summary>
        public void ShowCommanders()
        {
            Run = null;
            RefreshQuests();
            Show(CommandersScreen.Create(_screenRoot, content, Profile, OnCommanderChosen, OnTab));
        }

        /// <summary>The quests: daily goals, achievements and the star chest (PB-24, D-060).</summary>
        public void ShowQuests()
        {
            Run = null;
            RefreshQuests();
            Show(QuestsScreen.Create(_screenRoot, content, Profile, Quests, OnQuestClaimed, OnTab));
        }

        private void OnQuestClaimed()
        {
            _store.Save(Profile);
            TabBar.QuestBadge = Quests.ClaimableCount(Profile.TotalStars);
        }

        private void OnCommanderChosen(string commanderId)
        {
            _store.Save(Profile);
        }

        private void OnTab(MainTab tab)
        {
            if (tab == MainTab.Kingdom)
            {
                ShowKingdom();
            }
            else if (tab == MainTab.Commanders)
            {
                ShowCommanders();
            }
            else if (tab == MainTab.Quests)
            {
                ShowQuests();
            }
            else if (tab == MainTab.Map)
            {
                ShowStageSelect();
            }
        }

        /// <summary>Counts a finished attempt towards the quests (a new day is drawn first).</summary>
        private void RecordQuests(StageAttempt attempt)
        {
            Quests.Refresh(Today);
            Quests.Record(attempt.ToQuestEvent());
            _store.Save(Profile);
            TabBar.QuestBadge = Quests.ClaimableCount(Profile.TotalStars);
        }

        private void OnBuildingUpgraded(string buildingId)
        {
            _store.Save(Profile);
        }

        /// <summary>Opens the settings over the stage map (PB-14).</summary>
        public SettingsPanel OpenSettings()
        {
            string version = Application.version + (Debug.isDebugBuild ? " (dev)" : "");
            return SettingsPanel.Create(_screen.transform, Settings, version, ApplySettings, ResetProgress, ResetTutorial,
                () => AskConsent());
        }

        /// <summary>Shows the consent question (first launch, a new consent text, or Settings → Data and privacy).</summary>
        public ConsentPanel AskConsent()
        {
            ConsentPrompt = ConsentPanel.Create(_screen.transform, Settings, !Consent.NeedsAsking(Settings), OnConsentAnswered);
            return ConsentPrompt;
        }

        private void OnConsentAnswered(bool analytics, bool crashReports, bool personalisedAds)
        {
            bool firstAnswer = Consent.NeedsAsking(Settings);
            GameSettings answered = Settings.Copy();
            Consent.Answer(answered, analytics, crashReports, personalisedAds);
            Settings = answered.Normalize();
            _settingsStore.Save(Settings);
            ConsentPrompt = null;
            if (firstAnswer)
            {
                // The session start could not be counted before the answer.
                _analytics.Send(GameEvents.AppStarted(Application.version, Languages.Code(Loc.Language)));
            }
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
            _analytics.Send(GameEvents.TutorialSkipped(Tutorial != null && Tutorial.Current != null ? Tutorial.Current.Id : ""));
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
            GameSettings before = Settings;
            string languageBefore = Settings.language;
            GameSettings next = settings.Copy();
            // Consent only changes through its own question: keep it even if the settings panel holds an older copy.
            next.consentVersion = before.consentVersion;
            next.analytics = before.analytics;
            next.crashReports = before.crashReports;
            next.personalisedAds = before.personalisedAds;
            Settings = next.Normalize();
            _settingsStore.Save(Settings);
            SendSettingChanges(before, Settings);
            Sfx.Changed -= OnSoundSwitched; // already saved; do not save twice
            Sfx.EffectsOn = Settings.sound;
            Sfx.MusicOn = Settings.music;
            Sfx.Changed += OnSoundSwitched;
            if (Settings.language != languageBefore)
            {
                // A new language: redraw the map and the open settings in it (PB-32).
                _languageOverride = null;
                Loc.Use(Settings.LanguageFor(Loc.SystemIsTurkish));
                ShowStageSelect();
                OpenSettings();
            }
        }

        /// <summary>The puzzle screen's sound button changed the sound: remember it.</summary>
        private void OnSoundSwitched()
        {
            GameSettings before = Settings.Copy();
            Settings.sound = Sfx.EffectsOn;
            Settings.music = Sfx.MusicOn;
            _settingsStore.Save(Settings);
            SendSettingChanges(before, Settings);
        }

        private void SendSettingChanges(GameSettings before, GameSettings after)
        {
            if (before.sound != after.sound)
            {
                _analytics.Send(GameEvents.SettingChanged("sound", after.sound ? "on" : "off"));
            }
            if (before.music != after.music)
            {
                _analytics.Send(GameEvents.SettingChanged("music", after.music ? "on" : "off"));
            }
            if (before.BattleSpeed != after.BattleSpeed)
            {
                _analytics.Send(GameEvents.SettingChanged("battle_speed", after.BattleSpeed + "x"));
            }
            if (before.language != after.language)
            {
                _analytics.Send(GameEvents.SettingChanged("language", after.language.Length > 0 ? after.language : "auto"));
            }
        }

        /// <summary>Deletes stars and gold (settings stay) and redraws the stage map.</summary>
        public void ResetProgress()
        {
            Profile = new PlayerProfile();
            _store.Save(Profile);
            ShowStageSelect();
        }

        /// <summary>Starts a fresh attempt at campaign stage <paramref name="index"/>.</summary>
        public void StartStage(int index) => StartStage(content.Campaign[index], index);

        /// <summary>
        /// Starts a fresh attempt at any stage asset, also one outside the campaign (the Stage Editor's
        /// "Play in editor", PB-19): such a stage has no "Next stage" and saves its stars like any other.
        /// </summary>
        public void StartStage(StageDefinition stage)
        {
            int index = -1;
            for (int i = 0; i < content.Campaign.Count; i++)
            {
                if (content.Campaign[i] == stage)
                {
                    index = i;
                }
            }
            StartStage(stage, index);
        }

        private StageDefinition _stageDefinition;

        private void StartStage(StageDefinition stage, int index)
        {
            _stageIndex = index;
            _dailyDay = null;
            _stageDefinition = stage;
            if (Run != null)
            {
                Run.UnitRecruited -= OnUnitRecruited;
            }
            Run = content.CreateRun(stage, null, _config,
                content.KingdomRules != null ? Profile.Bonuses(content.KingdomRules.Rules) : null,
                Profile.ChosenCommander(content.CommanderList())?.Variant);
            Run.UnitRecruited += OnUnitRecruited;
            _stageSteps = stage.TutorialSteps();
            _puzzleStarted = Time.realtimeSinceStartup;
            _attemptLogged = false;
            _attemptsPerStage.TryGetValue(Run.Stage.Id, out int attempts);
            _attemptsPerStage[Run.Stage.Id] = ++attempts;
            _analytics.Send(GameEvents.StageStarted(Run.Stage.Id, attempts));
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
            List<Commander> allCommanders = content.CommanderList();
            var lockedBefore = new HashSet<string>(allCommanders.Where(c => !Profile.IsUnlocked(c)).Select(c => c.Id));
            if (_dailyDay != null)
            {
                // The daily challenge is no campaign stage: its result goes in the daily log, no stars.
                Profile.RecordDaily(DailyResult.From(_dailyDay, Run.Battle, Run.Won), reward.TotalGold);
            }
            else
            {
                Profile.Apply(Run.Stage.Id, reward);
            }
            _store.Save(Profile);
            string unlocked = string.Join(", ", allCommanders.Where(c => lockedBefore.Contains(c.Id) && Profile.IsUnlocked(c)).Select(c => Loc.T(c.DisplayName)));
            if (!_attemptLogged)
            {
                _attemptLogged = true;
                StageAttempt attempt = StageAttempt.From(Run);
                if (attempt != null)
                {
                    float puzzleSeconds = _campStarted - _puzzleStarted;
                    float campSeconds = _battleStarted - _campStarted;
                    attempt.puzzleSeconds = Mathf.Round(puzzleSeconds * 10f) / 10f;
                    attempt.campSeconds = Mathf.Round(campSeconds * 10f) / 10f;
                    _playtestLog?.Write(attempt, puzzleSeconds, campSeconds);
                    _analytics.Send(GameEvents.StageFinished(attempt));
                    RecordQuests(attempt);
                }
            }

            bool hasNext = _stageIndex >= 0 && _stageIndex + 1 < content.Campaign.Count
                           && Profile.IsUnlocked(content.CampaignOrder(), _stageIndex + 1);
            int retryIndex = _stageIndex;
            StageDefinition retryStage = _stageDefinition;
            var actions = new ResultsScreen.Actions
            {
                Retry = _dailyDay != null ? (System.Action)null : () => StartStage(retryStage, retryIndex),
                Next = _dailyDay == null && Run.Won && hasNext ? () => StartStage(retryIndex + 1) : (System.Action)null,
                StageSelect = ShowStageSelect,
            };
            float seconds = (float)Run.Battle.Result.Ticks / Run.Battle.Rules.TicksPerSecond;
            // PB-19: a battle won while missing the stage goal is a defeat; say which goal.
            string goalMissed = Run.Battle.Result.Victory && !Run.Won
                ? Loc.T("Goal missed") + ": " + StageBadges.GoalText(Run.Stage, Run.Puzzle.StonesBroken,
                    Run.Puzzle.IceAtStart - Run.Puzzle.Board.IceLeft, Run.Puzzle.IceAtStart)
                : null;
            Show(ResultsScreen.Create(_screenRoot, Run.Stage.DisplayName, Run.Battle.Result, seconds, reward,
                Profile.Gold, actions, Run.Won, goalMissed, unlocked));
            if (!Run.Won)
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
