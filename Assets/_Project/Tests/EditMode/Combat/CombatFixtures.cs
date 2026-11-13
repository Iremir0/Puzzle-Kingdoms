using System.Collections.Generic;
using System.Numerics;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;

namespace PuzzleKingdoms.Tests.Combat
{
    internal static class CombatFixtures
    {
        public static BattleRules Rules() => new BattleRules();

        /// <summary>Rules where both front rows start 2 cells apart, so ranged units shoot at once.</summary>
        public static BattleRules CloseRules() => new BattleRules { FrontGap = 2f };

        public static Formation Formation(Dictionary<GridPos, UnitDef> placements)
        {
            var result = new Formation();
            foreach (KeyValuePair<GridPos, UnitDef> pair in placements)
            {
                result.Place(new ArmyUnit(pair.Value), pair.Key);
            }
            return result;
        }

        public static Formation Single(UnitDef def, int x = 1, int y = 0) =>
            Formation(new Dictionary<GridPos, UnitDef> { { new GridPos(x, y), def } });

        public static Combatant Combatant(int id, Side side, UnitDef def, Vector2 position, int lane = 1)
        {
            var result = new Combatant(id, side, new ArmyUnit(def), new GridPos(lane, 0), new ArmyRules());
            result.Position = position;
            return result;
        }

        public static UnitDef Archer(float interval = 0.5f) =>
            Army.ArmyFixtures.UnitDef("archer", 100, 10, interval, 4f, 1f);

        /// <summary>Never moves and never reaches anything: a target dummy.</summary>
        public static UnitDef Dummy(int hp = 100000) =>
            Army.ArmyFixtures.UnitDef("dummy", hp, 1, 1f, 0.1f, 0f);
    }
}
