using NUnit.Framework;
using PuzzleKingdoms.Logic.Analytics;

namespace PuzzleKingdoms.Tests.Analytics
{
    /// <summary>PB-36: the events that show where a first session stops.</summary>
    public class FunnelEventTests
    {
        [Test]
        public void TutorialStepNamesTheStep()
        {
            AnalyticsEvent e = GameEvents.TutorialStep("1-1.match");
            Assert.AreEqual("tutorial_step", e.Name);
            Assert.AreEqual("1-1.match", e["step"]);
        }

        [Test]
        public void EveryKnownScreenCanBeLoggedAndUnknownOnesAreRefused()
        {
            foreach (string screen in GameEvents.Screens)
            {
                Assert.AreEqual(screen, GameEvents.ScreenOpened(screen)["screen"]);
            }
            Assert.Throws<System.ArgumentException>(() => GameEvents.ScreenOpened("Kingdom"), "names are lower case");
        }

        [Test]
        public void TheTabNamesAreKnownScreens()
        {
            // GameApp logs a tab as its lower-case name; keep the list in step with MainTab.
            foreach (string tab in new[] { "map", "kingdom", "commanders", "shop", "quests" })
            {
                CollectionAssert.Contains(GameEvents.Screens, tab);
            }
        }

        [Test]
        public void AdEventsCarryThePlacement()
        {
            AnalyticsEvent asked = GameEvents.AdRequested("extra_moves", false);
            Assert.AreEqual("ad_requested", asked.Name);
            Assert.AreEqual("extra_moves", asked["placement"]);
            Assert.AreEqual(false, asked["supporter"]);
            Assert.AreEqual("ad_rewarded", GameEvents.AdRewarded("shop_gold").Name);
            Assert.Throws<System.ArgumentException>(() => GameEvents.AdRewarded("banner"));
        }

        [Test]
        public void ProgressEventsCarryTheirNumbers()
        {
            AnalyticsEvent building = GameEvents.BuildingUpgraded("barracks", 3, 120);
            Assert.AreEqual(3, building["level"]);
            Assert.AreEqual(120, building["gold_left"]);

            AnalyticsEvent expedition = GameEvents.ExpeditionEnded(4, false, 240);
            Assert.AreEqual("expedition_ended", expedition.Name);
            Assert.AreEqual(4, expedition["depth"]);
            Assert.AreEqual(false, expedition["won"]);

            Assert.AreEqual(55, GameEvents.QuestClaimed(400, 55)["stars"]);
            Assert.AreEqual("fireball", GameEvents.CommanderChosen("fireball")["commander"]);
            Assert.AreEqual("kingdom", GameEvents.FeatureUnlocked("kingdom")["feature"]);
        }

        [Test]
        public void SessionPausedRoundsTheSeconds()
        {
            AnalyticsEvent e = GameEvents.SessionPaused(93.46f, "stage_1_3");
            Assert.AreEqual(93.5f, e["seconds"]);
            Assert.AreEqual("stage_1_3", e["stage_reached"]);
            StringAssert.Contains("\"event\":\"session_paused\"", e.ToJsonLine(""));
        }
    }
}
