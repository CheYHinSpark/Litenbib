using CommunityToolkit.Mvvm.ComponentModel;
using Litenbib.Models;
using System;

namespace Litenbib.ViewModels
{
    public partial class BatchFieldEditViewModel : ViewModelBase
    {
        public static string[] SupportedFields =>
        [
            "Keywords", "Note", "Comment", "Url", "DOI", "Publisher", "Journal", "Booktitle", "Year"
        ];

        [ObservableProperty]
        private string _selectedField = "Keywords";

        [ObservableProperty]
        private string _fieldValue = string.Empty;

        [ObservableProperty]
        private bool _appendMode = true;

        [ObservableProperty]
        private bool _removeField;

        public EntryFieldChange? CreateChange(BibtexEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(SelectedField)) return null;

            string? oldValue = entry.Fields.TryGetValue(SelectedField, out string? existingValue)
                ? existingValue
                : null;

            if (RemoveField)
            {
                return string.Equals(oldValue, null, StringComparison.Ordinal)
                    ? null
                    : new EntryFieldChange(entry, SelectedField, oldValue, null);
            }

            var current = SelectedField switch
            {
                "Keywords" => entry.Keywords,
                "Note" => entry.Note,
                "Comment" => entry.Comment,
                "Url" => entry.Url,
                "DOI" => entry.DOI,
                "Publisher" => entry.Publisher,
                "Journal" => entry.Journal,
                "Booktitle" => entry.Booktitle,
                "Year" => entry.Year,
                _ => string.Empty,
            };

            string nextValue = AppendMode && !string.IsNullOrWhiteSpace(current)
                ? $"{current}; {FieldValue.Trim()}"
                : FieldValue.Trim();

            string? newValue = string.IsNullOrWhiteSpace(nextValue) ? null : nextValue;
            return string.Equals(oldValue, newValue, StringComparison.Ordinal)
                ? null
                : new EntryFieldChange(entry, SelectedField, oldValue, newValue);
        }
    }
}
