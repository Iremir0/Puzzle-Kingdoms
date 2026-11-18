using System.IO;
using NUnit.Framework;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic.Analytics;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Tests.Flow
{
    /// <summary>PB-29: events and error reports reach a file only with consent; the local remote-config file.</summary>
    public class AnalyticsFilesTests
    {
        private string _folder;
        private GameSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-analytics-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _settings = new GameSettings();
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_folder, true);

        private string[] Lines(string fileName)
        {
            string path = Path.Combine(_folder, fileName);
            return File.Exists(path) ? File.ReadAllLines(path) : new string[0];
        }

        [Test]
        public void EventsAreDroppedUntilThePlayerSaysYes()
        {
            var hub = new AnalyticsHub(() => _settings);
            hub.Add(new LocalEventLog(_folder));

            hub.Send(GameEvents.StageStarted("stage_1_1", 1));
            Assert.AreEqual(0, hub.Sent);
            CollectionAssert.IsEmpty(Lines(LocalEventLog.FileName), "not asked yet: nothing written");

            Consent.Answer(_settings, analytics: false, crashReports: true, personalisedAds: false);
            hub.Send(GameEvents.StageStarted("stage_1_1", 2));
            CollectionAssert.IsEmpty(Lines(LocalEventLog.FileName), "said no: nothing written");

            Consent.Answer(_settings, analytics: true, crashReports: false, personalisedAds: false);
            hub.Send(GameEvents.StageStarted("stage_1_1", 3));
            string[] lines = Lines(LocalEventLog.FileName);
            Assert.AreEqual(1, lines.Length);
            StringAssert.StartsWith("{\"event\":\"stage_started\",\"at\":\"", lines[0]);
            StringAssert.EndsWith("\"stage\":\"stage_1_1\",\"attempt\":3}", lines[0]);
        }

        private sealed class BrokenSink : IAnalyticsSink
        {
            public void Write(AnalyticsEvent analyticsEvent, string utcTime) => throw new IOException("disk full");
        }

        [Test]
        public void ABrokenSinkDoesNotStopTheOthers()
        {
            Consent.Answer(_settings, true, false, false);
            var hub = new AnalyticsHub(() => _settings);
            hub.Add(new BrokenSink());
            hub.Add(new LocalEventLog(_folder));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("BrokenSink failed"));
            hub.Send(GameEvents.TutorialSkipped("1-1.puzzle"));
            Assert.AreEqual(1, Lines(LocalEventLog.FileName).Length);
        }

        [Test]
        public void ErrorsAreWrittenOnlyWithConsentAndCapped()
        {
            using (var reporter = new ErrorReporter(_folder, () => _settings, "0.1.0"))
            {
                reporter.Record("NullReferenceException: boom", "GameApp.Start ()\nother frame", LogType.Exception);
                CollectionAssert.IsEmpty(Lines(ErrorReporter.FileName), "no consent: nothing written");

                Consent.Answer(_settings, false, true, false);
                reporter.Record("a warning", "", LogType.Warning);
                reporter.Record("NullReferenceException: boom", "GameApp.Start ()\nother frame", LogType.Exception);
                string[] lines = Lines(ErrorReporter.FileName);
                Assert.AreEqual(1, lines.Length, "only exceptions");
                StringAssert.Contains("\"where\":\"GameApp.Start ()\"", lines[0]);
                StringAssert.Contains("\"version\":\"0.1.0\"", lines[0]);

                for (int i = 0; i < ErrorReporter.MaxPerSession + 5; i++)
                {
                    reporter.Record("again", "", LogType.Exception);
                }
                Assert.AreEqual(ErrorReporter.MaxPerSession, Lines(ErrorReporter.FileName).Length, "capped per session");
            }
        }

        [Test]
        public void RemoteConfigFileIsOptional()
        {
            Assert.AreEqual(0, RemoteConfigFile.Load(_folder).Count, "no file: no overrides");
            File.WriteAllText(Path.Combine(_folder, RemoteConfigFile.FileName), "stage.stage_1_1.moves = 15\n");
            Assert.IsTrue(RemoteConfigFile.Load(_folder).TryGetInt("stage.stage_1_1.moves", out int moves));
            Assert.AreEqual(15, moves);
        }

        [Test]
        public void OldSettingsFilesLoadAsNotAsked()
        {
            File.WriteAllText(Path.Combine(_folder, SettingsStore.FileName), "{\"version\":1,\"sound\":false,\"music\":true,\"battleSpeed\":2}");
            GameSettings loaded = new SettingsStore(_folder).Load();
            Assert.IsFalse(loaded.sound);
            Assert.IsTrue(Consent.NeedsAsking(loaded));
            Assert.IsFalse(loaded.analytics);
            Assert.AreEqual("", loaded.language);
        }
    }
}
