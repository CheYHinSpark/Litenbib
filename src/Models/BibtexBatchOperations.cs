using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Litenbib.Models
{
    public static class BibtexBatchOperations
    {
        public static List<EntryFieldChange> CreateCleanupChanges(IEnumerable<BibtexEntry> entries)
        {
            List<EntryFieldChange> changes = [];
            foreach (var entry in entries)
            {
                changes.AddRange(CreateCleanupChanges(entry));
            }

            return changes;
        }

        public static List<EntryFieldChange> CreateCitationKeyChanges(
            IReadOnlyList<BibtexEntry> selectedEntries,
            IEnumerable<BibtexEntry> allEntries)
        {
            HashSet<BibtexEntry> selectedEntrySet = new(selectedEntries);
            HashSet<string> occupiedKeys = new(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in allEntries)
            {
                if (!selectedEntrySet.Contains(entry) && !string.IsNullOrWhiteSpace(entry.CitationKey))
                {
                    occupiedKeys.Add(entry.CitationKey);
                }
            }

            List<EntryFieldChange> changes = [];
            foreach (var entry in selectedEntries)
            {
                string baseKey = entry.BuildCitationKey();
                string newKey = CreateUniqueCitationKey(baseKey, occupiedKeys);
                if (string.IsNullOrWhiteSpace(newKey)
                    || string.Equals(entry.CitationKey, newKey, StringComparison.Ordinal))
                {
                    continue;
                }

                changes.Add(new EntryFieldChange(
                    entry,
                    nameof(BibtexEntry.CitationKey),
                    entry.CitationKey,
                    newKey));
            }

            return changes;
        }

        private static List<EntryFieldChange> CreateCleanupChanges(BibtexEntry entry)
        {
            List<EntryFieldChange> changes = [];
            var keys = entry.Fields.Keys.ToList();
            foreach (var key in keys)
            {
                var oldValue = entry.Fields[key];
                var value = oldValue;
                value = value.Replace("\r", " ").Replace("\n", " ").Trim();
                while (value.Contains("  "))
                {
                    value = value.Replace("  ", " ");
                }

                if (key.Equals("doi", StringComparison.OrdinalIgnoreCase))
                {
                    value = BibtexDiagnostics.NormalizeDoi(value);
                }
                if (key.Equals("url", StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Trim();
                }

                string? newValue = string.IsNullOrWhiteSpace(value) ? null : value;
                if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                {
                    continue;
                }

                string propertyName = GetPropertyNameForField(key);
                changes.Add(new EntryFieldChange(entry, propertyName, oldValue, newValue));
            }

            return changes;
        }

        private static string CreateUniqueCitationKey(string baseKey, HashSet<string> occupiedKeys)
        {
            if (string.IsNullOrWhiteSpace(baseKey))
            {
                return string.Empty;
            }

            if (occupiedKeys.Add(baseKey))
            {
                return baseKey;
            }

            for (int index = 1; index < int.MaxValue; index++)
            {
                string candidate = baseKey + FormatDuplicateSuffix(AppSettingsState.Current.CitationKeyDuplicateSuffix, index);
                if (occupiedKeys.Add(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static string FormatDuplicateSuffix(string suffixRule, int index)
        {
            if (string.IsNullOrWhiteSpace(suffixRule))
            {
                suffixRule = AppSettings.DefaultCitationKeyDuplicateSuffix;
            }

            string prefix = suffixRule[..^1];
            return suffixRule[^1] == '1'
                ? prefix + index.ToString()
                : prefix + ToAlphabeticSuffix(index);
        }

        private static string ToAlphabeticSuffix(int index)
        {
            StringBuilder builder = new();
            int value = index;
            while (value > 0)
            {
                value--;
                builder.Insert(0, (char)('a' + value % 26));
                value /= 26;
            }

            return builder.ToString();
        }

        public static string GetPropertyNameForField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return fieldName;
            }

            return fieldName.ToLowerInvariant() switch
            {
                "doi" => "DOI",
                "isbn" => "ISBN",
                "issn" => "ISSN",
                "url" => "Url",
                _ => char.ToUpperInvariant(fieldName[0]) + fieldName[1..]
            };
        }
    }
}
