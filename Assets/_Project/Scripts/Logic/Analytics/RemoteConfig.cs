using System.Collections.Generic;
using System.Globalization;

namespace PuzzleKingdoms.Logic.Analytics
{
    /// <summary>
    /// Values that change the game without a new build (PB-29, D-047): text keys and values, as an
    /// online config service hands them over. Empty means "use the assets". Numbers are read with a
    /// dot ("1.25") whatever the phone's language.
    /// </summary>
    public sealed class RemoteConfig
    {
        private readonly Dictionary<string, string> _values;

        public RemoteConfig(IDictionary<string, string> values = null)
        {
            _values = values == null ? new Dictionary<string, string>() : new Dictionary<string, string>(values);
        }

        public static RemoteConfig Empty { get; } = new RemoteConfig();

        public int Count => _values.Count;

        public IEnumerable<string> Keys => _values.Keys;

        /// <summary>
        /// Reads <c>key = value</c> lines (the local override file of development builds). Empty lines
        /// and <c>#</c> notes are skipped, spaces around key and value trimmed; a later line wins.
        /// </summary>
        public static RemoteConfig Parse(string text)
        {
            var values = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(text))
            {
                foreach (string raw in text.Replace("\r", "").Split('\n'))
                {
                    string line = raw.Trim();
                    int equals = line.IndexOf('=');
                    if (line.Length == 0 || line[0] == '#' || equals <= 0)
                    {
                        continue;
                    }
                    values[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
                }
            }
            return new RemoteConfig(values);
        }

        public bool TryGetString(string key, out string value) => _values.TryGetValue(key, out value);

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            return _values.TryGetValue(key, out string text)
                   && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = 0f;
            return _values.TryGetValue(key, out string text)
                   && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                   && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
