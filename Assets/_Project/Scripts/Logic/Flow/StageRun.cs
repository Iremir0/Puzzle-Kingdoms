using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Logic.Flow
{
    public enum StagePhase
    {
        Puzzle,
        WarCamp,
        Battle,
        Finished,
    }

    /// <summary>
    /// One attempt at a stage, from the first swap to the reward (docs/GDD.md §4):
    /// Puzzle → War Camp → Battle → Finished. Screens drive it and read its
    /// state; it owns the rules of moving between phases.
    ///
    /// Gem types: 0..n-1 recruit <c>recruitableUnits[i]</c>, gem n is the Banner.
    /// </summary>
    public sealed class StageRun
    {
        public const string RallyKey = "rally";

        public event Action<StagePhase> PhaseChanged;
        /// <summary>Raised for every unit recruited on the puzzle board.</summary>
        public event Action<ArmyUnit> UnitRecruited;

        public readonly StageDef Stage;
        public readonly PuzzleSession Puzzle;
        public readonly Roster Roster;
        public StagePhase Phase { get; private set; } = StagePhase.Puzzle;
        public BattleSimulator Battle { get; private set; }
        public StageReward Reward { get; private set; }
        /// <summary>Bench units dismissed for gold when the battle started.</summary>
        public int DismissedUnits { get; private set; }

        private readonly Dictionary<string, UnitDef> _unitsByKey = new Dictionary<string, UnitDef>();
        private readonly BattleRules _battleRules;
        private readonly ArmyRules _armyRules;
        private readonly ProgressionRules _progressionRules;

        public StageRun(
            StageDef stage,
            IReadOnlyList<UnitDef> recruitableUnits,
            PuzzleRules puzzleRules,
            ArmyRules armyRules,
            BattleRules battleRules,
            ProgressionRules progressionRules,
            IRandom random)
        {
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            _armyRules = armyRules;
            _battleRules = battleRules;
            _progressionRules = progressionRules;

            var keys = new List<string>();
            var meters = new Dictionary<string, RecruitmentMeter>();
            foreach (UnitDef unit in recruitableUnits)
            {
                if (unit.Id == RallyKey || _unitsByKey.ContainsKey(unit.Id))
                {
                    throw new ArgumentException($"Unit id '{unit.Id}' is reserved or duplicated");
                }
                _unitsByKey[unit.Id] = unit;
                keys.Add(unit.Id);
                meters[unit.Id] = new RecruitmentMeter(puzzleRules.PointsPerUnit);
            }
            keys.Add(RallyKey);
            meters[RallyKey] = new RecruitmentMeter(puzzleRules.PointsPerRallyCharge, puzzleRules.MaxRallyCharges);

            var board = new BoardModel(puzzleRules.BoardWidth, puzzleRules.BoardHeight, keys.Count, random);
            board.Generate();
            board.PlaceStones(stage.StoneGems);
            Puzzle = new PuzzleSession(board, stage.Moves, keys, meters, puzzleRules.FourMatchBonusPoints);
            Puzzle.RewardsEarned += OnRewardsEarned;
            Puzzle.Finished += () => SetPhase(StagePhase.WarCamp);
            Roster = new Roster(armyRules);
        }

        /// <summary>Randomness for a stage: its fixed seed if it has one, otherwise the clock.</summary>
        public static IRandom CreateRandom(StageDef stage) =>
            stage.BoardSeed != StageDef.NoSeed ? new SeededRandom(stage.BoardSeed) : SeededRandom.FromTime();

        public int RallyCharges => Puzzle.RewardCount(RallyKey);

        public int RallyOverflowPoints => Puzzle.Meters[RallyKey].OverflowPoints;

        public bool CanStartBattle => Phase == StagePhase.WarCamp && Roster.Formation.Count > 0;

        /// <summary>
        /// Leaves the War Camp: bench units are dismissed for gold and the battle
        /// is set up. Returns false if not in the War Camp or nobody is deployed.
        /// </summary>
        public bool StartBattle()
        {
            if (!CanStartBattle)
            {
                return false;
            }
            DismissedUnits = Roster.Bench.Count;
            Battle = new BattleSimulator(
                _battleRules,
                _armyRules,
                Roster.Formation,
                Formation.ForStage(Stage),
                RallyCharges);
            SetPhase(StagePhase.Battle);
            return true;
        }

        /// <summary>Computes the reward once the battle is over. Returns null before that.</summary>
        public StageReward Finish()
        {
            if (Phase != StagePhase.Battle || !Battle.IsFinished)
            {
                return null;
            }
            Reward = StageReward.Compute(Stage, Battle.Result, DismissedUnits, RallyOverflowPoints, _progressionRules);
            SetPhase(StagePhase.Finished);
            return Reward;
        }

        private void OnRewardsEarned(string key, int amount)
        {
            if (!_unitsByKey.TryGetValue(key, out UnitDef def))
            {
                return; // Rally charges are read from the meter.
            }
            foreach (ArmyUnit unit in Roster.Recruit(def, amount))
            {
                UnitRecruited?.Invoke(unit);
            }
        }

        private void SetPhase(StagePhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
