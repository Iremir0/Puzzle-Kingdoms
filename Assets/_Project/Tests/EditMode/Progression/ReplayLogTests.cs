using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-28 (D-063): the replays the profile keeps.</summary>
    public class ReplayLogTests
    {
        [Test]
        public void OnlyTheLastFiveAreKeptNewestFirst()
        {
            var log = new ReplayLog();
            for (int i = 1; i <= 8; i++)
            {
                log.Add("battle " + i, "text " + i, true, i * 10);
            }

            Assert.AreEqual(ReplayLog.MaxRecent, log.Entries.Count);
            Assert.AreEqual("battle 8", log.Entries[0].Title);
            Assert.AreEqual("battle 4", log.Entries.Last().Title);
        }

        [Test]
        public void AStarredReplayStaysAndDoesNotCountAgainstTheRecentOnes()
        {
            var log = new ReplayLog();
            ReplayEntry keep = log.Add("keep me", "text", true, 100);
            Assert.IsTrue(log.ToggleStar(keep));
            for (int i = 0; i < 9; i++)
            {
                log.Add("later " + i, "text", false, 50);
            }

            Assert.IsTrue(log.Entries.Contains(keep));
            Assert.AreEqual(ReplayLog.MaxRecent + 1, log.Entries.Count);

            Assert.IsFalse(log.ToggleStar(keep), "unstarred");
            Assert.IsFalse(log.Entries.Contains(keep), "an old unstarred replay falls off at once");
        }

        [Test]
        public void TheStarredListHasALimit()
        {
            var log = new ReplayLog();
            for (int i = 0; i < ReplayLog.MaxStarred; i++)
            {
                Assert.IsTrue(log.ToggleStar(log.Add("s" + i, "t", true, 1)));
            }
            ReplayEntry extra = log.Add("one too many", "t", true, 1);
            Assert.IsFalse(log.ToggleStar(extra));
            Assert.IsFalse(extra.Starred);
        }

        [Test]
        public void ReplaysSurviveTheSaveInOrderAndAnOldSaveHasNone()
        {
            var profile = new PlayerProfile();
            profile.Replays.Add("first", "text one", false, 300);
            ReplayEntry second = profile.Replays.Add("second", "text two", true, 450);
            profile.Replays.ToggleStar(second);

            PlayerProfile loaded = PlayerProfile.FromSnapshot(profile.ToSnapshot());

            Assert.AreEqual(PlayerProfile.CurrentVersion, profile.ToSnapshot().version);
            Assert.AreEqual(2, loaded.Replays.Entries.Count);
            Assert.AreEqual("second", loaded.Replays.Entries[0].Title);
            Assert.IsTrue(loaded.Replays.Entries[0].Starred);
            Assert.IsTrue(loaded.Replays.Entries[0].Victory);
            Assert.AreEqual(450, loaded.Replays.Entries[0].Ticks);
            Assert.AreEqual("text one", loaded.Replays.Entries[1].Text);

            var old = new PlayerProfile.Snapshot { version = 7, replayTitles = null, replayTexts = null };
            Assert.AreEqual(0, PlayerProfile.FromSnapshot(old).Replays.Entries.Count);
        }
    }
}
