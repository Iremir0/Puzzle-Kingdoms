using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// What rewarded ads give (PB-30, D-064). Select <c>Data/Monetization.asset</c> and edit the numbers in the Inspector.
    /// Nothing here sells power: ads and the Supporter pack only give a little time, gold or comfort.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Monetization Rules", fileName = "Monetization")]
    public sealed class MonetizationRulesAsset : ScriptableObject
    {
        [Tooltip("Moves added when the puzzle has ended and the player watches an ad. Once per attempt.")]
        [SerializeField, Range(1, 10)] private int extraMoves = 5;
        [Tooltip("Rally charges added in the War Camp for an ad. Once per attempt.")]
        [SerializeField, Range(1, 3)] private int extraRally = 1;
        [Tooltip("Gold for one ad in the Shop.")]
        [SerializeField, Min(0)] private int bonusGold = 100;
        [Tooltip("How many gold ads a day.")]
        [SerializeField, Range(0, 10)] private int goldAdsPerDay = 3;

        public MonetizationRules Rules() => new MonetizationRules
        {
            ExtraMoves = extraMoves,
            ExtraRally = extraRally,
            BonusGold = bonusGold,
            GoldAdsPerDay = goldAdsPerDay,
        };
    }
}
