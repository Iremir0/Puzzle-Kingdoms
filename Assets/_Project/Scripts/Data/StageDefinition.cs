using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic;
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

        public string Id => id;
        public string DisplayName => displayName;
        public IReadOnlyList<EnemySlotData> Enemies => enemies;

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
