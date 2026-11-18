using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Balance
{
    /// <summary>
    /// Reads the game's ScriptableObject .asset files (Force Text YAML) into
    /// logic objects, mirroring what the PuzzleKingdoms.Data classes do in Unity.
    /// Only understands the fields our assets use.
    /// </summary>
    public sealed class AssetReader
    {
        private static readonly Regex Scalar = new Regex(@"^  (\w+): (.*)$");
        private static readonly Regex Guid = new Regex(@"guid: (\w+)");
        private static readonly Regex Cell = new Regex(@"cell: \{x: (-?\d+), y: (-?\d+)\}");

        private readonly Dictionary<string, string> _pathByGuid = new Dictionary<string, string>();
        private readonly Dictionary<string, UnitDef> _units = new Dictionary<string, UnitDef>();

        public readonly List<UnitDef> Recruitable = new List<UnitDef>();
        public readonly List<StageDef> Campaign = new List<StageDef>();
        public PuzzleRules PuzzleRules;
        public ArmyRules ArmyRules;
        public BattleRules BattleRules;
        public ProgressionRules ProgressionRules;

        public AssetReader(string dataFolder)
        {
            foreach (string meta in Directory.GetFiles(dataFolder, "*.asset.meta", SearchOption.AllDirectories))
            {
                Match match = Guid.Match(File.ReadAllText(meta));
                _pathByGuid[match.Groups[1].Value] = meta.Substring(0, meta.Length - ".meta".Length);
            }

            string[] content = File.ReadAllLines(Path.Combine(dataFolder, "GameContent.asset"));
            Recruitable.AddRange(ListGuids(content, "recruitableUnits").Select(Unit));
            Campaign.AddRange(ListGuids(content, "campaign").Select(g => Stage(PathOf(g))));
            Dictionary<string, string> refs = Scalars(content);
            PuzzleRules = ReadPuzzleRules(PathOf(GuidIn(refs["puzzleRules"])));
            ArmyRules = ReadArmyRules(PathOf(GuidIn(refs["armyRules"])));
            BattleRules = ReadBattleRules(PathOf(GuidIn(refs["battleRules"])));
            ProgressionRules = ReadProgressionRules(PathOf(GuidIn(refs["progressionRules"])));
        }

        private string PathOf(string guid) =>
            _pathByGuid.TryGetValue(guid, out string path) ? path : throw new InvalidDataException("unknown guid " + guid);

        private static string GuidIn(string value) => Guid.Match(value).Groups[1].Value;

        private static float F(string value) => float.Parse(value, CultureInfo.InvariantCulture);

        private static int I(string value) => int.Parse(value, CultureInfo.InvariantCulture);

        private static Dictionary<string, string> Scalars(IEnumerable<string> lines)
        {
            var values = new Dictionary<string, string>();
            foreach (string line in lines)
            {
                Match match = Scalar.Match(line);
                if (match.Success)
                {
                    values[match.Groups[1].Value] = match.Groups[2].Value.Trim();
                }
            }
            return values;
        }

        /// <summary>Items of a top-level YAML list ("  key:" followed by "  - ..." lines).</summary>
        private static List<string> ListItems(string[] lines, string key)
        {
            var items = new List<string>();
            int start = Array.IndexOf(lines, "  " + key + ":");
            for (int i = start + 1; start >= 0 && i < lines.Length; i++)
            {
                if (lines[i].StartsWith("  - "))
                {
                    items.Add(lines[i].Substring(4));
                }
                else if (!lines[i].StartsWith("    "))
                {
                    break;
                }
            }
            return items;
        }

        private static IEnumerable<string> ListGuids(string[] lines, string key) =>
            ListItems(lines, key).Select(GuidIn);

        private UnitDef Unit(string guid)
        {
            if (_units.TryGetValue(guid, out UnitDef cached))
            {
                return cached;
            }
            Dictionary<string, string> v = Scalars(File.ReadAllLines(PathOf(guid)));
            var unit = new UnitDef
            {
                Id = v["id"],
                DisplayName = v["displayName"],
                MaxHp = I(v["maxHp"]),
                Damage = I(v["damage"]),
                DamageType = (DamageType)I(v["damageType"]),
                AttackInterval = F(v["attackInterval"]),
                AttackRange = F(v["attackRange"]),
                MoveSpeed = F(v["moveSpeed"]),
                Armor = F(v["armor"]),
                Ward = F(v["ward"]),
            };
            _units[guid] = unit;
            return unit;
        }

        private StageDef Stage(string path)
        {
            string[] lines = File.ReadAllLines(path);
            Dictionary<string, string> v = Scalars(lines);
            var stage = new StageDef
            {
                Id = v["id"],
                DisplayName = v["displayName"],
                Moves = I(v["moves"]),
                BoardSeed = (ulong)long.Parse(v["boardSeed"], CultureInfo.InvariantCulture),
                GoldReward = I(v["goldReward"]),
                StoneGems = v.TryGetValue("stoneGems", out string stones) ? I(stones) : 0,
                EnemyHpMultiplier = v.TryGetValue("enemyHpMultiplier", out string hp) ? F(hp) : 1f,
                EnemyDamageMultiplier = v.TryGetValue("enemyDamageMultiplier", out string dmg) ? F(dmg) : 1f,
                // PB-19: the goal and the hand-placed board (absent in older stage assets).
                Goal = new StageGoal
                {
                    Kind = v.TryGetValue("goalKind", out string kind) ? (StageGoalKind)I(kind) : StageGoalKind.DefeatArmy,
                    Target = v.TryGetValue("goalTarget", out string target) ? I(target) : 0,
                },
            };
            List<string> layout = ListItems(lines, "layout").Select(r => r.Trim().Trim('\'', '"')).ToList();
            stage.Layout = layout.Count > 0 ? layout.ToArray() : null;
            // PB-20: ice and chains, and the boss dropping stones.
            List<string> covers = ListItems(lines, "covers").Select(r => r.Trim().Trim('\'', '"')).ToList();
            stage.Covers = covers.Count > 0 ? covers.ToArray() : null;
            stage.BossStonesEvery = v.TryGetValue("bossStonesEvery", out string every) ? I(every) : 0;
            stage.BossStonesCount = v.TryGetValue("bossStonesCount", out string count) ? I(count) : 0;
            int start = Array.IndexOf(lines, "  enemies:");
            EnemySlot slot = null;
            for (int i = start + 1; i < lines.Length && (lines[i].StartsWith("  - ") || lines[i].StartsWith("    ")); i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("- unit:"))
                {
                    slot = new EnemySlot { Unit = Unit(GuidIn(line)) };
                    stage.Enemies.Add(slot);
                }
                else if (line.StartsWith("tier:"))
                {
                    slot.Tier = I(line.Substring(5).Trim());
                }
                else if (Cell.IsMatch(line))
                {
                    Match cell = Cell.Match(line);
                    slot.Cell = new GridPos(I(cell.Groups[1].Value), I(cell.Groups[2].Value));
                }
            }
            return stage;
        }

        private static PuzzleRules ReadPuzzleRules(string path)
        {
            Dictionary<string, string> v = Scalars(File.ReadAllLines(path));
            return new PuzzleRules
            {
                BoardWidth = I(v["boardWidth"]),
                BoardHeight = I(v["boardHeight"]),
                PointsPerUnit = I(v["pointsPerUnit"]),
                PointsPerRallyCharge = I(v["pointsPerRallyCharge"]),
                MaxRallyCharges = I(v["maxRallyCharges"]),
                FourMatchBonusPoints = v.TryGetValue("fourMatchBonusPoints", out string four) ? I(four) : 0,
            };
        }

        private static ArmyRules ReadArmyRules(string path)
        {
            string[] lines = File.ReadAllLines(path);
            return new ArmyRules
            {
                TierMultipliers = ListItems(lines, "tierMultipliers").Select(F).ToArray(),
                MergeCount = I(Scalars(lines)["mergeCount"]),
            };
        }

        private static BattleRules ReadBattleRules(string path)
        {
            Dictionary<string, string> v = Scalars(File.ReadAllLines(path));
            return new BattleRules
            {
                TicksPerSecond = I(v["ticksPerSecond"]),
                TimeoutSeconds = F(v["timeoutSeconds"]),
                FrontGap = F(v["frontGap"]),
                RowSpacing = F(v["rowSpacing"]),
                LaneSpacing = F(v["laneSpacing"]),
                SameLaneTieDistance = F(v["sameLaneTieDistance"]),
                SeparationDistance = F(v["separationDistance"]),
                RallyDamageBonus = F(v["rallyDamageBonus"]),
                RallyAttackSpeedBonus = F(v["rallyAttackSpeedBonus"]),
                RallyDurationSeconds = F(v["rallyDurationSeconds"]),
            };
        }

        private static ProgressionRules ReadProgressionRules(string path)
        {
            Dictionary<string, string> v = Scalars(File.ReadAllLines(path));
            return new ProgressionRules
            {
                DismissGoldPerUnit = I(v["dismissGoldPerUnit"]),
                GoldPerOverflowRallyPoint = I(v["goldPerOverflowRallyPoint"]),
            };
        }
    }
}
