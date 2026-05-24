using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Litenbib.Models
{
    public sealed record BibtexCleanupRuleDefinition(
        string Id,
        string DisplayNameKey,
        string DescriptionKey,
        bool DefaultEnabled);

    public static class BibtexCleanupRuleIds
    {
        public const string Whitespace = "whitespace";
        public const string Doi = "doi";
        public const string UrlTrim = "url-trim";
        public const string LatexAmpersand = "latex-ampersand";
    }

    public static class BibtexBatchOperations
    {
        public static IReadOnlyList<BibtexCleanupRuleDefinition> CleanupRules { get; } =
        [
            new(
                BibtexCleanupRuleIds.Whitespace,
                "Cleanup.Rule.Whitespace",
                "Cleanup.Rule.Whitespace.Description",
                true),
            new(
                BibtexCleanupRuleIds.Doi,
                "Cleanup.Rule.Doi",
                "Cleanup.Rule.Doi.Description",
                true),
            new(
                BibtexCleanupRuleIds.UrlTrim,
                "Cleanup.Rule.UrlTrim",
                "Cleanup.Rule.UrlTrim.Description",
                true),
            new(
                BibtexCleanupRuleIds.LatexAmpersand,
                "Cleanup.Rule.LatexAmpersand",
                "Cleanup.Rule.LatexAmpersand.Description",
                true),
        ];

        public static List<EntryFieldChange> CreateCleanupChanges(IEnumerable<BibtexEntry> entries)
        {
            return CreateCleanupChanges(
                entries,
                CleanupRules.Where(rule => rule.DefaultEnabled).Select(rule => rule.Id));
        }

        public static List<EntryFieldChange> CreateCleanupChanges(
            IEnumerable<BibtexEntry> entries,
            IEnumerable<string> enabledRuleIds)
        {
            List<EntryFieldChange> changes = [];
            HashSet<string> enabledRules = new(enabledRuleIds, StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                changes.AddRange(CreateCleanupChanges(entry, enabledRules));
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

        private static List<EntryFieldChange> CreateCleanupChanges(BibtexEntry entry, ISet<string> enabledRules)
        {
            List<EntryFieldChange> changes = [];
            var keys = entry.Fields.Keys.ToList();
            foreach (var key in keys)
            {
                var oldValue = entry.Fields[key];
                var value = oldValue;
                if (enabledRules.Contains(BibtexCleanupRuleIds.Whitespace))
                {
                    value = value.Replace("\r", " ").Replace("\n", " ").Trim();
                    while (value.Contains("  "))
                    {
                        value = value.Replace("  ", " ");
                    }
                }

                if (enabledRules.Contains(BibtexCleanupRuleIds.Doi)
                    && key.Equals("doi", StringComparison.OrdinalIgnoreCase))
                {
                    value = BibtexDiagnostics.NormalizeDoi(value);
                }
                if (enabledRules.Contains(BibtexCleanupRuleIds.UrlTrim)
                    && key.Equals("url", StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Trim();
                }
                if (enabledRules.Contains(BibtexCleanupRuleIds.LatexAmpersand)
                    && BibtexAmpersand.ShouldCleanupField(key))
                {
                    value = BibtexAmpersand.Normalize(value);
                }

                string? newValue = enabledRules.Contains(BibtexCleanupRuleIds.Whitespace) && string.IsNullOrWhiteSpace(value)
                    ? null
                    : value;
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
