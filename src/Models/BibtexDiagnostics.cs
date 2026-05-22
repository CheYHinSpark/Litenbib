using System;
using System.Collections.Generic;
using System.Linq;

namespace Litenbib.Models
{
    public enum DuplicateKind
    {
        CitationKey,
        Doi,
        TitleYear,
        SameTitle
    }

    public sealed class DuplicateGroup(List<BibtexEntry> entries, DuplicateKind kind, string token)
    {
        public List<BibtexEntry> Entries { get; } = entries;
        public DuplicateKind Kind { get; } = kind;
        public string Token { get; } = token;
    }

    public static class BibtexDiagnostics
    {
        public static IEnumerable<DuplicateGroup> FindDuplicates(IEnumerable<BibtexEntry> entries)
        {
            var list = entries.Where(e => e != null).ToList();

            foreach (var group in GroupByToken(list, e => e.CitationKey, DuplicateKind.CitationKey))
                yield return group;
            foreach (var group in GroupByToken(list, e => NormalizeDoi(e.DOI), DuplicateKind.Doi))
                yield return group;

            var titleYearGroups = GroupByToken(list, e => NormalizeTitleYear(e.Title, e.Year), DuplicateKind.TitleYear)
                .ToList();
            foreach (var group in titleYearGroups)
                yield return group;

            var titleYearEntrySets = titleYearGroups
                .Select(group => GetEntrySetKey(list, group.Entries))
                .ToHashSet(StringComparer.Ordinal);
            foreach (var group in GroupSameTitles(list, titleYearEntrySets))
                yield return group;
        }

        public static string NormalizeDoi(string? doi)
        {
            if (string.IsNullOrWhiteSpace(doi)) return string.Empty;
            return doi.Trim().Replace("https://doi.org/", "", StringComparison.OrdinalIgnoreCase)
                .Replace("http://doi.org/", "", StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
        }

        public static string NormalizeTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            var chars = title.ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray();
            return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        public static string NormalizeTitleYear(string? title, string? year)
        {
            var normalizedTitle = NormalizeTitle(title);
            var normalizedYear = string.IsNullOrWhiteSpace(year) ? string.Empty : year.Trim();
            if (string.IsNullOrWhiteSpace(normalizedTitle) || string.IsNullOrWhiteSpace(normalizedYear)) return string.Empty;
            return $"{normalizedTitle}::{normalizedYear}";
        }

        private static IEnumerable<DuplicateGroup> GroupByToken(List<BibtexEntry> entries, Func<BibtexEntry, string> selector, DuplicateKind kind)
        {
            foreach (var group in entries
                .Select(e => (Entry: e, Token: selector(e)))
                .Where(x => !string.IsNullOrWhiteSpace(x.Token))
                .GroupBy(x => x.Token, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1))
            {
                yield return new DuplicateGroup(group.Select(x => x.Entry).ToList(), kind, group.Key);
            }
        }

        private static IEnumerable<DuplicateGroup> GroupSameTitles(
            List<BibtexEntry> entries,
            HashSet<string> suppressedEntrySets)
        {
            foreach (var group in GroupByToken(entries, e => NormalizeTitle(e.Title), DuplicateKind.SameTitle))
            {
                if (suppressedEntrySets.Contains(GetEntrySetKey(entries, group.Entries)))
                {
                    continue;
                }

                yield return group;
            }
        }

        private static string GetEntrySetKey(List<BibtexEntry> entries, IEnumerable<BibtexEntry> group)
        {
            return string.Join(',', group
                .Select(entry => entries.IndexOf(entry))
                .Where(index => index >= 0)
                .OrderBy(index => index));
        }
    }
}
