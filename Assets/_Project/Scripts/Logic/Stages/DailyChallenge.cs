using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;

namespace PuzzleKingdoms.Logic.Stages
{
    /// <summary>The numbers the daily challenge is built from (PB-25, D-061); filled from a data asset.</summary>
    public sealed class DailyChallengeRules
    {
        /// <summary>The enemies a day may pick from.</summary>
        public readonly List<UnitDef> EnemyPool = new List<UnitDef>();
        public int MinEnemies = 4;
        public int MaxEnemies = 7;
        public int MaxTier = 2;
        public int MinMoves = 10;
        public int MaxMoves = 14;
        public int MaxStones = 4;
        public int GoldReward = 120;
        public float EnemyHpMultiplier = 1.2f;
        public float EnemyDamageMultiplier = 1.1f;
    }

    /// <summary>
    /// The challenge of a day: the same date gives the same enemy army, formation, moves, stones, goal and
    /// board seed on every device (PB-25, D-061). The battle is deterministic, so results are comparable.
    /// </summary>
    public static class DailyChallenge
    {
        public const string IdPrefix = "daily-";

        public static bool IsDailyId(string stageId) => stageId != null && stageId.StartsWith(IdPrefix, StringComparison.Ordinal);

        /// <summary>The stage of a day, given as "yyyy-MM-dd". Throws if the pool is empty.</summary>
        public static StageDef For(string day, DailyChallengeRules rules)
        {
            if (rules == null || rules.EnemyPool.Count == 0)
            {
                throw new ArgumentException("The daily challenge needs a pool of enemies");
            }
            var random = new SeededRandom(Hash("daily:" + day));
            var stage = new StageDef
            {
                Id = IdPrefix + day,
                DisplayName = "Daily Challenge",
                Chapter = 1,
                Moves = random.Range(rules.MinMoves, rules.MaxMoves + 1),
                BoardSeed = BoardSeedFor(day),
                GoldReward = rules.GoldReward,
                EnemyHpMultiplier = rules.EnemyHpMultiplier,
                EnemyDamageMultiplier = rules.EnemyDamageMultiplier,
                StoneGems = random.Range(0, rules.MaxStones + 1),
            };

            // The formation cells in a day-specific order; the first few hold enemies.
            var cells = new List<GridPos>();
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    cells.Add(new GridPos(x, y));
                }
            }
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                GridPos swap = cells[i];
                cells[i] = cells[j];
                cells[j] = swap;
            }
            int count = Math.Min(random.Range(rules.MinEnemies, rules.MaxEnemies + 1), cells.Count);
            for (int i = 0; i < count; i++)
            {
                stage.Enemies.Add(new EnemySlot
                {
                    Unit = rules.EnemyPool[random.Range(0, rules.EnemyPool.Count)],
                    Tier = random.Range(1, Math.Max(1, rules.MaxTier) + 1),
                    Cell = cells[i],
                });
            }

            // With a few stones, some days ask to break most of them.
            if (stage.StoneGems >= 2 && random.Range(0, 3) == 0)
            {
                stage.Goal = StageGoal.BreakStones(stage.StoneGems - 1);
            }
            return stage;
        }

        /// <summary>The board seed of a day (never 0, which means a random board).</summary>
        public static ulong BoardSeedFor(string day)
        {
            ulong seed = Hash("board:" + day);
            return seed == StageDef.NoSeed ? 1UL : seed;
        }

        private static ulong Hash(string text)
        {
            ulong hash = 1469598103934665603UL; // FNV-1a
            foreach (char c in text ?? "")
            {
                hash = (hash ^ c) * 1099511628211UL;
            }
            return hash;
        }
    }

    /// <summary>
    /// How one daily battle went. Better means: won over lost, then more of the army's HP left, then a faster
    /// win (the order of PB-25).
    /// </summary>
    public sealed class DailyResult
    {
        public string Day = "";
        public bool Won;
        /// <summary>Remaining HP of the army as a share of its starting HP, 0..100.</summary>
        public int HpPercent;
        public int Ticks;

        public bool IsBetterThan(DailyResult other)
        {
            if (other == null)
            {
                return true;
            }
            if (Won != other.Won)
            {
                return Won;
            }
            if (HpPercent != other.HpPercent)
            {
                return HpPercent > other.HpPercent;
            }
            return Ticks < other.Ticks;
        }

        /// <summary>The result of a finished battle; <paramref name="won"/> is the stage's win (goal met), not only the battle's.</summary>
        public static DailyResult From(string day, BattleSimulator battle, bool won)
        {
            int hp = 0;
            int max = 0;
            foreach (Combatant c in battle.Combatants)
            {
                if (c.Side != Side.Player)
                {
                    continue;
                }
                max += c.MaxHp;
                hp += Math.Max(0, c.Hp);
            }
            return new DailyResult
            {
                Day = day,
                Won = won,
                HpPercent = max <= 0 ? 0 : Math.Min(100, hp * 100 / max),
                Ticks = battle.Result != null ? battle.Result.Ticks : battle.Tick,
            };
        }
    }
}
