using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>D-067: saves from older versions load, saves from newer versions are recognised and left alone.</summary>
    public class SaveVersionTests
    {
        [Test]
        public void OnlyANewerVersionIsTooNew()
        {
            Assert.IsFalse(PlayerProfile.IsTooNew(null));
            Assert.IsFalse(PlayerProfile.IsTooNew(new PlayerProfile.Snapshot { version = 1 }));
            Assert.IsFalse(PlayerProfile.IsTooNew(new PlayerProfile.Snapshot()), "a fresh snapshot is the current version");
            Assert.IsTrue(PlayerProfile.IsTooNew(new PlayerProfile.Snapshot { version = PlayerProfile.CurrentVersion + 1 }));
        }

        [Test]
        public void ACloudSaveFromANewerVersionIsLeftAlone()
        {
            var local = new PlayerProfile.Snapshot();
            var cloud = new PlayerProfile.Snapshot { version = PlayerProfile.CurrentVersion + 1, gold = 900 };

            Assert.AreEqual(SaveChoice.CloudTooNew, SaveConflict.Resolve(local, System.DateTime.MinValue, cloud, System.DateTime.UtcNow));
        }

        /// <summary>A save with only the version 1 fields: everything added later is missing (null).</summary>
        private static PlayerProfile.Snapshot VersionOne() => new PlayerProfile.Snapshot
        {
            version = 1,
            gold = 120,
            stageIds = new List<string> { "stage_1_1", "stage_1_2" },
            stageStars = new List<int> { 3, 2 },
            seenTutorialSteps = null,
            buildingIds = null,
            buildingLevels = null,
            commanderId = null,
            questDay = null,
            dailyIds = null,
            dailyProgress = null,
            dailyClaimed = null,
            achievementIds = null,
            achievementProgress = null,
            achievementClaimed = null,
            dailyDays = null,
            dailyWon = null,
            dailyHp = null,
            dailyTicks = null,
            expedition = null,
            replayTitles = null,
            replayTexts = null,
            replayVictory = null,
            replayTicks = null,
            replayStarred = null,
            adGoldDay = null,
        };

        [Test]
        public void AVersionOneSaveWithNothingNewerLoads()
        {
            PlayerProfile profile = PlayerProfile.FromSnapshot(VersionOne());

            Assert.AreEqual(120, profile.Gold);
            Assert.AreEqual(3, profile.BestStars("stage_1_1"));
            Assert.AreEqual(2, profile.BestStars("stage_1_2"));
            Assert.AreEqual("", profile.CommanderId);
            Assert.IsNull(profile.ActiveExpedition);
            Assert.AreEqual(0, profile.BestExpeditionDepth);
        }

        [Test]
        public void ALoadedOldSaveIsWrittenBackAsTheCurrentVersion()
        {
            PlayerProfile.Snapshot again = PlayerProfile.FromSnapshot(VersionOne()).ToSnapshot();

            Assert.AreEqual(PlayerProfile.CurrentVersion, again.version);
            Assert.AreEqual(120, again.gold);
            Assert.AreEqual(2, again.stageIds.Count);
            Assert.AreEqual(3, PlayerProfile.FromSnapshot(again).BestStars("stage_1_1"));
        }
    }
}
