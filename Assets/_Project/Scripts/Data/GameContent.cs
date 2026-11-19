using System.Collections.Generic;
using System.Linq;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Analytics;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Stages;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// Root of all game data: recruitable units (in gem order), the campaign
    /// stage order and every rules asset. Screens get everything from here.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Game Content", fileName = "GameContent")]
    public sealed class GameContent : ScriptableObject
    {
        [Tooltip("Gem i recruits unit i (Sword, Bow, Staff). The Banner gem comes after them.")]
        [SerializeField] private List<UnitDefinition> recruitableUnits = new List<UnitDefinition>();
        [Tooltip("The classes beyond the default three (PB-21: Defender, Support). Stages pick their class set from these and the recruitable units.")]
        [SerializeField] private List<UnitDefinition> extraUnits = new List<UnitDefinition>();
        [SerializeField] private List<StageDefinition> campaign = new List<StageDefinition>();
        [SerializeField] private PuzzleRulesAsset puzzleRules;
        [SerializeField] private ArmyRulesAsset armyRules;
        [SerializeField] private BattleRulesAsset battleRules;
        [SerializeField] private ProgressionRulesAsset progressionRules;
        [SerializeField] private KingdomRulesAsset kingdomRules;
        [Tooltip("Daily quests, achievements and the star chest (PB-24).")]
        [SerializeField] private QuestRulesAsset quests;
        [Tooltip("The expedition mode: a run of battles with a carried army (PB-26).")]
        [SerializeField] private ExpeditionRulesAsset expedition;
        [Tooltip("What the daily challenge is built from (PB-25).")]
        [SerializeField] private DailyChallengeAsset dailyChallenge;
        [Tooltip("The commanders the player can earn (PB-23); the classic Rally is what a player without one has.")]
        [SerializeField] private List<CommanderDefinition> commanders = new List<CommanderDefinition>();
        [SerializeField] private SoundBank sounds;
        [Tooltip("3D models of the battle view (PB-11); may be empty, units are then drawn as capsules.")]
        [SerializeField] private BattleModelSet battleModels;

        /// <summary>3D models for the battle view; may be null.</summary>
        public BattleModelSet BattleModels => battleModels;

        public IReadOnlyList<UnitDefinition> RecruitableUnits => recruitableUnits;
        public IReadOnlyList<UnitDefinition> ExtraUnits => extraUnits;
        public IReadOnlyList<StageDefinition> Campaign => campaign;
        public PuzzleRulesAsset PuzzleRules => puzzleRules;
        public ArmyRulesAsset ArmyRules => armyRules;
        public BattleRulesAsset BattleRules => battleRules;
        public ProgressionRulesAsset ProgressionRules => progressionRules;
        /// <summary>Buildings and their costs (PB-22).</summary>
        public KingdomRulesAsset KingdomRules => kingdomRules;
        /// <summary>The quest pool (PB-24); may be null (no Quests tab content).</summary>
        public QuestRulesAsset Quests => quests;
        /// <summary>The daily challenge setup (PB-25); may be null (no daily challenge).</summary>
        public DailyChallengeAsset DailyChallenge => dailyChallenge;
        /// <summary>The expedition setup (PB-26); may be null (no expedition mode).</summary>
        public ExpeditionRulesAsset Expedition => expedition;
        public IReadOnlyList<CommanderDefinition> Commanders => commanders;

        /// <summary>The logic commanders, in asset order.</summary>
        public List<Logic.Progression.Commander> CommanderList() => commanders.Select(c => c.Commander).ToList();
        /// <summary>Sound effects and music; may be null (the game then stays silent).</summary>
        public SoundBank Sounds => sounds;

        public List<string> CampaignOrder() => campaign.Select(s => s.Id).ToList();

        /// <summary>
        /// Starts a new attempt at a campaign stage; <paramref name="config"/> may override the stage's
        /// moves and enemy strength (PB-29, <see cref="StageTuning"/>).
        /// </summary>
        public StageRun CreateRun(StageDefinition stage, IRandom random = null, RemoteConfig config = null,
            Logic.Kingdom.KingdomBonuses bonuses = null,
            Logic.Combat.RallyVariant rallyVariant = null)
        {
            StageDef def = stage.ToStageDef();
            StageTuning.Apply(def, config);
            // PB-21: a stage may choose its own class set (at most 4, in gem order); otherwise the default three.
            List<UnitDef> units = (stage.Units.Count > 0 ? stage.Units : recruitableUnits).Select(u => u.Def).ToList();
            return CreateRun(def, units, random, bonuses, rallyVariant);
        }

        /// <summary>Starts an attempt at a stage built in code, such as the day's challenge (PB-25): the default class set.</summary>
        public StageRun CreateRun(StageDef def, IRandom random = null, Logic.Kingdom.KingdomBonuses bonuses = null,
            Logic.Combat.RallyVariant rallyVariant = null, IReadOnlyList<ArmyUnit> carried = null) =>
            CreateRun(def, recruitableUnits.Select(u => u.Def).ToList(), random, bonuses, rallyVariant, carried);

        private StageRun CreateRun(StageDef def, List<UnitDef> units, IRandom random, Logic.Kingdom.KingdomBonuses bonuses,
            Logic.Combat.RallyVariant rallyVariant, IReadOnlyList<ArmyUnit> carried = null)
        {
            return new StageRun(
                def,
                units,
                puzzleRules.Rules,
                armyRules.Rules,
                battleRules.Rules,
                progressionRules.Rules,
                random ?? StageRun.CreateRandom(def),
                bonuses,
                rallyVariant,
                carried);
        }
    }
}
