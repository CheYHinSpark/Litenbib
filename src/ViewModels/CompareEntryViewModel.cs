using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using Litenbib.Views;
using ShimSkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class CompareEntryViewModel(List<BibtexEntry> list = null!) : ViewModelBase
    {
        public List<BibtexEntry> Entries = list ?? [];

        public Dictionary<string, string> SelectedField = [];

        public BibtexEntry MergedEntry = new();

        public (string, string) SetField
        {
            set
            {
                if (value is not (string, string)) { return; }
                SelectedField[value.Item1] = value.Item2;
            }
        }

        [RelayCommand]
        private void Merge(object? sender)
        {
            if (sender is not CompareEntryView window || Entries.Count == 0) { return; }
            MergedEntry.EntryType = SelectedField.GetValueOrDefault("Entry Type", Entries[0].EntryType);
            MergedEntry.CitationKey = SelectedField.GetValueOrDefault("Citation Key", Entries[0].CitationKey);
            foreach (var field in SelectedField)
            {
                if (field.Key == "Entry Type" || field.Key == "Citation Key") { continue; }
                if (string.IsNullOrWhiteSpace(field.Value)) { continue; }
                MergedEntry.Fields[field.Key] = field.Value;
            }
            MergedEntry.UpdateBibtex();
            window.Close(true);
        }
    }
}
