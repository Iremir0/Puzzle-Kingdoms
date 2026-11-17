using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Stages;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>One campaign stage: puzzle settings, enemy army and rewards.</summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Stage Definition", fileName = "NewStage")]
    public sealed class StageDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class EnemySlotData
        {
            public UnitDefinition unit;
            [Range(1, 3)] public int tier = 1;
            [Tooltip("x = lane (0 left … 2 right), y = row (0 front … 2 back).")]
            public Vector2Int cell;
        }

        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField, Min(1)] private int moves = 12;
        [Tooltip("0 = random board; any other value makes the board reproducible.")]
        [SerializeField] private long boardSeed;
        [SerializeField] private List<EnemySlotData> enemies = new List<EnemySlotData>();
        [SerializeField, Min(0)] private int goldReward;
        [Tooltip("Gems that start the puzzle encased in stone: the first match only breaks the stone.")]
        [SerializeField, Min(0)] private int stoneGems;
        [Tooltip("Multiplies the HP of every enemy of this stage (1 = as defined on the unit).")]
        [SerializeField, Min(0.1f)] private float enemyHpMultiplier = 1f;
        [Tooltip("Multiplies the damage of every enemy of this stage (1 = as defined on the unit).")]
        [SerializeField, Min(0.1f)] private float enemyDamageMultiplier = 1f;

        [Header("Stage map (PB-13)")]
        [Tooltip("Where the node sits on the chapter map: x 0 = left … 1 = right, y 0 = bottom … 1 = top.")]
        [SerializeField] private Vector2 mapPosition = new Vector2(0.5f, 0.5f);

        [Serializable]
        public sealed class TutorialStepData
        {
            [Tooltip("Unique in the whole game, e.g. 1-1.puzzle. Players see each id once.")]
            public string id = "";
            public TutorialTrigger trigger;
            [TextArea(2, 4)] public string text = "";
            [Tooltip("Name of the UI object to point at (e.g. Bench, Board Area, Rally Button); empty = none.")]
            public string target = "";
        }

        [Tooltip("Prompts shown the first time each trigger happens in this stage (D-040).")]
        [SerializeField] private List<TutorialStepData> tutorial = new List<TutorialStepData>();

        public string Id => id;
        public string DisplayName => displayName;
        public IReadOnlyList<EnemySlotData> Enemies => enemies;
        public Vector2 MapPosition => mapPosition;

        public List<TutorialStep> TutorialSteps()
        {
            var steps = new List<TutorialStep>();
            foreach (TutorialStepData data in tutorial)
            {
                steps.Add(new TutorialStep { Id = data.id, Trigger = data.trigger, Text = data.text, Target = data.target ?? "" });
            }
            return steps;
        }

        /// <summary>Builds a fresh logic definition from the asset.</summary>
        public StageDef ToStageDef()
        {
            var stage = new StageDef
            {
                Id = id,
                DisplayName = displayName,
                Moves = moves,
                BoardSeed = (ulong)boardSeed,
                GoldReward = goldReward,
                StoneGems = stoneGems,
                EnemyHpMultiplier = enemyHpMultiplier,
                EnemyDamageMultiplier = enemyDamageMultiplier,
            };
            foreach (EnemySlotData slot in enemies)
            {
                stage.Enemies.Add(new EnemySlot
                {
                    Unit = slot.unit.Def,
                    Tier = slot.tier,
                    Cell = new GridPos(slot.cell.x, slot.cell.y),
                });
            }
            return stage;
        }
    }
}
