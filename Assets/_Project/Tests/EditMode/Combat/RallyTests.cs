using NUnit.Framework;
using PuzzleKingdoms.Logic.Combat;

namespace PuzzleKingdoms.Tests.Combat
{
    public class RallyTests
    {
        [Test]
        public void ActivationConsumesACharge()
        {
            var rally = new Rally(2, 100);
            Assert.IsTrue(rally.Activate());
            Assert.AreEqual(1, rally.Charges);
            Assert.IsTrue(rally.IsActive);
        }

        [Test]
        public void CannotStackActivations()
        {
            var rally = new Rally(2, 100);
            rally.Activate();
            Assert.IsFalse(rally.Activate());
            Assert.AreEqual(1, rally.Charges);
        }

        [Test]
        public void CannotActivateWithoutCharges()
        {
            var rally = new Rally(0, 100);
            Assert.IsFalse(rally.CanActivate);
            Assert.IsFalse(rally.Activate());
        }

        [Test]
        public void ExpiresAfterDuration()
        {
            var rally = new Rally(1, 3);
            rally.Activate();
            Assert.IsFalse(rally.Tick());
            Assert.IsFalse(rally.Tick());
            Assert.IsTrue(rally.Tick(), "ends on tick 3");
            Assert.IsFalse(rally.IsActive);
            Assert.IsFalse(rally.Tick(), "no further end events");
        }
    }
}
