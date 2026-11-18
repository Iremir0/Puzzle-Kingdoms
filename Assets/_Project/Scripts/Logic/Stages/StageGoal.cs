using PuzzleKingdoms.Logic.Combat;

namespace PuzzleKingdoms.Logic.Stages
{
    /// <summary>What a stage asks for on top of winning the battle (PB-19, D-048).</summary>
    public enum StageGoalKind
    {
        /// <summary>Win the battle (every stage before PB-19).</summary>
        DefeatArmy = 0,
        /// <summary>Break at least <see cref="StageGoal.Target"/> stones on the board, then win.</summary>
        BreakStones = 1,
        /// <summary>Win the battle within <see cref="StageGoal.Target"/> seconds.</summary>
        WinWithin = 2,
        /// <summary>Melt every layer of ice on the board, then win (PB-20).</summary>
        BreakIce = 3,
    }

    /// <summary>
    /// A stage's goal. The battle must always be won; a goal adds one condition, so a stage can ask
    /// for a different way of playing without new rules. Failing it counts as a defeat (no stars, no
    /// victory gold); stars still come from the survivors.
    /// </summary>
    public sealed class StageGoal
    {
        public StageGoalKind Kind = StageGoalKind.DefeatArmy;

        /// <summary>Stones to break, or seconds to win in. Unused for <see cref="StageGoalKind.DefeatArmy"/>.</summary>
        public int Target;

        public static StageGoal DefeatArmy() => new StageGoal();

        public static StageGoal BreakStones(int stones) => new StageGoal { Kind = StageGoalKind.BreakStones, Target = stones };

        public static StageGoal WinWithin(int seconds) => new StageGoal { Kind = StageGoalKind.WinWithin, Target = seconds };

        public static StageGoal BreakIce() => new StageGoal { Kind = StageGoalKind.BreakIce };

        /// <summary>A goal that needs a target has one above zero (break the ice needs none: all of it).</summary>
        public bool IsValid => Kind == StageGoalKind.DefeatArmy || Kind == StageGoalKind.BreakIce || Target > 0;

        /// <summary>True when the stage is won: the battle won and the goal reached.</summary>
        public bool IsMet(BattleResult battle, int stonesBroken, int ticksPerSecond, int iceLeft = 0)
        {
            if (Kind == StageGoalKind.BreakIce && iceLeft > 0)
            {
                return false;
            }
            if (battle == null || !battle.Victory)
            {
                return false;
            }
            switch (Kind)
            {
                case StageGoalKind.BreakStones:
                    return stonesBroken >= Target;
                case StageGoalKind.WinWithin:
                    return battle.Ticks <= Target * ticksPerSecond;
                default:
                    return true;
            }
        }

        /// <summary>For the stone goal: true once enough stones are broken (the puzzle can show it as done).</summary>
        public bool StonesDone(int stonesBroken) => Kind != StageGoalKind.BreakStones || stonesBroken >= Target;
    }
}
