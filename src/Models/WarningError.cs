using Avalonia.Input.TextInput;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    /// <summary>
    /// 警告与错误类
    /// </summary>
    public enum WarningErrorClass
    {
        MissingType = 1,
        MissingCitationKey = 2,
        SameCitationKey = 3,
        MissingRequiredField = -1,
        MissingNecessaryField = -2
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
            Dictionary<string, List<BibtexEntry>> keysCount = [];
            IList<WarningError> result = [];
            int _error = -1;
            await Task.Run(() =>
            {
                foreach (BibtexEntry entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.CitationKey))
                    {
                        result.Add(new WarningError([entry], WarningErrorClass.MissingCitationKey, "citationKey"));
                        _error = 1;
                        continue;
                    }

                    if (keysCount.TryGetValue(entry.CitationKey, out List<BibtexEntry>? value))
                    {
                        value.Add(entry);
                    }
                    else
                    {
                        keysCount[entry.CitationKey] = [entry];
                    }

                    if (string.IsNullOrWhiteSpace(entry.Type))
                    {
                        result.Add(new WarningError([entry], WarningErrorClass.MissingType, "type"));
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

                foreach (var kv in keysCount)
                {
                    if (kv.Value.Count > 1)
                    {
                        result.Add(new WarningError(kv.Value, WarningErrorClass.SameCitationKey, kv.Key));
                        _error = 1;
                    }
                }
            });
            return (result, _error);
        }
    }
}
