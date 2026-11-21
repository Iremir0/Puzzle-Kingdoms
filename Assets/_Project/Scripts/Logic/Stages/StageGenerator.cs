using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Logic.Stages
{
    /// <summary>
    /// What a generated stage is made from (PB-40, D-073): the enemies it must have (its lesson), a pool to fill
    /// the rest from, the army size, how many tier 2 enemies, and the puzzle side. The tuner sets the enemy HP.
    /// </summary>
    public sealed class StageBlueprint
    {
        public string Id = "";
        public string DisplayName = "";
        public int Chapter = 1;

        /// <summary>Always in the army, in this order (the stage's lesson).</summary>
        public readonly List<UnitDef> Required = new List<UnitDef>();
        /// <summary>Fills the army up to its size; picked at random, repeats allowed.</summary>
        public readonly List<UnitDef> Pool = new List<UnitDef>();
        /// <summary>Optional boss, placed in the back row, centre.</summary>
        public UnitDef Boss;

        /// <summary>Army size without the boss, inclusive range.</summary>
        public int MinEnemies = 5;
        public int MaxEnemies = 6;
        /// <summary>How many of the army (not the boss) are tier 2, inclusive range.</summary>
        public int MinTier2;
        public int MaxTier2;

        public int MinMoves = 18;
        public int MaxMoves = 22;
        public int StoneGems;
        public StageGoal Goal = new StageGoal();
        public string[] Covers;
        public int BossStonesEvery;
        public int BossStonesCount;
        public int GoldReward = 100;
        public float EnemyDamageMultiplier = 1f;
    }

    /// <summary>
    /// Makes a stage from a <see cref="StageBlueprint"/> and a seed (PB-40, D-073). Same blueprint and seed, same
    /// stage. Melee enemies stand in front, ranged enemies and healers behind them, the boss at the back. The board
    /// stays random (<see cref="StageDef.NoSeed"/>) like the campaign stages, so the bot measures many boards.
    /// </summary>
    public static class StageGenerator
    {
        public static StageDef Generate(StageBlueprint blueprint, ulong seed)
        {
            if (blueprint == null)
            {
                throw new ArgumentNullException(nameof(blueprint));
            }
            if (blueprint.Required.Count == 0 && blueprint.Pool.Count == 0)
            {
                throw new ArgumentException("A blueprint needs required enemies or a pool");
            }
            var random = new SeededRandom(seed == 0 ? 1UL : seed);
            var stage = new StageDef
            {
                Id = blueprint.Id,
                DisplayName = blueprint.DisplayName,
                Chapter = blueprint.Chapter,
                Moves = RangeOf(random, blueprint.MinMoves, blueprint.MaxMoves),
                GoldReward = blueprint.GoldReward,
                StoneGems = blueprint.StoneGems,
                EnemyDamageMultiplier = blueprint.EnemyDamageMultiplier,
                Goal = new StageGoal { Kind = blueprint.Goal.Kind, Target = blueprint.Goal.Target },
                Covers = blueprint.Covers,
                BossStonesEvery = blueprint.BossStonesEvery,
                BossStonesCount = blueprint.BossStonesCount,
            };

            int size = Math.Max(blueprint.Required.Count, RangeOf(random, blueprint.MinEnemies, blueprint.MaxEnemies));
            int capacity = Formation.Size * Formation.Size - (blueprint.Boss != null ? 1 : 0);
            size = Math.Min(size, capacity);
            var army = new List<UnitDef>(blueprint.Required);
            while (army.Count < size)
            {
                army.Add(blueprint.Pool.Count > 0
                    ? blueprint.Pool[random.Range(0, blueprint.Pool.Count)]
                    : blueprint.Required[random.Range(0, blueprint.Required.Count)]);
            }
            int tier2 = Math.Min(army.Count, RangeOf(random, blueprint.MinTier2, blueprint.MaxTier2));
            var tiers = new int[army.Count];
            for (int i = 0; i < tiers.Length; i++)
            {
                tiers[i] = 1;
            }
            for (int given = 0; given < tier2;)
            {
                int i = random.Range(0, tiers.Length);
                if (tiers[i] == 1)
                {
                    tiers[i] = 2;
                    given++;
                }
            }

            // Front to back: melee first, then the rest; inside a group a random lane order.
            var order = new List<int>();
            for (int i = 0; i < army.Count; i++)
            {
                order.Add(i);
            }
            Shuffle(order, random);
            var front = order.FindAll(i => Rank(army[i]) == 0);
            front.AddRange(order.FindAll(i => Rank(army[i]) == 1)); // a stable split, the same in Unity and .NET
            order = front;

            var bossCell = new GridPos(1 + random.Range(0, 2), Formation.Size - 1);
            var cells = new List<GridPos>();
            for (int y = 0; y < Formation.Size; y++)
            {
                var row = new List<GridPos>();
                for (int x = 0; x < Formation.Size; x++)
                {
                    var cell = new GridPos(x, y);
                    if (blueprint.Boss == null || cell != bossCell)
                    {
                        row.Add(cell);
                    }
                }
                Shuffle(row, random);
                cells.AddRange(row);
            }
            for (int i = 0; i < order.Count; i++)
            {
                stage.Enemies.Add(new EnemySlot { Unit = army[order[i]], Tier = tiers[order[i]], Cell = cells[i] });
            }
            if (blueprint.Boss != null)
            {
                stage.Enemies.Add(new EnemySlot { Unit = blueprint.Boss, Tier = 1, Cell = bossCell });
            }
            return stage;
        }

        /// <summary>0 = front (melee that does not heal), 1 = behind (ranged or healer).</summary>
        private static int Rank(UnitDef unit) => unit.AttackRange > UnitDef.RangedThreshold || unit.HealAmount > 0 ? 1 : 0;

        private static int RangeOf(IRandom random, int min, int max) => max <= min ? min : random.Range(min, max + 1);

        private static void Shuffle<T>(List<T> items, IRandom random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                T swap = items[i];
                items[i] = items[j];
                items[j] = swap;
            }
        }
    }
}
