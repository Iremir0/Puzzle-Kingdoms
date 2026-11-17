using System.IO;
using NUnit.Framework;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Flow
{
    /// <summary>PB-14: the save file survives bad files, crashes and newer versions (D-039).</summary>
    public class SaveFileTests
    {
        private string _folder;
        private ProfileStore _store;

        private string Tmp => _store.Path + SafeFile.TempSuffix;
        private string Bak => _store.Path + ProfileStore.BackupSuffix;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pk-save-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _store = new ProfileStore(_folder);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_folder, true);

        private static PlayerProfile SomeProgress()
        {
            var profile = new PlayerProfile();
            profile.Apply("stage_1_1", new StageReward { Stars = 3, VictoryGold = 30 });
            profile.Apply("stage_1_2", new StageReward { Stars = 1, VictoryGold = 40, DismissGold = 5 });
            return profile;
        }

        private static void AssertSameProgress(PlayerProfile expected, PlayerProfile actual)
        {
            Assert.AreEqual(expected.Gold, actual.Gold);
            Assert.AreEqual(expected.BestStars("stage_1_1"), actual.BestStars("stage_1_1"));
            Assert.AreEqual(expected.BestStars("stage_1_2"), actual.BestStars("stage_1_2"));
        }

        [Test]
        public void MissingFileGivesAFreshProfile()
        {
            PlayerProfile profile = _store.Load();

            Assert.AreEqual(0, profile.Gold);
            Assert.AreEqual(0, profile.BestStars("stage_1_1"));
            Assert.IsFalse(File.Exists(Bak));
        }

        [Test]
        public void SaveThenLoadGivesTheSameGoldAndStars()
        {
            PlayerProfile saved = SomeProgress();

            _store.Save(saved);

            AssertSameProgress(saved, new ProfileStore(_folder).Load());
            Assert.IsFalse(File.Exists(Tmp), "no temp file left behind");
        }

        [Test]
        public void SavingAgainReplacesTheFile()
        {
            _store.Save(new PlayerProfile());
            PlayerProfile later = SomeProgress();

            _store.Save(later);

            AssertSameProgress(later, _store.Load());
            Assert.IsFalse(File.Exists(Tmp));
        }

        [Test]
        public void CorruptFileStartsFreshAndKeepsABackup()
        {
            File.WriteAllText(_store.Path, "{{{");

            PlayerProfile profile = _store.Load();

            Assert.AreEqual(0, profile.Gold);
            Assert.AreEqual("{{{", File.ReadAllText(Bak));
        }

        [Test]
        public void EmptyFileStartsFreshAndKeepsABackup()
        {
            File.WriteAllText(_store.Path, "");

            Assert.AreEqual(0, _store.Load().Gold);
            Assert.IsTrue(File.Exists(Bak));
        }

        [Test]
        public void NewerSaveVersionStartsFreshAndKeepsTheFileAsBackup()
        {
            // D-039: a save from a newer build (after a downgrade) is not read but kept.
            string newer = "{\"version\": " + (PlayerProfile.CurrentVersion + 1) + ", \"gold\": 500}";
            File.WriteAllText(_store.Path, newer);

            PlayerProfile profile = _store.Load();

            Assert.AreEqual(0, profile.Gold);
            Assert.AreEqual(newer, File.ReadAllText(Bak));
        }

        [Test]
        public void LeftoverTempFileDoesNotBreakSaveOrLoad()
        {
            PlayerProfile saved = SomeProgress();
            _store.Save(saved);
            File.WriteAllText(Tmp, "half a sa"); // a crash while writing the next save

            AssertSameProgress(saved, _store.Load());
            _store.Save(saved);
            AssertSameProgress(saved, _store.Load());
            Assert.IsFalse(File.Exists(Tmp));
        }

        [Test]
        public void SaveLeftOnlyInTheTempFileIsRecovered()
        {
            // What the old delete-then-move could leave after a crash between the two steps.
            PlayerProfile saved = SomeProgress();
            _store.Save(saved);
            File.Move(_store.Path, Tmp);

            AssertSameProgress(saved, _store.Load());
        }

        [Test]
        public void SettingsRoundTripAndLiveInTheirOwnFile()
        {
            var store = new SettingsStore(_folder);
            Assert.IsTrue(store.Load().sound, "defaults without a file");

            store.Save(new GameSettings { sound = false, music = true, battleSpeed = 2 });
            _store.Save(new PlayerProfile()); // a progress reset does not touch the settings

            GameSettings loaded = new SettingsStore(_folder).Load();
            Assert.IsFalse(loaded.sound);
            Assert.IsTrue(loaded.music);
            Assert.AreEqual(2, loaded.BattleSpeed);
            Assert.AreNotEqual(_store.Path, store.Path);
        }

        [Test]
        public void BrokenSettingsFileGivesDefaults()
        {
            var store = new SettingsStore(_folder);
            File.WriteAllText(store.Path, "not json");

            GameSettings settings = store.Load();

            Assert.IsTrue(settings.sound);
            Assert.IsTrue(settings.music);
            Assert.AreEqual(1, settings.BattleSpeed);
        }

        [Test]
        public void HandEditedBattleSpeedIsNormalized()
        {
            var store = new SettingsStore(_folder);
            File.WriteAllText(store.Path, "{\"sound\": true, \"music\": false, \"battleSpeed\": 7}");

            GameSettings settings = store.Load();

            Assert.AreEqual(1, settings.battleSpeed);
            Assert.IsFalse(settings.music);
        }
    }
}
