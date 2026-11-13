using System.Collections.Generic;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Logic.Stages
{
    /// <summary>One enemy placed in a stage's 3×3 enemy formation.</summary>
    public sealed class EnemySlot
    {
        public UnitDef Unit;
        public int Tier = 1;
        /// <summary>x = lane (0 left … 2 right), y = row (0 front … 2 back).</summary>
        public GridPos Cell;
    }

    /// <summary>One campaign stage: puzzle settings, enemy army and rewards.</summary>
    public sealed class StageDef
    {
        public const ulong NoSeed = 0;

        public string Id = "";
        public string DisplayName = "";
        public int Moves = 12;
        /// <summary>Fixed board seed for reproducible stages; NoSeed means random.</summary>
        public ulong BoardSeed = NoSeed;
        public readonly List<EnemySlot> Enemies = new List<EnemySlot>();
        public int GoldReward;
    }
}
