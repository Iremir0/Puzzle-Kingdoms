using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-23 (D-053): commanders are unlocked by stages, chosen in the profile and saved.</summary>
    public class CommanderTests
    {
        private static List<Commander> Commanders() => new List<Commander>
        {
            new Commander { Id = "shield_wall", DisplayName = "Shield Wall", UnlockStageId = "stage_1_3", Variant = new RallyVariant { Kind = RallyKind.ShieldWall } },
            new Commander { Id = "arrow_rain", DisplayName = "Arrow Rain", UnlockStageId = "stage_2_3", Variant = new RallyVariant { Kind = RallyKind.ArrowRain } },
        };

        private static void Win(PlayerProfile profile, string stage) =>
            profile.Apply(stage, new StageReward { Stars = 1 });

        [Test]
        public void ACommanderUnlocksWithTheFirstWinOfItsStage()
        {
            var profile = new PlayerProfile();
            Commander shield = Commanders()[0];
            Assert.IsFalse(profile.IsUnlocked(shield));
            profile.Apply("stage_1_3", new StageReward { Stars = 0 });
            Assert.IsFalse(profile.IsUnlocked(shield), "a defeat does not unlock");
            Win(profile, "stage_1_3");
            Assert.IsTrue(profile.IsUnlocked(shield));
        }

        [Test]
        public void ALockedOrUnknownCommanderCannotBeChosen()
        {
            var profile = new PlayerProfile();
            Assert.IsFalse(profile.TryChooseCommander(Commanders(), "shield_wall"));
            Assert.IsFalse(profile.TryChooseCommander(Commanders(), "nobody"));
            Assert.AreEqual("", profile.CommanderId);
        }

        [Test]
        public void ChoosingAnUnlockedCommanderWorksAndNoneIsAlwaysAllowed()
        {
            var profile = new PlayerProfile();
            Win(profile, "stage_1_3");
            int changes = 0;
            profile.Changed += () => changes++;
            Assert.IsTrue(profile.TryChooseCommander(Commanders(), "shield_wall"));
            Assert.AreEqual("shield_wall", profile.CommanderId);
            Assert.AreEqual(1, changes);
            Assert.IsTrue(profile.TryChooseCommander(Commanders(), "shield_wall"));
            Assert.AreEqual(1, changes, "the same choice changes nothing");
            Assert.IsTrue(profile.TryChooseCommander(Commanders(), ""));
            Assert.AreEqual("", profile.CommanderId);
        }

        [Test]
        public void TheChosenCommanderGivesItsVariantOtherwiseNull()
        {
            var profile = new PlayerProfile();
            Assert.IsNull(profile.ChosenCommander(Commanders()), "no commander: the classic Rally");
            Win(profile, "stage_1_3");
            profile.TryChooseCommander(Commanders(), "shield_wall");
            Assert.AreEqual(RallyKind.ShieldWall, profile.ChosenCommander(Commanders()).Variant.Kind);
        }

        [Test]
        public void AChosenCommanderWhoIsNoLongerKnownFallsBackToNone()
        {
            var profile = new PlayerProfile();
            Win(profile, "stage_1_3");
            profile.TryChooseCommander(Commanders(), "shield_wall");
            Assert.IsNull(profile.ChosenCommander(new List<Commander>()), "removed from the data");
        }

        [Test]
        public void TheChoiceSurvivesASaveAndOlderSavesHaveNone()
        {
            var profile = new PlayerProfile();
            Win(profile, "stage_1_3");
            profile.TryChooseCommander(Commanders(), "shield_wall");
            PlayerProfile loaded = PlayerProfile.FromSnapshot(profile.ToSnapshot());
            Assert.AreEqual("shield_wall", loaded.CommanderId);
            Assert.AreEqual(1, loaded.BestStars("stage_1_3"));
            PlayerProfile old = PlayerProfile.FromSnapshot(new PlayerProfile.Snapshot { version = 3, commanderId = null });
            Assert.AreEqual("", old.CommanderId);
        }
    }
}
