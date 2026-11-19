using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>One saved battle replay (PB-28): the replay text plus what the list shows.</summary>
    public sealed class ReplayEntry
    {
        public string Title = "";
        public string Text = "";
        public bool Victory;
        public int Ticks;
        public bool Starred;
    }

    /// <summary>
    /// The replays the profile keeps (PB-28, D-063): the last <see cref="MaxRecent"/> battles, newest first, and
    /// those the player starred, which stay (up to <see cref="MaxStarred"/>) until unstarred.
    /// </summary>
    public sealed class ReplayLog
    {
        public const int MaxRecent = 5;
        public const int MaxStarred = 10;

        private readonly List<ReplayEntry> _entries = new List<ReplayEntry>();

        /// <summary>Newest first.</summary>
        public IReadOnlyList<ReplayEntry> Entries => _entries;

        /// <summary>Adds a replay in front and drops the oldest unstarred ones beyond the limit.</summary>
        public ReplayEntry Add(string title, string text, bool victory, int ticks)
        {
            var entry = new ReplayEntry { Title = title ?? "", Text = text ?? "", Victory = victory, Ticks = ticks };
            _entries.Insert(0, entry);
            Trim();
            return entry;
        }

        /// <summary>Stars or unstars a replay. A star is refused when the starred list is full. Returns the new state.</summary>
        public bool ToggleStar(ReplayEntry entry)
        {
            if (entry == null || !_entries.Contains(entry))
            {
                return false;
            }
            if (entry.Starred)
            {
                entry.Starred = false;
                Trim();
                return false;
            }
            int starred = _entries.FindAll(e => e.Starred).Count;
            if (starred >= MaxStarred)
            {
                return false;
            }
            entry.Starred = true;
            return true;
        }

        public void Remove(ReplayEntry entry) => _entries.Remove(entry);

        private void Trim()
        {
            int recent = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Starred)
                {
                    continue;
                }
                recent++;
                if (recent > MaxRecent)
                {
                    _entries.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>Appends an entry read from a save without trimming (the order is the saved order).</summary>
        internal void Load(ReplayEntry entry) => _entries.Add(entry);
    }
}
