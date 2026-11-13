using System;
using PuzzleKingdoms.Logic.Army;

namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>
    /// Damage formula (docs/GDD.md §7): Physical is reduced by Armor, Magic by
    /// Ward. Results are whole numbers and at least 1.
    /// </summary>
    public static class Damage
    {
        public static int Compute(int baseDamage, DamageType damageType, Combatant target, float multiplier)
        {
            float mitigation = damageType == DamageType.Magic ? target.Ward : target.Armor;
            double raw = (double)baseDamage * multiplier * (1.0 - mitigation);
            return Math.Max(1, (int)Math.Round(raw, MidpointRounding.AwayFromZero));
        }
    }
}
