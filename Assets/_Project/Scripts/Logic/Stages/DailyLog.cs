using System;
using System.Collections.Generic;
using System.Globalization;

namespace PuzzleKingdoms.Logic.Stages
{
    /// <summary>
    /// The daily challenge results saved in the profile (PB-25): one per day, the last <see cref="MaxDays"/>
    /// days kept. Parallel lists so JsonUtility can store them.
    /// </summary>
    public sealed class DailyLog
    {
        public const int MaxDays = 30;

        public readonly List<string> Days = new List<string>();
        public readonly List<bool> Won = new List<bool>();
        public readonly List<int> HpPercent = new List<int>();
        public readonly List<int> Ticks = new List<int>();

        public bool HasPlayed(string day) => Days.Contains(day);

        /// <summary>The result of a day, or null if it was not played.</summary>
        public DailyResult Result(string day)
        {
            int at = Days.IndexOf(day);
            return at < 0 ? null : new DailyResult { Day = day, Won = Won[at], HpPercent = HpPercent[at], Ticks = Ticks[at] };
        }

        /// <summary>Saves a day's result. False when that day already has one: one attempt a day.</summary>
        public bool Record(DailyResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.Day) || HasPlayed(result.Day))
            {
                return false;
            }
            Days.Add(result.Day);
            Won.Add(result.Won);
            HpPercent.Add(result.HpPercent);
            Ticks.Add(result.Ticks);
            while (Days.Count > MaxDays)
            {
                Days.RemoveAt(0);
                Won.RemoveAt(0);
                HpPercent.RemoveAt(0);
                Ticks.RemoveAt(0);
            }
            return true;
        }

        /// <summary>The best result kept, or null when nothing was played.</summary>
        public DailyResult Best()
        {
            DailyResult best = null;
            foreach (string day in Days)
            {
                DailyResult result = Result(day);
                if (result.IsBetterThan(best))
                {
                    best = result;
                }
            }
            return best;
        }

        /// <summary>Wins among the kept days.</summary>
        public int WinCount
        {
            get
            {
                int wins = 0;
                foreach (bool won in Won)
                {
                    wins += won ? 1 : 0;
                }
                return wins;
            }
        }

        /// <summary>The last <paramref name="count"/> days up to and including <paramref name="today"/>, oldest first; null where nothing was played.</summary>
        public List<DailyResult> Recent(string today, int count)
        {
            var list = new List<DailyResult>();
            if (!DateTime.TryParseExact(today, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return list;
            }
            for (int i = count - 1; i >= 0; i--)
            {
                list.Add(Result(date.AddDays(-i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            }
            return list;
        }
    }
}
