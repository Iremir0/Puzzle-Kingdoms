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
        /// <summary>How many gems start the puzzle encased in stone (D-032); 0 = none.</summary>
        public int StoneGems;
        /// <summary>Multiplies the HP of every enemy of the stage (D-033); 1 = as defined.</summary>
        public float EnemyHpMultiplier = 1f;
        /// <summary>Multiplies the damage of every enemy of the stage (D-033); 1 = as defined.</summary>
        public float EnemyDamageMultiplier = 1f;

        /// <summary>What the stage asks for besides winning (PB-19, D-048); win the battle by default.</summary>
        public StageGoal Goal = new StageGoal();

        /// <summary>A hand-placed start board (<see cref="Puzzle.BoardLayout"/>); null or empty = random.</summary>
        public string[] Layout;

        /// <summary>Ice and chains on the start board (<see cref="Puzzle.BoardLayout.TryParseCovers"/>, PB-20); null = none.</summary>
        public string[] Covers;

        /// <summary>The boss drops <see cref="BossStonesCount"/> stones every this many moves (PB-20); 0 = never.</summary>
        public int BossStonesEvery;

        public int BossStonesCount;
    }
}
