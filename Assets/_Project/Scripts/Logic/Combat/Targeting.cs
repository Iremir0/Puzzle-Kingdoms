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
            if (attacker.Def.TargetsBackRow)
            {
                return PickBackRow(attacker, candidates);
            }
            Combatant taunter = PickTaunter(attacker, candidates);
            if (taunter != null)
            {
                return taunter;
            }
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

        /// <summary>
        /// Defender (PB-21): the nearest living enemy that taunts and is within its own taunt radius of the attacker.
        /// </summary>
        private static Combatant PickTaunter(Combatant attacker, IReadOnlyList<Combatant> candidates)
        {
            Combatant best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (Combatant other in candidates)
            {
                if (!IsValid(attacker, other) || other.Def.TauntRadius <= 0f)
                {
                    continue;
                }
                float distance = Vector2.Distance(attacker.Position, other.Position);
                if (distance > other.Def.TauntRadius + DistanceEpsilon)
                {
                    continue;
                }
                if (best == null || distance < bestDistance - DistanceEpsilon
                    || (Math.Abs(distance - bestDistance) <= DistanceEpsilon && other.Id < best.Id))
                {
                    best = other;
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>
        /// Wolf rider (PB-18): the living enemy that started in the back-most formation row;
        /// ties go to the nearer one, then the lower id.
        /// </summary>
        private static Combatant PickBackRow(Combatant attacker, IReadOnlyList<Combatant> candidates)
        {
            Combatant best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (Combatant other in candidates)
            {
                if (!IsValid(attacker, other))
                {
                    continue;
                }
                float distance = Vector2.Distance(attacker.Position, other.Position);
                if (best == null
                    || other.Cell.Y > best.Cell.Y
                    || (other.Cell.Y == best.Cell.Y && distance < bestDistance - DistanceEpsilon))
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
