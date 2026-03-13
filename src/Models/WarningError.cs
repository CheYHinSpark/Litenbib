using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    /// <summary>
    /// 警告与错误类
    /// </summary>
    public enum WarningErrorClass
    {
        /// <summary> 错误：缺少Type </summary>
        MissingEntryType = 1,
        /// <summary> 错误：缺少Citation </summary>
        MissingCitationKey = 2,
        /// <summary> 错误：相同Key </summary>
        SameCitationKey = 3,
        /// <summary> 警告：缺少要求字段如Author、Title、Year </summary>
        MissingRequiredField = -1,
        /// <summary> 警告：缺少重要字段如 </summary>
        MissingNecessaryField = -2,
        /// <summary> 错误：相同 DOI </summary>
        SameDoi = 4,
        /// <summary> 警告：标题和年份重复 </summary>
        SameTitleYear = -3,
        /// <summary> 警告：标题可能重复 </summary>
        SimilarTitle = -4
    }

    /// <summary>
    /// 警告与错误类
    /// </summary>
    public class WarningError(List<BibtexEntry> _sourceEntries, WarningErrorClass _class, string _field = "")
    {
        public List<BibtexEntry> SourceEntries = _sourceEntries;
        public WarningErrorClass Class = _class;
        public string FieldName = _field;

        public int WarningClass { get => (int)Class; }

        public string HintString
        {
            get
            {
                if (Class == WarningErrorClass.SameCitationKey)
                {
                    return $"{SourceEntries.Count} entries have the same CitationKey {FieldName}";
                }
                if (Class == WarningErrorClass.SameDoi)
                {
                    return $"{SourceEntries.Count} entries have the same DOI {FieldName}";
                }
                if (Class == WarningErrorClass.SameTitleYear)
                {
                    return $"{SourceEntries.Count} entries share the same title and year";
                }
                if (Class == WarningErrorClass.SimilarTitle)
                {
                    return $"{SourceEntries.Count} entries have very similar titles";
                }
                return $"1 entry missing {FieldName}";
            }
        }
    }

    /// <summary>
    /// 警告与错误检查器
    /// </summary>
    public static class WarningErrorChecker
    {
        public static async Task<(IList<WarningError>, int)> CheckBibtex(IEnumerable<BibtexEntry> entries)
        {
            IList<WarningError> result = [];
            int _error = -1;
            await Task.Run(() =>
            {
                var entryList = entries.ToList();
                foreach (BibtexEntry entry in entryList)
                {
                    if (string.IsNullOrWhiteSpace(entry.CitationKey))
                    {
                        result.Add(new WarningError([entry], WarningErrorClass.MissingCitationKey, "citationKey"));
                        _error = 1;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entry.EntryType))
                    {
                        result.Add(new WarningError([entry], WarningErrorClass.MissingEntryType, "entry type"));
                        _error = 1;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entry.Title))
                    {
                        result.Add(new WarningError([entry], WarningErrorClass.MissingNecessaryField, "title"));
                    }
                    if (string.IsNullOrWhiteSpace(entry.Year))
                    {
                        result.Add(new WarningError([entry], WarningErrorClass.MissingNecessaryField, "year"));
                    }
                    if (string.IsNullOrWhiteSpace(entry.Author) && string.IsNullOrWhiteSpace(entry.Editor))
                    {
                        result.Add(new WarningError([entry], WarningErrorClass.MissingNecessaryField, "author or editor"));
                    }
                }

                foreach (var duplicate in BibtexDiagnostics.FindDuplicates(entryList))
                {
                    switch (duplicate.Kind)
                    {
                        case DuplicateKind.CitationKey:
                            result.Add(new WarningError(duplicate.Entries, WarningErrorClass.SameCitationKey, duplicate.Token));
                            _error = 1;
                            break;
                        case DuplicateKind.Doi:
                            result.Add(new WarningError(duplicate.Entries, WarningErrorClass.SameDoi, duplicate.Token));
                            _error = 1;
                            break;
                        case DuplicateKind.TitleYear:
                            result.Add(new WarningError(duplicate.Entries, WarningErrorClass.SameTitleYear, duplicate.Token));
                            break;
                        case DuplicateKind.SimilarTitle:
                            result.Add(new WarningError(duplicate.Entries, WarningErrorClass.SimilarTitle, duplicate.Token));
                            break;
                    }
                }
            });
            return (result, _error);
        }
    }
}
