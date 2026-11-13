using System;
using System.Collections.Generic;
using System.Numerics;

namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>
    /// Target choice (docs/GDD.md §9): the nearest living enemy. Enemies at most
    /// <c>sameLaneTie</c> farther than the nearest count as tied; among tied
    /// enemies the attacker's own lane wins, then the shorter distance, then
    /// the lower id.
    /// </summary>
    public static class Targeting
    {
        private const float DistanceEpsilon = 0.00001f;

        public static Combatant PickTarget(Combatant attacker, IReadOnlyList<Combatant> candidates, float sameLaneTie)
        {
            float nearest = float.PositiveInfinity;
            foreach (Combatant other in candidates)
            {
                if (IsValid(attacker, other))
                {
                    nearest = Math.Min(nearest, Vector2.Distance(attacker.Position, other.Position));
                }
            }
            if (float.IsPositiveInfinity(nearest))
            {
                return null;
            }

            Combatant best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (Combatant other in candidates)
            {
                if (!IsValid(attacker, other))
                {
                    continue;
                }
                float distance = Vector2.Distance(attacker.Position, other.Position);
                if (distance > nearest + sameLaneTie)
                {
                    continue;
                }
                if (best == null || IsBetter(attacker, other, distance, best, bestDistance))
                {
                    best = other;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static bool IsValid(Combatant attacker, Combatant other) =>
            other.IsAlive && attacker.IsEnemyOf(other);

        private static bool IsBetter(Combatant attacker, Combatant a, float aDistance, Combatant b, float bDistance)
        {
            bool aSameLane = a.Lane == attacker.Lane;
            bool bSameLane = b.Lane == attacker.Lane;
            if (aSameLane != bSameLane)
            {
                return aSameLane;
            }
            if (Math.Abs(aDistance - bDistance) > DistanceEpsilon)
            {
                return aDistance < bDistance;
            }
            return a.Id < b.Id;
        }
    }
}
