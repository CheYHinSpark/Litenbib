using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class VenueNameNormalizationItemViewModel(BibtexEntry entry) : ViewModelBase
    {
        public BibtexEntry Entry { get; } = entry;

        public string CitationKey => string.IsNullOrWhiteSpace(Entry.CitationKey)
            ? I18n.Get("Common.NoCitationKey")
            : Entry.CitationKey;

        public string EntryTitle => string.IsNullOrWhiteSpace(Entry.Title)
            ? I18n.Get("Common.Untitled").ToLowerInvariant()
            : Entry.Title;

        public string FieldName { get; } = !string.IsNullOrWhiteSpace(entry.Journal)
            ? nameof(BibtexEntry.Journal)
            : !string.IsNullOrWhiteSpace(entry.Booktitle)
                ? nameof(BibtexEntry.Booktitle)
                : string.Empty;

        public string OriginalValue { get; } = !string.IsNullOrWhiteSpace(entry.Journal)
            ? entry.Journal
            : !string.IsNullOrWhiteSpace(entry.Booktitle)
                ? entry.Booktitle
                : string.Empty;

        public bool HasVenue => !string.IsNullOrWhiteSpace(FieldName)
            && !string.IsNullOrWhiteSpace(OriginalValue);

        [ObservableProperty]
        private string _suggestedValue = !string.IsNullOrWhiteSpace(entry.Journal)
            ? entry.Journal
            : !string.IsNullOrWhiteSpace(entry.Booktitle)
                ? entry.Booktitle
                : string.Empty;

        [RelayCommand]
        private void ResetSuggestion()
        { SuggestedValue = OriginalValue; }

        public EntryFieldChange? CreateChange()
        {
            string newValue = SuggestedValue.Trim();
            if (!HasVenue
                || string.IsNullOrWhiteSpace(newValue)
                || string.Equals(OriginalValue, newValue, StringComparison.Ordinal))
            { return null; }

            return new EntryFieldChange(Entry, FieldName, OriginalValue, newValue);
        }
    }

    public partial class VenueNameNormalizationViewModel : ViewModelBase
    {
        public ObservableCollection<VenueNameNormalizationItemViewModel> Items { get; }

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public VenueNameNormalizationViewModel() : this([]) { }

        public VenueNameNormalizationViewModel(IEnumerable<BibtexEntry> entries)
        {
            Items = new ObservableCollection<VenueNameNormalizationItemViewModel>(
                entries.Select(entry => new VenueNameNormalizationItemViewModel(entry)));
            StatusMessage = Items.Count == 0
                ? I18n.Get("VenueNormalize.Status.NoEntriesSelected")
                : I18n.Format("VenueNormalize.Status.SelectedEntries", Items.Count);
        }

        [RelayCommand]
        private async Task ExpandVenues()
        { await NormalizeVenues(VenueNameNormalizationMode.Expand); }

        [RelayCommand]
        private async Task AbbreviateVenues()
        { await NormalizeVenues(VenueNameNormalizationMode.Abbreviate); }

        public List<EntryFieldChange> CreateChanges()
        {
            return Items
                .Select(item => item.CreateChange())
                .Where(change => change != null)
                .Select(change => change!.Value)
                .ToList();
        }

        private async Task NormalizeVenues(VenueNameNormalizationMode mode)
        {
            if (IsBusy) { return; }

            List<VenueNameNormalizationItemViewModel> targetItems = Items
                .Where(item => item.HasVenue)
                .ToList();
            if (targetItems.Count == 0)
            {
                StatusMessage = I18n.Get("VenueNormalize.Status.NoSelectedVenues");
                return;
            }

            List<VenueAbbreviationMapping> mappings;
            try { mappings = VenueAbbreviationMappings.Load(); }
            catch (Exception ex)
            {
                StatusMessage = I18n.Format("VenueNormalize.Status.CouldNotReadMappings", ex.Message);
                return;
            }

            if (mappings.Count == 0)
            {
                StatusMessage = I18n.Get("VenueNormalize.Status.NoMappings");
                return;
            }

            IsBusy = true;
            StatusMessage = mode == VenueNameNormalizationMode.Expand
                ? I18n.Format("VenueNormalize.Status.Expanding", targetItems.Count)
                : I18n.Format("VenueNormalize.Status.Abbreviating", targetItems.Count);

            VenueNameNormalizationResult result = await AiVenueNameNormalizer.NormalizeAsync(
                mode,
                mappings,
                targetItems.Select(item => item.OriginalValue).ToList());

            IsBusy = false;
            if (!result.Success)
            {
                StatusMessage = result.ErrorMessage;
                NotificationCenter.Error(result.ErrorMessage);
                return;
            }

            if (result.Values.Count != targetItems.Count)
            {
                StatusMessage = I18n.Format("VenueNormalize.Status.AiReturnedWrongCount", result.Values.Count, targetItems.Count);
                return;
            }

            for (int index = 0; index < targetItems.Count; index++)
            { targetItems[index].SuggestedValue = result.Values[index]; }

            StatusMessage = I18n.Format("VenueNormalize.Status.SuggestionsReady", targetItems.Count);
        }
    }
}
