using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;

namespace PuzzleKingdoms.Logic.Flow
{
    /// <summary>
    /// What a player did in one finished stage attempt, for playtests (PB-5,
    /// D-037). Built from a finished <see cref="StageRun"/>; the Flow layer adds
    /// the wall-clock fields and writes one JSON line per attempt. Public fields
    /// so a JSON serializer can write it as is.
    /// </summary>
    [Serializable]
    public sealed class StageAttempt
    {
        // Filled by the Flow layer (the logic has no clock).
        public string session = "";
        public int attempt;
        public string finishedUtc = "";
        public float puzzleSeconds;
        public float campSeconds;

        // From the run.
        public string stage = "";
        public bool victory;
        public bool timedOut;
        public int stars;
        public float battleSeconds;
        /// <summary>Units recruited on the board, all classes.</summary>
        public int recruited;
        /// <summary>Recruited units per class in gem order, e.g. "warrior 5, archer 3, mage 4".</summary>
        public string recruitedByClass = "";
        /// <summary>Merges done in the War Camp (each turns 3 units into 1).</summary>
        public int merges;
        public int deployed;
        public int survivors;
        /// <summary>Bench units dismissed for gold when the battle started.</summary>
        public int dismissed;
        public int rallyEarned;
        public int rallyUsed;
        /// <summary>
        /// The formation rows from front to back, lanes left to right, e.g.
        /// "W1 W2 W1 W1 | . M1 M1 . | . . . . | A1 . . A1": first letter of the unit
        /// id plus tier, "." for an empty cell.
        /// </summary>
        public string formation = "";
        public int gold;
        /// <summary>The highest unit tier in the formation (PB-24 quests).</summary>
        public int maxTier;

        /// <summary>The record of a finished run, or null if the battle is not over yet.</summary>
        public static StageAttempt From(StageRun run)
        {
            if (run == null || run.Reward == null || run.Battle == null || !run.Battle.IsFinished)
            {
                return null;
            }
            BattleResult result = run.Battle.Result;
            var classes = new List<string>();
            int recruited = 0;
            foreach (string key in run.Puzzle.GemRewardKeys)
            {
                if (key == StageRun.RallyKey)
                {
                    continue;
                }
                int count = run.Puzzle.RewardCount(key);
                recruited += count;
                classes.Add($"{key} {count}");
            }

            return new StageAttempt
            {
                stage = run.Stage.Id,
                victory = run.Won, // the stage, not only the battle: a missed goal is a loss (PB-19)
                timedOut = result.TimedOut,
                stars = run.Reward.Stars,
                battleSeconds = (float)result.Ticks / run.Battle.Rules.TicksPerSecond,
                recruited = recruited,
                recruitedByClass = string.Join(", ", classes),
                // Every merge removes two units; whatever was not deployed or dismissed was merged away.
                merges = Math.Max(0, (recruited - result.Deployed - run.DismissedUnits) / 2),
                deployed = result.Deployed,
                survivors = result.Survivors,
                dismissed = run.DismissedUnits,
                rallyEarned = run.RallyCharges,
                rallyUsed = run.RallyCharges - run.Battle.Rally.Charges,
                formation = Describe(run.Roster.Formation),
                gold = run.Reward.TotalGold,
                maxTier = HighestTier(run.Roster.Formation),
            };
        }

        private static int HighestTier(Formation formation)
        {
            int best = 0;
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    ArmyUnit unit = formation.GetUnit(new GridPos(x, y));
                    best = unit == null ? best : Math.Max(best, unit.Tier);
                }
            }
            return best;
        }

        /// <summary>Rows front to back, see <see cref="formation"/>.</summary>
        public static string Describe(Formation formation)
        {
            var rows = new List<string>();
            for (int y = 0; y < Formation.Size; y++)
            {
                var cells = new StringBuilder();
                for (int x = 0; x < Formation.Size; x++)
                {
                    if (x > 0)
                    {
                        cells.Append(' ');
                    }
                    ArmyUnit unit = formation.GetUnit(new GridPos(x, y));
                    cells.Append(unit == null ? "." : Tag(unit));
                }
                rows.Add(cells.ToString());
            }
            return string.Join(" | ", rows);
        }

        private static string Tag(ArmyUnit unit)
        {
            string id = unit.Def.Id;
            char letter = string.IsNullOrEmpty(id) ? '?' : char.ToUpperInvariant(id.First());
            return letter + unit.Tier.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>What this attempt is worth to the quests (PB-24).</summary>
        public Quests.QuestEvent ToQuestEvent() => new Quests.QuestEvent
        {
            Won = victory,
            RallyUsed = rallyUsed,
            Recruited = recruited,
            Merges = merges,
            Gold = gold,
            MaxTier = maxTier,
        };
    }
}
