using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-13, D-040: tutorial prompts show once per player and the profile remembers them.</summary>
    public class TutorialTests
    {
        private static readonly List<TutorialStep> Steps = new List<TutorialStep>
        {
            new TutorialStep { Id = "1-1.puzzle", Trigger = TutorialTrigger.PuzzleStarted, Text = "Match 3" },
            new TutorialStep { Id = "1-1.unit", Trigger = TutorialTrigger.UnitRecruited, Text = "A Warrior joined" },
            new TutorialStep { Id = "1-1.camp", Trigger = TutorialTrigger.WarCampOpened, Text = "Drag", Target = "Bench" },
            new TutorialStep { Id = "1-1.camp2", Trigger = TutorialTrigger.WarCampOpened, Text = "Merge" },
        };

        private static string Ids(IEnumerable<TutorialStep> steps) => string.Join(",", steps.Select(s => s.Id));

        [Test]
        public void StepsForATriggerComeInOrder()
        {
            var profile = new PlayerProfile();

            Assert.AreEqual("1-1.puzzle", Ids(TutorialGuide.StepsFor(Steps, TutorialTrigger.PuzzleStarted, profile)));
            Assert.AreEqual("1-1.camp,1-1.camp2", Ids(TutorialGuide.StepsFor(Steps, TutorialTrigger.WarCampOpened, profile)));
            Assert.IsEmpty(TutorialGuide.StepsFor(Steps, TutorialTrigger.BattleLost, profile));
        }

        [Test]
        public void SeenStepsDoNotShowAgain()
        {
            var profile = new PlayerProfile();
            int changes = 0;
            profile.Changed += () => changes++;

            profile.MarkStepSeen("1-1.camp");
            profile.MarkStepSeen("1-1.camp");

            Assert.AreEqual("1-1.camp2", Ids(TutorialGuide.StepsFor(Steps, TutorialTrigger.WarCampOpened, profile)));
            Assert.AreEqual(1, changes, "marking twice changes once");
        }

        [Test]
        public void SkippingHidesEverythingUntilReset()
        {
            var profile = new PlayerProfile();
            profile.MarkStepSeen("1-1.puzzle");

            profile.SkipTutorial();
            Assert.IsTrue(profile.TutorialSkipped);
            Assert.IsEmpty(TutorialGuide.StepsFor(Steps, TutorialTrigger.WarCampOpened, profile));

            profile.ResetTutorial();
            Assert.IsFalse(profile.TutorialSkipped);
            Assert.IsFalse(profile.HasSeenStep("1-1.puzzle"));
            Assert.AreEqual("1-1.puzzle", Ids(TutorialGuide.StepsFor(Steps, TutorialTrigger.PuzzleStarted, profile)));
        }

        [Test]
        public void ResetTutorialKeepsGoldAndStars()
        {
            var profile = new PlayerProfile();
            profile.Apply("stage_1_1", new StageReward { Stars = 2, VictoryGold = 30 });

            profile.ResetTutorial();

            Assert.AreEqual(30, profile.Gold);
            Assert.AreEqual(2, profile.BestStars("stage_1_1"));
        }

        [Test]
        public void StepsWithoutIdOrDuplicatedAreIgnored()
        {
            var steps = new List<TutorialStep>
            {
                new TutorialStep { Id = "", Trigger = TutorialTrigger.PuzzleStarted, Text = "no id" },
                null,
                new TutorialStep { Id = "a", Trigger = TutorialTrigger.PuzzleStarted, Text = "first" },
                new TutorialStep { Id = "a", Trigger = TutorialTrigger.PuzzleStarted, Text = "copy" },
            };

            List<TutorialStep> shown = TutorialGuide.StepsFor(steps, TutorialTrigger.PuzzleStarted, new PlayerProfile());

            Assert.AreEqual(1, shown.Count);
            Assert.AreEqual("first", shown[0].Text);
            Assert.IsEmpty(TutorialGuide.StepsFor(null, TutorialTrigger.PuzzleStarted, new PlayerProfile()));
        }

        [Test]
        public void TutorialProgressSurvivesTheSnapshot()
        {
            var profile = new PlayerProfile();
            profile.MarkStepSeen("1-1.puzzle");
            profile.MarkStepSeen("1-2.camp");
            profile.SkipTutorial();

            PlayerProfile.Snapshot snapshot = profile.ToSnapshot();
            PlayerProfile restored = PlayerProfile.FromSnapshot(snapshot);

            Assert.AreEqual(2, snapshot.version);
            CollectionAssert.AreEqual(new[] { "1-1.puzzle", "1-2.camp" }, restored.SeenTutorialSteps);
            Assert.IsTrue(restored.TutorialSkipped);
        }

        [Test]
        public void VersionOneSavesLoadWithNoTutorialSeen()
        {
            // A version 1 file has no tutorial fields; the serializer leaves the defaults.
            var old = new PlayerProfile.Snapshot { version = 1, gold = 70 };
            old.stageIds.Add("stage_1_1");
            old.stageStars.Add(3);
            old.seenTutorialSteps = null;

            PlayerProfile profile = PlayerProfile.FromSnapshot(old);

            Assert.AreEqual(70, profile.Gold);
            Assert.AreEqual(3, profile.BestStars("stage_1_1"));
            Assert.IsEmpty(profile.SeenTutorialSteps);
            Assert.IsFalse(profile.TutorialSkipped);
        }
    }
}
