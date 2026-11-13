using System.Collections.Generic;
using System.Linq;
using PuzzleKingdoms.Logic;
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
        [SerializeField] private List<StageDefinition> campaign = new List<StageDefinition>();
        [SerializeField] private PuzzleRulesAsset puzzleRules;
        [SerializeField] private ArmyRulesAsset armyRules;
        [SerializeField] private BattleRulesAsset battleRules;
        [SerializeField] private ProgressionRulesAsset progressionRules;

        public IReadOnlyList<UnitDefinition> RecruitableUnits => recruitableUnits;
        public IReadOnlyList<StageDefinition> Campaign => campaign;
        public PuzzleRulesAsset PuzzleRules => puzzleRules;
        public ArmyRulesAsset ArmyRules => armyRules;
        public BattleRulesAsset BattleRules => battleRules;
        public ProgressionRulesAsset ProgressionRules => progressionRules;

        public List<string> CampaignOrder() => campaign.Select(s => s.Id).ToList();

        /// <summary>Starts a new attempt at a campaign stage.</summary>
        public StageRun CreateRun(StageDefinition stage, IRandom random = null)
        {
            StageDef def = stage.ToStageDef();
            List<UnitDef> units = recruitableUnits.Select(u => u.Def).ToList();
            return new StageRun(
                def,
                units,
                puzzleRules.Rules,
                armyRules.Rules,
                battleRules.Rules,
                progressionRules.Rules,
                random ?? StageRun.CreateRandom(def));
        }
    }
}
