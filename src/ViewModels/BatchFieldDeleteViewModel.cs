using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Litenbib.ViewModels
{
    public partial class BatchFieldDeleteRowViewModel(BatchFieldDeleteViewModel owner, string fieldName) : ViewModelBase
    {
        public static IReadOnlyList<string> SupportedFields => BatchFieldDeleteViewModel.SupportedFields;

        [ObservableProperty]
        private string _fieldName = fieldName;

        public bool CanRemove => owner.FieldRows.Count > 1;

        [RelayCommand(CanExecute = nameof(CanRemove))]
        private void Remove()
        { owner.RemoveFieldRow(this); }

        public void NotifyCanRemoveChanged()
        {
            OnPropertyChanged(nameof(CanRemove));
            RemoveCommand.NotifyCanExecuteChanged();
        }
    }

    public partial class BatchFieldDeleteViewModel : ViewModelBase
    {
        public static IReadOnlyList<string> SupportedFields { get; } =
        [
            "Author",
            "Title",
            "Year",
            "Journal",
            "Booktitle",
            "Publisher",
            "DOI",
            "Url",
            "Keywords",
            "Note",
            "Comment",
            "Abstract",
            "Address",
            "Annote",
            "Chapter",
            "Crossref",
            "Edition",
            "Editor",
            "File",
            "Howpublished",
            "Institution",
            "ISBN",
            "ISSN",
            "Key",
            "Month",
            "Number",
            "Organization",
            "Pages",
            "School",
            "Series",
            "Type",
            "Volume",
        ];

        public ObservableCollection<BatchFieldDeleteRowViewModel> FieldRows { get; } = [];

        [ObservableProperty]
        private bool _keepSelectedFieldsOnly;

        public bool DeleteSelectedFields
        {
            get => !KeepSelectedFieldsOnly;
            set { if (value) { KeepSelectedFieldsOnly = false; } }
        }

        public BatchFieldDeleteViewModel()
        { FieldRows.Add(new BatchFieldDeleteRowViewModel(this, "Keywords")); }

        partial void OnKeepSelectedFieldsOnlyChanged(bool value)
        { OnPropertyChanged(nameof(DeleteSelectedFields)); }

        [RelayCommand]
        private void AddField()
        {
            HashSet<string> usedFields = new(GetSelectedFieldNames(), System.StringComparer.OrdinalIgnoreCase);
            string fieldName = SupportedFields.FirstOrDefault(field => !usedFields.Contains(field)) ?? SupportedFields[0];
            FieldRows.Add(new BatchFieldDeleteRowViewModel(this, fieldName));
            NotifyRowRemoveStateChanged();
        }

        internal void RemoveFieldRow(BatchFieldDeleteRowViewModel row)
        {
            if (FieldRows.Count <= 1) { return; }

            FieldRows.Remove(row);
            NotifyRowRemoveStateChanged();
        }

        public List<string> GetSelectedFieldNames()
        {
            return FieldRows
                .Select(row => row.FieldName)
                .Where(field => !string.IsNullOrWhiteSpace(field))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public List<EntryFieldChange> CreateChanges(BibtexEntry entry)
        {
            List<EntryFieldChange> changes = [];
            if (entry == null) { return changes; }

            List<string> selectedFields = GetSelectedFieldNames();
            if (selectedFields.Count == 0) { return changes; }

            if (KeepSelectedFieldsOnly)
            {
                HashSet<string> fieldsToKeep = new(selectedFields, System.StringComparer.OrdinalIgnoreCase);
                foreach (var key in entry.Fields.Keys.ToList())
                {
                    if (fieldsToKeep.Contains(key)) { continue; }

                    changes.Add(new EntryFieldChange(
                        entry,
                        GetPropertyNameForField(key),
                        entry.Fields[key],
                        null));
                }
                return changes;
            }

            foreach (var field in selectedFields)
            {
                if (!entry.Fields.TryGetValue(field, out string? oldValue))
                { continue; }

                changes.Add(new EntryFieldChange(
                    entry,
                    GetPropertyNameForField(field),
                    oldValue,
                    null));
            }

            return changes;
        }

        private void NotifyRowRemoveStateChanged()
        {
            foreach (var row in FieldRows)
            { row.NotifyCanRemoveChanged(); }
        }

        private static string GetPropertyNameForField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            { return fieldName; }

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
