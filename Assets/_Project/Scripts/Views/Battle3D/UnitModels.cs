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
                return _set.TryGet(look, out entry);
            }
            entry = default;
            return false;
        }
    }
}
