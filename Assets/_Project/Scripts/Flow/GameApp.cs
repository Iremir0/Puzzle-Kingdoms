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
        private Transform _screenRoot;
        private GameObject _screen;
        private int _stageIndex;

        public PlayerProfile Profile { get; private set; }
        public StageRun Run { get; private set; }

        /// <summary>The screen component currently shown (PuzzleScreen, BattleScreen, ...).</summary>
        public Component CurrentScreen { get; private set; }

        public GameContent Content => content;

        /// <summary>Lets tests point the save file somewhere harmless before Start runs.</summary>
        public void UseStore(ProfileStore store) => _store = store;

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

            _store ??= new ProfileStore();
            Profile = _store.Load();
            Ui.EnsureEventSystem();
            _screenRoot = Ui.CreateCanvas("Canvas").transform;
            ShowStageSelect();
        }

        public void ShowStageSelect()
        {
            Run = null;
            Show(StageSelectScreen.Create(_screenRoot, content, Profile, StartStage));
        }

        /// <summary>Starts a fresh attempt at campaign stage <paramref name="index"/>.</summary>
        public void StartStage(int index)
        {
            _stageIndex = index;
            Run = content.CreateRun(content.Campaign[index]);
            Show(PuzzleScreen.Create(_screenRoot, Run, ShowWarCamp));
        }

        public void ShowWarCamp() => Show(WarCampScreen.Create(_screenRoot, Run, ShowBattle));

        public void ShowBattle() => Show(BattleScreen.Create(_screenRoot, Run, ShowResults));

        public void ShowResults()
        {
            StageReward reward = Run.Finish() ?? Run.Reward;
            if (reward == null)
            {
                return;
            }
            Profile.Apply(Run.Stage.Id, reward);
            _store.Save(Profile);

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
