using NUnit.Framework;
using UnityEngine;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>
    /// Runs once around all PlayMode tests: the master volume is zero, so tests never play sound through the
    /// speakers. Sfx still counts what it "plays", so sound assertions keep working.
    /// </summary>
    [SetUpFixture]
    public sealed class SilentTests
    {
        private float _volume;

        [OneTimeSetUp]
        public void Mute()
        {
            _volume = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        [OneTimeTearDown]
        public void Restore() => AudioListener.volume = _volume;
    }
}
