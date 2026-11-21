using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>The game systems that open one by one as the campaign goes on (PB-37, D-070).</summary>
    public enum Feature
    {
        Kingdom = 0,
        Quests = 1,
        Commanders = 2,
        Shop = 3,
        Daily = 4,
        Expedition = 5,
    }

    /// <summary>One feature and the campaign stage whose first win opens it.</summary>
    [Serializable]
    public sealed class FeatureUnlock
    {
        public Feature Feature;
        public string StageId = "";
    }

    /// <summary>
    /// Which campaign stage opens which feature (PB-37, D-070). A feature with no entry is open from the start. The
    /// first time a player sees an open feature, the game shows its intro once; "intro." + the feature's name is kept
    /// with the tutorial prompts already shown, so the save format does not change.
    /// </summary>
    public sealed class FeatureUnlockRules
    {
        public readonly List<FeatureUnlock> Unlocks = new List<FeatureUnlock>();

        /// <summary>The defaults of D-070: one new thing per stage over the first six stages.</summary>
        public static FeatureUnlockRules Default()
        {
            var rules = new FeatureUnlockRules();
            rules.Add(Feature.Quests, "stage_1_1");
            rules.Add(Feature.Kingdom, "stage_1_2");
            rules.Add(Feature.Commanders, "stage_1_3");
            rules.Add(Feature.Daily, "stage_1_4");
            rules.Add(Feature.Shop, "stage_2_1");
            rules.Add(Feature.Expedition, "stage_2_3");
            return rules;
        }

        public FeatureUnlockRules Add(Feature feature, string stageId)
        {
            Unlocks.Add(new FeatureUnlock { Feature = feature, StageId = stageId ?? "" });
            return this;
        }

        /// <summary>The stage that opens <paramref name="feature"/>, or "" when it is open from the start.</summary>
        public string OpenedBy(Feature feature)
        {
            foreach (FeatureUnlock unlock in Unlocks)
            {
                if (unlock.Feature == feature)
                {
                    return unlock.StageId ?? "";
                }
            }
            return "";
        }

        /// <summary>True when the stage that opens the feature has been won (at least one star).</summary>
        public bool IsOpen(Feature feature, PlayerProfile profile)
        {
            string stage = OpenedBy(feature);
            return stage.Length == 0 || profile != null && profile.BestStars(stage) > 0;
        }

        /// <summary>"stage_1_2" → "1-2", the way the map names stages.</summary>
        public static string StageLabel(string stageId)
        {
            string id = stageId ?? "";
            return id.StartsWith("stage_") ? id.Substring(6).Replace('_', '-') : id;
        }

        /// <summary>The tutorial-step id that records the intro of <paramref name="feature"/> as shown.</summary>
        public static string IntroId(Feature feature) => "intro." + feature.ToString().ToLowerInvariant();

        /// <summary>Open features whose intro the player has not seen yet, in the order they open.</summary>
        public List<Feature> IntrosDue(PlayerProfile profile)
        {
            var due = new List<Feature>();
            foreach (FeatureUnlock unlock in Unlocks)
            {
                Feature feature = unlock.Feature;
                if (!due.Contains(feature) && OpenedBy(feature).Length > 0 && IsOpen(feature, profile)
                    && !profile.HasSeenStep(IntroId(feature)))
                {
                    due.Add(feature);
                }
            }
            return due;
        }
    }
}
