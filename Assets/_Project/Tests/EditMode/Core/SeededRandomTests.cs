using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic;

namespace PuzzleKingdoms.Tests.Core
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeedGivesSameSequence()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.Range(0, 1000), b.Range(0, 1000));
            }
        }

        [Test]
        public void DifferentSeedsDiffer()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);
            int same = 0;
            for (int i = 0; i < 100; i++)
            {
                if (a.Range(0, 1000) == b.Range(0, 1000))
                {
                    same++;
                }
            }
            Assert.Less(same, 10);
        }

        [Test]
        public void RangeStaysInBoundsAndCoversAllValues()
        {
            var random = new SeededRandom(7);
            var seen = new HashSet<int>();
            for (int i = 0; i < 1000; i++)
            {
                int value = random.Range(-2, 3);
                Assert.That(value, Is.InRange(-2, 2));
                seen.Add(value);
            }
            Assert.AreEqual(5, seen.Count);
        }

        [Test]
        public void KnownSequenceIsStableAcrossRuntimes()
        {
            // Guards against accidental algorithm changes: daily challenges and
            // replays depend on these exact numbers.
            var random = new SeededRandom(12345);
            var values = new List<uint>();
            for (int i = 0; i < 3; i++)
            {
                values.Add(random.NextUInt());
            }
            var again = new SeededRandom(12345);
            Assert.AreEqual(values, new List<uint> { again.NextUInt(), again.NextUInt(), again.NextUInt() });
            Assert.AreEqual(KnownValues.Pcg32Seed12345First, values[0]);
        }
    }

    internal static class KnownValues
    {
        // PCG32 (pcg32_srandom_r with our fixed increment), cross-checked against
        // an independent implementation that reproduces the reference outputs.
        public const uint Pcg32Seed12345First = 1411482639u;
    }
}
