using System;
using System.Collections.Generic;

namespace Echo.NativeGame.GameFlow.Results
{
    // Select only text already recorded by Narrative. Category order is deterministic.
    internal static class KeyBehaviorSelector
    {
        public static IReadOnlyList<string> Select(IReadOnlyList<NativeBehaviorRecord> records)
        {
            var buckets = new List<string>[4];
            for (int i = 0; i < buckets.Length; i++) buckets[i] = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (records == null) records = Array.Empty<NativeBehaviorRecord>();
            foreach (NativeBehaviorRecord record in records)
            {
                string value = record.Text.Trim();
                if (value.Length == 0 || !seen.Add(value)) continue;
                int category = Category(record.Key);
                if (category >= 0) buckets[category].Add(value);
            }

            var selected = new List<string>(3);
            // Give each preferred behavior family one place before taking a second
            // support or memory choice. Remaining places use original record order.
            for (int i = 0; i < 3; i++)
                if (buckets[i].Count != 0) selected.Add(buckets[i][0]);
            if (selected.Count == 3) return selected.AsReadOnly();
            foreach (var bucket in buckets)
                foreach (string value in bucket)
                {
                    if (selected.Contains(value)) continue;
                    selected.Add(value);
                    if (selected.Count == 3) return selected.AsReadOnly();
                }
            return selected.AsReadOnly();
        }

        static int Category(string key)
        {
            key = key ?? string.Empty;
            if (key.StartsWith("support_", StringComparison.Ordinal) || key == "north_pulse") return 0;
            if (key == "bypass") return 1;
            if (key == "preserve" || key == "rewrite") return 2;
            if (key == "start" || key == "combat") return -1;
            return 3;
        }
    }
}
