using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PuzzleKingdoms.Logic.Analytics
{
    /// <summary>
    /// One thing that happened in the game, for play statistics (PB-29): a snake_case name and a few
    /// values (text, whole numbers, decimals, yes/no) in the order they were added. No personal data:
    /// no names, no device ids; the service adds its own anonymous install id.
    /// </summary>
    public sealed class AnalyticsEvent
    {
        private readonly List<KeyValuePair<string, object>> _values = new List<KeyValuePair<string, object>>();

        public AnalyticsEvent(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("an event needs a name", nameof(name));
            }
            Name = name;
        }

        public string Name { get; }

        public IReadOnlyList<KeyValuePair<string, object>> Values => _values;

        public AnalyticsEvent With(string key, string value) => Add(key, value ?? "");

        public AnalyticsEvent With(string key, int value) => Add(key, value);

        /// <summary>Decimals are rounded to one place (seconds, multipliers).</summary>
        public AnalyticsEvent With(string key, float value) => Add(key, (float)Math.Round(value, 1, MidpointRounding.AwayFromZero));

        public AnalyticsEvent With(string key, bool value) => Add(key, value);

        /// <summary>The value of <paramref name="key"/>, or null.</summary>
        public object this[string key]
        {
            get
            {
                foreach (KeyValuePair<string, object> pair in _values)
                {
                    if (pair.Key == key)
                    {
                        return pair.Value;
                    }
                }
                return null;
            }
        }

        /// <summary>One JSON object on one line: <c>{"event":"stage_won","at":"…","stage":"stage_1_1",…}</c>.</summary>
        public string ToJsonLine(string utcTime)
        {
            var json = new StringBuilder("{");
            AppendString(json, "event");
            json.Append(':');
            AppendString(json, Name);
            if (!string.IsNullOrEmpty(utcTime))
            {
                json.Append(',');
                AppendString(json, "at");
                json.Append(':');
                AppendString(json, utcTime);
            }
            foreach (KeyValuePair<string, object> pair in _values)
            {
                json.Append(',');
                AppendString(json, pair.Key);
                json.Append(':');
                switch (pair.Value)
                {
                    case bool flag:
                        json.Append(flag ? "true" : "false");
                        break;
                    case int whole:
                        json.Append(whole.ToString(CultureInfo.InvariantCulture));
                        break;
                    case float number:
                        json.Append(number.ToString("0.0", CultureInfo.InvariantCulture));
                        break;
                    default:
                        AppendString(json, pair.Value as string ?? "");
                        break;
                }
            }
            return json.Append('}').ToString();
        }

        private AnalyticsEvent Add(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("a value needs a key", nameof(key));
            }
            _values.Add(new KeyValuePair<string, object>(key, value));
            return this;
        }

        private static void AppendString(StringBuilder json, string text)
        {
            json.Append('"');
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\r': json.Append("\\r"); break;
                    case '\t': json.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            json.Append(c);
                        }
                        break;
                }
            }
            json.Append('"');
        }
    }
}
