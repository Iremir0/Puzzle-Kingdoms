using NUnit.Framework;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-37 (D-070): systems open one by one, each with one intro.</summary>
    public class FeatureUnlockTests
    {
        private static void Win(PlayerProfile profile, string stageId) =>
            profile.Apply(stageId, new StageReward { Stars = 1, VictoryGold = 10 });

        [Test]
        public void ANewPlayerHasEverySystemLocked()
        {
            var rules = FeatureUnlockRules.Default();
            var profile = new PlayerProfile();
            foreach (Feature feature in System.Enum.GetValues(typeof(Feature)))
            {
                Assert.IsFalse(rules.IsOpen(feature, profile), feature.ToString());
            }
            CollectionAssert.IsEmpty(rules.IntrosDue(profile));
        }

        [Test]
        public void EachOfTheFirstStagesOpensOneSystem()
        {
            var rules = FeatureUnlockRules.Default();
            var profile = new PlayerProfile();
            Win(profile, "stage_1_1");
            CollectionAssert.AreEqual(new[] { Feature.Quests }, rules.IntrosDue(profile));
            Win(profile, "stage_1_2");
            Win(profile, "stage_1_3");
            CollectionAssert.AreEqual(new[] { Feature.Quests, Feature.Kingdom, Feature.Commanders }, rules.IntrosDue(profile));
            Assert.IsFalse(rules.IsOpen(Feature.Expedition, profile));
            Win(profile, "stage_2_3");
            Assert.IsTrue(rules.IsOpen(Feature.Expedition, profile));
        }

        [Test]
        public void AnIntroIsShownOnce()
        {
            var rules = FeatureUnlockRules.Default();
            var profile = new PlayerProfile();
            Win(profile, "stage_1_2");
            profile.MarkStepSeen(FeatureUnlockRules.IntroId(Feature.Kingdom));
            CollectionAssert.DoesNotContain(rules.IntrosDue(profile), Feature.Kingdom);
            Assert.AreEqual("intro.kingdom", FeatureUnlockRules.IntroId(Feature.Kingdom));
        }

        [Test]
        public void ASystemWithNoEntryIsOpenFromTheStart()
        {
            var rules = new FeatureUnlockRules().Add(Feature.Kingdom, "stage_1_2");
            var profile = new PlayerProfile();
            Assert.IsTrue(rules.IsOpen(Feature.Shop, profile));
            Assert.AreEqual("", rules.OpenedBy(Feature.Shop));
            CollectionAssert.IsEmpty(rules.IntrosDue(profile), "nothing to introduce for a system that was always there");
        }

        [Test]
        public void StageLabelsMatchTheMap()
        {
            Assert.AreEqual("1-2", FeatureUnlockRules.StageLabel("stage_1_2"));
            Assert.AreEqual("2-10", FeatureUnlockRules.StageLabel("stage_2_10"));
            Assert.AreEqual("custom", FeatureUnlockRules.StageLabel("custom"));
        }
    }
}
