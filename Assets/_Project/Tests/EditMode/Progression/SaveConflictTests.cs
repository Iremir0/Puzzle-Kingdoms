using System;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-31 (D-065): which of two saves wins, and when the player has to choose.</summary>
    public class SaveConflictTests
    {
        private static readonly DateTime Early = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Late = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

        private static PlayerProfile.Snapshot Save(int gold = 0, params (string stage, int stars)[] stages)
        {
            var snapshot = new PlayerProfile.Snapshot { gold = gold };
            foreach ((string stage, int stars) in stages)
            {
                snapshot.stageIds.Add(stage);
                snapshot.stageStars.Add(stars);
            }
            return snapshot;
        }

        [Test]
        public void NoCloudSaveMeansKeepTheLocalOne_AndNoLocalSaveMeansTakeTheCloud()
        {
            Assert.AreEqual(SaveChoice.KeepLocal, SaveConflict.Resolve(Save(), Late, null, Early));
            Assert.AreEqual(SaveChoice.UseCloud, SaveConflict.Resolve(null, Late, Save(0, ("1-1", 2)), Early));
        }

        [Test]
        public void TheSaveThatIsAheadWinsWhateverTheTime()
        {
            var behind = Save(900, ("1-1", 2));
            var ahead = Save(10, ("1-1", 3), ("1-2", 1));

            Assert.AreEqual(SaveChoice.UseCloud, SaveConflict.Resolve(behind, Late, ahead, Early), "the cloud has more stars though older");
            Assert.AreEqual(SaveChoice.KeepLocal, SaveConflict.Resolve(ahead, Early, behind, Late), "this phone has more though older");
        }

        [Test]
        public void BuildingsAndTheExpeditionRecordCountAsProgressToo()
        {
            var plain = Save(0, ("1-1", 1));
            var built = Save(0, ("1-1", 1));
            built.buildingIds.Add("barracks");
            built.buildingLevels.Add(2);
            Assert.AreEqual(SaveChoice.UseCloud, SaveConflict.Resolve(plain, Late, built, Early));

            var deep = Save(0, ("1-1", 1));
            deep.bestExpeditionDepth = 4;
            Assert.AreEqual(SaveChoice.KeepLocal, SaveConflict.Resolve(deep, Early, plain, Late));
        }

        [Test]
        public void TheSameProgressGoesByTimeAndGoldIsNotProgress()
        {
            var a = Save(100, ("1-1", 2));
            var b = Save(5000, ("1-1", 2));

            Assert.AreEqual(SaveChoice.UseCloud, SaveConflict.Resolve(a, Early, b, Late), "the cloud was written later");
            Assert.AreEqual(SaveChoice.KeepLocal, SaveConflict.Resolve(a, Late, b, Early), "this phone was written later");
            Assert.AreEqual(SaveChoice.KeepLocal, SaveConflict.Resolve(a, Late, Save(100, ("1-1", 2)), Late), "equal: nothing to change");
        }

        [Test]
        public void EachSideAheadSomewhereMakesThePlayerChoose()
        {
            var phone = Save(0, ("1-1", 3), ("1-2", 3));
            var cloud = Save(0, ("1-1", 1));
            cloud.buildingIds.Add("treasury");
            cloud.buildingLevels.Add(1);

            Assert.AreEqual(SaveChoice.Ask, SaveConflict.Resolve(phone, Late, cloud, Early));
            Assert.AreEqual(SaveChoice.Ask, SaveConflict.Resolve(cloud, Early, phone, Late), "the other way round too");
        }

        [Test]
        public void ACloudSaveFromANewerGameIsLeftAlone()
        {
            var newer = Save(0, ("1-1", 3));
            newer.version = PlayerProfile.CurrentVersion + 1;
            Assert.AreEqual(SaveChoice.CloudTooNew, SaveConflict.Resolve(Save(), Late, newer, Late));
        }

        [Test]
        public void TheSummaryCountsStarsStagesAndGold()
        {
            (int stars, int stages, int gold) = SaveConflict.Summary(Save(420, ("1-1", 3), ("1-2", 1), ("1-3", 0)));
            Assert.AreEqual(4, stars);
            Assert.AreEqual(2, stages);
            Assert.AreEqual(420, gold);
        }
    }
}
