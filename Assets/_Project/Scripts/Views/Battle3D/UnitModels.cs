using PuzzleKingdoms.Data;
using UnityEngine;

namespace PuzzleKingdoms.Views.Battle3D
{
    /// <summary>
    /// The model set the 3D battle draws from. <c>GameApp</c> registers it at start-up (like <c>Palette</c>
    /// registers unit colours); without one every unit is a capsule.
    /// </summary>
    public static class UnitModels
    {
        private static BattleModelSet _set;

        public static void Register(BattleModelSet set) => _set = set;

        /// <summary>The Animator Controller shared by the rigged models, or null.</summary>
        public static RuntimeAnimatorController Controller => _set != null ? _set.Animator : null;

        public static bool TryGet(UnitLook look, out BattleModelSet.Entry entry)
        {
            if (_set != null)
            {
                return _set.TryGet(look, out entry) || _set.TryGet(StandIn(look), out entry);
            }
            entry = default;
            return false;
        }

        /// <summary>The model a look borrows until it has its own (PB-41 Chapter 3 enemies, PB-21 classes); itself when there is none.</summary>
        public static UnitLook StandIn(UnitLook look)
        {
            switch (look)
            {
                case UnitLook.BogTroll: return UnitLook.GoblinBrute;
                case UnitLook.GoblinBerserker: return UnitLook.GoblinGrunt;
                case UnitLook.MarshSapper: return UnitLook.GoblinBomber;
                case UnitLook.TrollKing: return UnitLook.GoblinChief;
                case UnitLook.Defender: return UnitLook.Warrior; // PB-21 classes, until their models exist
                case UnitLook.Support: return UnitLook.Mage;
                default: return look;
            }
        }
    }
}
