using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Analytics;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
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

        // ---- Rewarded ads and the Shop (PB-30, D-064) ----

        private IAds _ads;
        private IBilling _billing;

        /// <summary>Lets tests (or a real SDK) replace the ads. Without an ad SDK the game ships with <see cref="NoAds"/>.</summary>
        public void UseAds(IAds ads) => _ads = ads;

        public void UseBilling(IBilling billing) => _billing = billing;

        public IAds Ads => _ads ??= (Application.isEditor || Debug.isDebugBuild ? new TestAds() : (IAds)new NoAds());

        public IBilling Billing => _billing ??= (Application.isEditor || Debug.isDebugBuild ? new TestBilling() : (IBilling)new NoBilling());

        public MonetizationRules AdRules => content.Monetization != null ? content.Monetization.Rules() : new MonetizationRules();

        private bool CanRewardNow => Profile.AdLedger.Supporter || Ads.IsReady;

        /// <summary>Gives a reward after an ad (or at once for a Supporter). <paramref name="apply"/> must return true when granted.</summary>
        private void Reward(System.Func<bool> apply, System.Action granted)
        {
            void Grant()
            {
                if (apply())
                {
                    SaveProfile();
                    granted?.Invoke();
                }
            }
            if (Profile.AdLedger.Supporter)
            {
                Grant();
            }
            else
            {
                Ads.ShowRewarded(ok =>
                {
                    if (ok)
                    {
                        Grant();
                    }
                });
            }
        }

        private string AdCaption(string text) => Profile.AdLedger.Supporter ? text : Loc.T("Ad") + ": " + text;

        /// <summary>The extra-moves offer of a campaign attempt, or null (the daily challenge and the expedition have none).</summary>
        private AdOffer ExtraMovesOffer()
        {
            if (_dailyDay != null || _expeditionBattle)
            {
                return null;
            }
            MonetizationRules rules = AdRules;
            return new AdOffer
            {
                Available = () => Run != null && Run.CanGrantExtraMoves && CanRewardNow,
                Caption = AdCaption(Loc.F("+{0} moves", rules.ExtraMoves)),
                Request = granted => Reward(() => Run.TryGrantExtraMoves(rules.ExtraMoves), granted),
            };
        }

        private AdOffer ExtraRallyOffer()
        {
            if (_dailyDay != null || _expeditionBattle)
            {
                return null;
            }
            MonetizationRules rules = AdRules;
            return new AdOffer
            {
                Available = () => Run != null && Run.CanGrantExtraRally && CanRewardNow,
                Caption = AdCaption(Loc.F("+{0} Rally", rules.ExtraRally)),
                Request = granted => Reward(() => Run.TryGrantExtraRally(rules.ExtraRally), granted),
            };
        }

        /// <summary>The Shop tab.</summary>
        public void ShowShop()
        {
            Run = null;
            RefreshQuests();
            MonetizationRules rules = AdRules;
            var info = new ShopScreen.Info
            {
                Gold = Profile.Gold,
                Supporter = Profile.AdLedger.Supporter,
                GoldAdsLeft = Profile.AdLedger.GoldAdsLeft(Today, rules),
                GoldPerAd = rules.BonusGold,
                AdsReady = Ads.IsReady,
                BillingAvailable = Billing.IsAvailable,
                Price = Billing.SupporterPrice,
            };
            Show(ShopScreen.Create(_screenRoot, info, new ShopScreen.Actions
            {
                WatchGold = () => Reward(() => Profile.TakeGoldAd(Today, rules), ShowShop),
                BuySupporter = () => Billing.BuySupporter(ok => OnSupporterResult(ok)),
                Restore = () => Billing.RestoreSupporter(ok => OnSupporterResult(ok)),
            }, OnTab));
        }

        private void OnSupporterResult(bool owned)
        {
            if (owned)
            {
                Profile.SetSupporter(true);
                SaveProfile();
            }
            ShowShop();
        }

        // ---- Cloud save (PB-31, D-065) ----

        private ICloudSave _cloud;
        private bool _cloudDirty;
        private float _lastCloudWrite = -1000f;
        private const float CloudWriteGapSeconds = 20f;

        /// <summary>Lets tests (or the Play Games plugin) replace the cloud. Releases ship <see cref="NoCloud"/> until then.</summary>
        public void UseCloud(ICloudSave cloud) => _cloud = cloud;

        public ICloudSave Cloud => _cloud ??= (Application.isEditor || Debug.isDebugBuild ? new TestCloud() : (ICloudSave)new NoCloud());

        /// <summary>The conflict question while it is open (tests answer it).</summary>
        public CloudConflictPanel ConflictPanel { get; private set; }

        /// <summary>
        /// The save on this phone comes from a newer version of the game: play goes on with a fresh profile, but
        /// nothing is written to the phone or the cloud, so updating the game brings the progress back (D-067).
        /// </summary>
        public bool SaveLocked => _store != null && _store.Locked;

        /// <summary>The "update the game" message while it is open (tests close it).</summary>
        public NoticePanel SaveNotice { get; private set; }

        /// <summary>Writes the profile to disk now, and to the cloud soon.</summary>
        private void SaveProfile()
        {
            _store.Save(Profile);
            _cloudDirty = true;
        }

        private void Update()
        {
            if (_cloudDirty && Time.realtimeSinceStartup - _lastCloudWrite >= CloudWriteGapSeconds)
            {
                FlushCloud();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                FlushCloud();
            }
        }

        /// <summary>Uploads the profile if it changed since the last upload (no-op when signed out).</summary>
        public void FlushCloud()
        {
            if (!_cloudDirty || !Cloud.IsSignedIn || SaveLocked)
            {
                return;
            }
            _cloudDirty = false;
            _lastCloudWrite = Time.realtimeSinceStartup;
            Cloud.Write(new CloudData { Json = JsonUtility.ToJson(Profile.ToSnapshot()), SavedUtc = System.DateTime.UtcNow }, ok => _cloudDirty |= !ok);
        }

        private static PlayerProfile.Snapshot ParseSnapshot(string json)
        {
            try
            {
                return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<PlayerProfile.Snapshot>(json);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Compares this phone's save with the cloud's and acts: keep and upload, take the cloud's, or ask when each
        /// has progress the other lacks. Called after signing in and when the game starts signed in.
        /// </summary>
        public void SyncCloud()
        {
            if (!Cloud.IsSignedIn || SaveLocked)
            {
                return;
            }
            Cloud.Read(data =>
            {
                PlayerProfile.Snapshot cloud = data != null ? ParseSnapshot(data.Json) : null;
                System.DateTime localUtc = System.IO.File.Exists(_store.Path) ? System.IO.File.GetLastWriteTimeUtc(_store.Path) : System.DateTime.MinValue;
                SaveChoice choice = SaveConflict.Resolve(Profile.ToSnapshot(), localUtc, cloud, data != null ? data.SavedUtc : System.DateTime.MinValue);
                switch (choice)
                {
                    case SaveChoice.UseCloud:
                        TakeCloudSave(cloud);
                        break;
                    case SaveChoice.Ask:
                        ConflictPanel = CloudConflictPanel.Create(_screen.transform, Profile.ToSnapshot(), cloud,
                            KeepThisPhone, () => TakeCloudSave(cloud));
                        break;
                    case SaveChoice.KeepLocal:
                        _cloudDirty = true;
                        FlushCloud();
                        break;
                }
            });
        }

        private void KeepThisPhone()
        {
            ConflictPanel = null;
            _cloudDirty = true;
            FlushCloud();
        }

        private void TakeCloudSave(PlayerProfile.Snapshot cloud)
        {
            ConflictPanel = null;
            Profile = PlayerProfile.FromSnapshot(cloud);
            _quests = null;
            _expedition = null;
            _store.Save(Profile);
            _cloudDirty = false;
            ShowStageSelect();
        }

        private void ToggleCloudSignIn(SettingsPanel panel)
        {
            if (Cloud.IsSignedIn)
            {
                FlushCloud();
                Cloud.SignOut();
                panel.SetCloud(false, "");
                return;
            }
            Cloud.SignIn(ok =>
            {
                panel.SetCloud(ok, Cloud.AccountName);
                if (ok)
                {
                    SyncCloud();
                }
            });
        }

        // ---- Battle replays (PB-28, D-063) ----

        /// <summary>Keeps the battle that just ended as a replay.</summary>
        private void SaveReplay(string title)
        {
            BattleReplay replay = BattleReplay.From(Run, title);
            if (replay != null)
            {
                Profile.Replays.Add(title, replay.ToText(), replay.Victory, replay.Ticks);
                SaveProfile();
            }
        }

        private bool IsReplayPlayable(ReplayEntry entry)
        {
            BattleReplay replay = BattleReplay.Parse(entry.Text, out _);
            return replay != null && replay.IsCurrent(content.BattleRules.Rules, content.ArmyRules.Rules, content.FindUnit)
                   && replay.Rebuild(content.BattleRules.Rules, content.ArmyRules.Rules, content.FindUnit) != null;
        }

        /// <summary>The list of saved replays.</summary>
        public void ShowReplays()
        {
            Run = null;
            Show(ReplaysScreen.Create(_screenRoot, Profile.Replays, IsReplayPlayable, entry => PlayReplay(entry, 0),
                entry =>
                {
                    Profile.Replays.ToggleStar(entry);
                    SaveProfile();
                    ShowReplays();
                },
                ShowStageSelect, content.BattleRules.Rules.TicksPerSecond));
        }

        /// <summary>Plays a saved battle from <paramref name="startTick"/> (0 = from the start).</summary>
        public void PlayReplay(ReplayEntry entry, int startTick)
        {
            BattleReplay replay = BattleReplay.Parse(entry.Text, out _);
            BattleSimulator sim = replay?.Rebuild(content.BattleRules.Rules, content.ArmyRules.Rules, content.FindUnit);
            if (sim == null)
            {
                return;
            }
            while (!sim.IsFinished && sim.Tick < startTick)
            {
                replay.Advance(sim);
            }
            Show(BattleScreen.CreateReplay(_screenRoot, sim, replay, ShowReplays, tick => PlayReplay(entry, tick), Settings.BattleSpeed));
        }

        // ---- Expedition (PB-26, D-062) ----

        private Expedition _expedition;
        private bool _expeditionBattle;

        /// <summary>True when the expedition exists in the content.</summary>
        public bool HasExpedition
        {
            get
            {
                if (content.Expedition == null)
                {
                    return false;
                }
                ExpeditionRules rules = content.Expedition.Rules();
                return rules.EnemyPool.Count > 0 && rules.PlayerClasses.Count > 0;
            }
        }

        /// <summary>The run in progress or a fresh one; null if the expedition does not exist.</summary>
        public Expedition CurrentExpedition => _expedition;

        /// <summary>Opens the expedition: the saved run if there is one, otherwise a new run.</summary>
        public void ShowExpedition()
        {
            if (!HasExpedition)
            {
                return;
            }
            Run = null;
            _expeditionBattle = false;
            ExpeditionRules rules = content.Expedition.Rules();
            if (_expedition == null)
            {
                _expedition = Profile.ActiveExpedition != null ? Expedition.FromSave(Profile.ActiveExpedition, rules) : null;
            }
            if (_expedition == null)
            {
                _expedition = new Expedition(rules, (ulong)System.DateTime.UtcNow.Ticks);
                Profile.SaveExpedition(_expedition.ToSave());
                SaveProfile();
            }
            Show(ExpeditionScreen.Create(_screenRoot, _expedition, Profile, new ExpeditionScreen.Actions
            {
                Start = StartExpeditionBattle,
                Pick = PickExpeditionReward,
                GiveUp = () => EndExpedition(),
                Collect = () => EndExpedition(),
                Back = ShowStageSelect,
            }));
        }

        private void PickExpeditionReward(int index)
        {
            if (_expedition != null && _expedition.Pick(index))
            {
                Profile.SaveExpedition(_expedition.ToSave());
                SaveProfile();
                ShowExpedition();
            }
        }

        private void EndExpedition()
        {
            if (_expedition != null)
            {
                bool won = _expedition.Phase == ExpeditionPhase.Won;
                Profile.EndExpedition(_expedition.Step, won, _expedition.GoldEarned);
                SaveProfile();
                _expedition = null;
            }
            ShowStageSelect();
        }

        private void StartExpeditionBattle()
        {
            if (_expedition == null || _expedition.Phase != ExpeditionPhase.Battle)
            {
                return;
            }
            StageDef def = _expedition.StageForStep();
            _dailyDay = null;
            _stageIndex = -1;
            _stageDefinition = null;
            _expeditionBattle = true;
            if (Run != null)
            {
                Run.UnitRecruited -= OnUnitRecruited;
            }
            Run = content.CreateRun(def, new PuzzleKingdoms.Logic.SeededRandom(def.BoardSeed), null, null, _expedition.Army);
            Run.BonusRally = _expedition.BonusRally;
            Run.UnitRecruited += OnUnitRecruited;
            _stageSteps = new List<TutorialStep>();
            _puzzleStarted = Time.realtimeSinceStartup;
            _attemptLogged = false;
            _analytics.Send(GameEvents.StageStarted(def.Id, 1));
            Show(PuzzleScreen.Create(_screenRoot, Run, ShowWarCamp));
        }

        /// <summary>After an expedition battle: carry the army on and show the road again.</summary>
        private void FinishExpeditionBattle()
        {
            Run.Finish();
            SaveReplay("Expedition " + (_expedition.Step + 1));
            _expedition.Finish(Run);
            _expeditionBattle = false;
            Profile.SaveExpedition(_expedition.ToSave());
            SaveProfile();
            ShowExpedition();
        }

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
            _expeditionBattle = false;
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
                SaveProfile();
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
            foreach (UnitDefinition unit in content.ExtraUnits)
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
            _screenRoot = Ui.CreateSafeArea(Ui.CreateCanvas("Canvas"));
            ShowStageSelect();
            if (SaveLocked)
            {
                SaveNotice = NoticePanel.Create(_screen.transform, Loc.T("Update the game"),
                    Loc.T("Your save comes from a newer version of Puzzle Kingdoms. Update the game to continue it. Until then, nothing you play here is saved."),
                    () => SaveNotice = null);
            }
            SyncCloud();
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
            FlushCloud();
            Sfx.Changed -= OnSoundSwitched;
            _errors?.Dispose();
        }

        public void ShowStageSelect()
        {
            Run = null;
            RefreshQuests();
            DailyResult dailyDone = HasDaily ? Profile.Daily.Result(Today) : null;
            string dailyCaption = Loc.T("Daily Challenge") + "  ·  " + (dailyDone == null ? Loc.T("Play") : dailyDone.Won ? Loc.T("Won") + " " + dailyDone.HpPercent + "%" : Loc.T("Lost"));
            string expeditionCaption = null;
            if (HasExpedition)
            {
                ExpeditionSave active = Profile.ActiveExpedition;
                expeditionCaption = Loc.T("Expedition") + "  ·  " + (active != null
                    ? active.step + "/" + content.Expedition.Rules().Steps
                    : Loc.F("Best: {0}", Profile.BestExpeditionDepth));
            }
            Show(StageMapScreen.Create(_screenRoot, content, Profile, StartStage, () => OpenSettings(), OnTab,
                HasDaily ? ShowDaily : (System.Action)null, dailyCaption,
                HasExpedition ? ShowExpedition : (System.Action)null, expeditionCaption));
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
            SaveProfile();
            TabBar.QuestBadge = Quests.ClaimableCount(Profile.TotalStars);
        }

        private void OnCommanderChosen(string commanderId)
        {
            SaveProfile();
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
            else if (tab == MainTab.Shop)
            {
                ShowShop();
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
            SaveProfile();
            TabBar.QuestBadge = Quests.ClaimableCount(Profile.TotalStars);
        }

        private void OnBuildingUpgraded(string buildingId)
        {
            SaveProfile();
        }

        /// <summary>Opens the settings over the stage map (PB-14).</summary>
        public SettingsPanel OpenSettings()
        {
            string version = Application.version + (Debug.isDebugBuild ? " (dev)" : "");
            SettingsPanel panel = null;
            var cloud = new SettingsPanel.CloudInfo
            {
                Available = Cloud.IsAvailable,
                SignedIn = Cloud.IsSignedIn,
                Account = Cloud.AccountName,
                Toggle = () => ToggleCloudSignIn(panel),
            };
            panel = SettingsPanel.Create(_screen.transform, Settings, version, ApplySettings, ResetProgress, ResetTutorial,
                () => AskConsent(), ShowReplays, cloud);
            return panel;
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
            SaveProfile();
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
            SaveProfile();
        }

        private void OnTutorialSkipped()
        {
            _analytics.Send(GameEvents.TutorialSkipped(Tutorial != null && Tutorial.Current != null ? Tutorial.Current.Id : ""));
            Profile.SkipTutorial();
            SaveProfile();
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
            _quests = null;
            _expedition = null;
            SaveProfile();
            FlushCloud(); // an empty profile must not be brought back by the cloud copy
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
            _expeditionBattle = false;
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
            Show(PuzzleScreen.Create(_screenRoot, Run, ShowWarCamp, ExtraMovesOffer()));
            OnTrigger(TutorialTrigger.PuzzleStarted);
        }

        public void ShowWarCamp()
        {
            _campStarted = Time.realtimeSinceStartup;
            Show(WarCampScreen.Create(_screenRoot, Run, ShowBattle, ExtraRallyOffer()));
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
            if (_expeditionBattle && _expedition != null)
            {
                FinishExpeditionBattle();
                return;
            }
            StageReward reward = Run.Finish() ?? Run.Reward;
            if (reward == null)
            {
                return;
            }
            List<Commander> allCommanders = content.CommanderList();
            var lockedBefore = new HashSet<string>(allCommanders.Where(c => !Profile.IsUnlocked(c)).Select(c => c.Id));
            bool firstWin = Run.Won && _dailyDay == null && Profile.BestStars(Run.Stage.Id) == 0;
            if (_dailyDay != null)
            {
                // The daily challenge is no campaign stage: its result goes in the daily log, no stars.
                Profile.RecordDaily(DailyResult.From(_dailyDay, Run.Battle, Run.Won), reward.TotalGold);
            }
            else
            {
                Profile.Apply(Run.Stage.Id, reward);
            }
            SaveProfile();
            string unlocked = string.Join(", ", allCommanders.Where(c => lockedBefore.Contains(c.Id) && Profile.IsUnlocked(c)).Select(c => Loc.T(c.DisplayName)));
            // PB-21: a class that unlocks with this first win (Defender, Support).
            string classesUnlocked = firstWin
                ? string.Join(", ", content.ExtraUnits.Where(u => u.UnlockStageId == Run.Stage.Id).Select(u => Loc.T(u.Def.DisplayName)))
                : "";
            if (!_attemptLogged)
            {
                _attemptLogged = true;
                SaveReplay(_dailyDay != null ? "Daily Challenge" : Run.Stage.DisplayName);
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
                Profile.Gold, actions, Run.Won, goalMissed, unlocked, classesUnlocked));
            if (!Run.Won)
            {
                OnTrigger(TutorialTrigger.BattleLost);
            }
        }

        /// <summary>The music theme of a screen (PB-33): puzzle and War Camp, battle, results, and everything else is the map.</summary>
        public static MusicTheme ThemeFor(Component screen) =>
            screen is PuzzleScreen || screen is WarCampScreen ? MusicTheme.Puzzle
            : screen is BattleScreen ? MusicTheme.Battle
            : screen is ResultsScreen ? MusicTheme.Results
            : MusicTheme.Map;

        private void Show(Component screen)
        {
            Sfx.PlayTheme(ThemeFor(screen));
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
