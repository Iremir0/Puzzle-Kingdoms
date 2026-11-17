using NUnit.Framework;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-14: the settings model.</summary>
    public class GameSettingsTests
    {
        [Test]
        public void DefaultsAreSoundAndMusicOnAtNormalSpeed()
        {
            var settings = new GameSettings();

            Assert.IsTrue(settings.sound);
            Assert.IsTrue(settings.music);
            Assert.AreEqual(1, settings.BattleSpeed);
            Assert.AreEqual(GameSettings.CurrentVersion, settings.version);
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(0, 1)]
        [TestCase(3, 1)]
        [TestCase(-2, 1)]
        public void OnlyAllowedBattleSpeedsAreUsed(int stored, int used)
        {
            var settings = new GameSettings { battleSpeed = stored };

            Assert.AreEqual(used, settings.BattleSpeed);
            Assert.AreEqual(used, settings.Normalize().battleSpeed);
        }

        [Test]
        public void CopyIsIndependent()
        {
            var settings = new GameSettings { music = false, battleSpeed = 2 };

            GameSettings copy = settings.Copy();
            copy.music = true;

            Assert.IsFalse(settings.music);
            Assert.AreEqual(2, copy.battleSpeed);
        }
    }
}
