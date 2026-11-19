using System;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>What a rewarded ad can give (PB-30, D-064). The player always chooses to watch.</summary>
    public enum AdRewardKind
    {
        /// <summary>More puzzle moves, once per attempt, when the moves have run out.</summary>
        ExtraMoves = 0,
        /// <summary>One more Rally charge for the battle, once per attempt.</summary>
        ExtraRally = 1,
        /// <summary>A little gold from the Shop, a few times a day.</summary>
        BonusGold = 2,
    }

    /// <summary>The numbers of the rewarded ads; filled from a data asset.</summary>
    public sealed class MonetizationRules
    {
        public int ExtraMoves = 5;
        public int ExtraRally = 1;
        public int BonusGold = 100;
        public int GoldAdsPerDay = 3;
    }

    /// <summary>
    /// The player's side of the rewarded ads and the Supporter pack (PB-30, D-064): how many gold ads were taken
    /// today, and whether the Supporter pack is owned. A Supporter gets every ad reward without watching an ad;
    /// nothing here sells power. The day is passed in as text because Logic has no clock.
    /// </summary>
    public sealed class AdLedger
    {
        public string GoldDay = "";
        public int GoldCount;
        public bool Supporter;

        /// <summary>Gold rewards still available today.</summary>
        public int GoldAdsLeft(string today, MonetizationRules rules) =>
            GoldDay == today ? Math.Max(0, rules.GoldAdsPerDay - GoldCount) : rules.GoldAdsPerDay;

        /// <summary>Takes one gold reward of today. False when the day's limit is reached.</summary>
        public bool TryTakeGold(string today, MonetizationRules rules)
        {
            if (GoldAdsLeft(today, rules) <= 0)
            {
                return false;
            }
            if (GoldDay != today)
            {
                GoldDay = today;
                GoldCount = 0;
            }
            GoldCount++;
            return true;
        }
    }
}
