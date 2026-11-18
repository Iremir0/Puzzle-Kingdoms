using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Kingdom
{
    /// <summary>What a building improves (PB-22, D-052).</summary>
    public enum BuildingStat
    {
        /// <summary>Max HP of one unit class.</summary>
        UnitHp = 0,
        /// <summary>Attack speed of one unit class (the interval gets shorter).</summary>
        UnitAttackSpeed = 1,
        /// <summary>Damage of one unit class.</summary>
        UnitDamage = 2,
        /// <summary>The stage's victory gold.</summary>
        StageGold = 3,
    }

    /// <summary>One building: what it improves and what each level costs.</summary>
    public sealed class BuildingRule
    {
        public string Id = "";
        public string DisplayName = "";
        public BuildingStat Stat;
        /// <summary>Id of the unit class the building improves; empty for the Treasury.</summary>
        public string UnitId = "";
        /// <summary>Bonus per level as a fraction (0.03 = +3 % per level).</summary>
        public float BonusPerLevel = 0.03f;
        /// <summary>Multiplies the shared cost curve (a dearer building has a bigger factor).</summary>
        public float CostFactor = 1f;
    }

    /// <summary>
    /// Kingdom numbers (docs/GDD.md §10): the buildings, the level cap and the shared cost curve. Built from
    /// the KingdomRules asset by the Data layer; Logic never sees the asset.
    /// </summary>
    public sealed class KingdomRules
    {
        public int MaxLevel = 10;
        /// <summary>Gold for the first level of a building with cost factor 1.</summary>
        public int BaseCost = 60;
        /// <summary>Each further level costs this many times the one before.</summary>
        public float CostGrowth = 1.4f;
        public readonly List<BuildingRule> Buildings = new List<BuildingRule>();

        public BuildingRule Find(string buildingId)
        {
            foreach (BuildingRule building in Buildings)
            {
                if (building.Id == buildingId)
                {
                    return building;
                }
            }
            return null;
        }

        /// <summary>
        /// Gold to take a building from <paramref name="currentLevel"/> to the next, rounded to 5; -1 when it is
        /// at the cap or unknown.
        /// </summary>
        public int CostToUpgrade(string buildingId, int currentLevel)
        {
            BuildingRule building = Find(buildingId);
            if (building == null || currentLevel < 0 || currentLevel >= MaxLevel)
            {
                return -1;
            }
            double raw = BaseCost * building.CostFactor * Math.Pow(CostGrowth, currentLevel);
            return Math.Max(5, (int)Math.Round(raw / 5.0, MidpointRounding.AwayFromZero) * 5);
        }

        /// <summary>Total gold for a building from level 0 to <paramref name="level"/>.</summary>
        public int TotalCostTo(string buildingId, int level)
        {
            int total = 0;
            for (int i = 0; i < level; i++)
            {
                int cost = CostToUpgrade(buildingId, i);
                total += cost < 0 ? 0 : cost;
            }
            return total;
        }

        /// <summary>The bonuses of a set of building levels (id → level).</summary>
        public KingdomBonuses BonusesFor(Func<string, int> levelOf)
        {
            var bonuses = new KingdomBonuses();
            foreach (BuildingRule building in Buildings)
            {
                int level = Math.Max(0, Math.Min(MaxLevel, levelOf(building.Id)));
                bonuses.Add(building.Stat, building.UnitId, level * building.BonusPerLevel);
            }
            return bonuses;
        }

        /// <summary>The same level in every building, for the balance tool.</summary>
        public KingdomBonuses BonusesAtLevel(int level) => BonusesFor(_ => level);
    }
}
