using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VsDteCli
{
    internal static partial class Program
    {
        private sealed class CommandOptions
        {
            private readonly Dictionary<string, List<string>> values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            public bool JsonOutput
            {
                get { return GetBoolOrDefault("json", false); }
            }

            public static CommandOptions Parse(string[] args)
            {
                CommandOptions options = new CommandOptions();
                for (int i = 0; i < args.Length; i++)
                {
                    string current = args[i];
                    if (!current.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Unsupported argument: " + current);
                    }

                    string key = current.Substring(2);
                    string value = "true";
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        value = args[++i];
                    }

                    options.Add(key, value);
                }

                return options;
            }

            public string Get(string key)
            {
                List<string> list;
                return values.TryGetValue(key, out list) && list.Count > 0 ? list[list.Count - 1] : null;
            }

            public IEnumerable<string> GetAll(string key)
            {
                List<string> list;
                return values.TryGetValue(key, out list) ? list : Enumerable.Empty<string>();
            }

            public CommandOptions With(string key, string value)
            {
                CommandOptions copy = new CommandOptions();
                foreach (KeyValuePair<string, List<string>> pair in values)
                {
                    foreach (string item in pair.Value)
                    {
                        copy.Add(pair.Key, item);
                    }
                }

                copy.Add(key, value);
                return copy;
            }

            public int GetIntOrDefault(string key, int defaultValue)
            {
                string value = Get(key);
                if (string.IsNullOrWhiteSpace(value))
                {
                    return defaultValue;
                }

                int parsed;
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    throw new InvalidOperationException("Invalid integer for --" + key + ": " + value);
                }

                return parsed;
            }

            public bool GetBoolOrDefault(string key, bool defaultValue)
            {
                string value = Get(key);
                if (string.IsNullOrWhiteSpace(value))
                {
                    return defaultValue;
                }

                if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "no", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                throw new InvalidOperationException("Invalid boolean for --" + key + ": " + value);
            }

            private void Add(string key, string value)
            {
                List<string> list;
                if (!values.TryGetValue(key, out list))
                {
                    list = new List<string>();
                    values[key] = list;
                }

                list.Add(value);
            }
        }
    }
}
