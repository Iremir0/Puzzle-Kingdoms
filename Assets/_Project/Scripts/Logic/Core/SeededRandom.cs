using System;

namespace PuzzleKingdoms.Logic
{
    /// <summary>
    /// PCG32 generator: small, fast and bit-identical on every platform and
    /// runtime (Mono, IL2CPP, .NET), which System.Random does not promise.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private const ulong Increment = 1442695040888963407UL;

        private ulong _state;

        public SeededRandom(ulong seed)
        {
            _state = 0;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        /// <summary>Seed from the clock, for boards that need no reproducibility.</summary>
        public static SeededRandom FromTime() => new SeededRandom((ulong)DateTime.UtcNow.Ticks);

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + Increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentException("maxExclusive must be greater than minInclusive");
            }
            uint range = (uint)(maxExclusive - minInclusive);
            // Rejection sampling removes modulo bias.
            uint threshold = (uint)((1UL << 32) % range);
            uint value;
            do
            {
                value = NextUInt();
            } while (value < threshold);
            return minInclusive + (int)(value % range);
        }
    }
}
