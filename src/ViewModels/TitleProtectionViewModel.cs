using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Litenbib.ViewModels
{
    public partial class TitleProtectionItemViewModel : ViewModelBase
    {
        private readonly TitleProtectionViewModel? owner;

        public BibtexEntry Entry { get; }

        public string CitationKey => string.IsNullOrWhiteSpace(Entry.CitationKey)
            ? I18n.Get("Common.NoCitationKey")
            : Entry.CitationKey;

        public string OriginalTitle => Entry.Title;

        public bool HasTitle => !string.IsNullOrWhiteSpace(OriginalTitle);

        [ObservableProperty]
        private string _suggestedTitle;

        public TitleProtectionItemViewModel()
            : this(null, new BibtexEntry())
        { }

        public TitleProtectionItemViewModel(TitleProtectionViewModel? owner, BibtexEntry entry)
        {
            this.owner = owner;
            Entry = entry;
            _suggestedTitle = entry.Title;
        }

        partial void OnSuggestedTitleChanged(string value)
        {
            owner?.RefreshState();
        }

        [RelayCommand]
        private void ResetSuggestion()
        {
            SuggestedTitle = OriginalTitle;
        }

        public EntryFieldChange? CreateChange()
        {
            string newValue = SuggestedTitle.Trim();
            if (!HasTitle
                || string.IsNullOrWhiteSpace(newValue)
                || string.Equals(OriginalTitle, newValue, StringComparison.Ordinal))
            {
                return null;
            }

            return new EntryFieldChange(Entry, nameof(BibtexEntry.Title), OriginalTitle, newValue);
        }
    }

    public partial class TitleProtectionViewModel : ViewModelBase, ITaskDialogContentViewModel
    {
        private readonly IReadOnlyList<BibtexEntry> entries;

        public ObservableCollection<TitleProtectionItemViewModel> Items { get; } = [];

        public string Title => I18n.Get("TitleProtection.Title");

        public string Heading => I18n.Get("TitleProtection.Heading");

        public string StatusMessage => I18n.Format("TitleProtection.Status", entries.Count, CreateChanges().Count);

        public bool HasChanges => CreateChanges().Count > 0;

        public bool CanApply => HasChanges;

        [ObservableProperty]
        private bool _protectTerms = true;

        [ObservableProperty]
        private bool _protectTitleCase;

        public TitleProtectionViewModel() : this([]) { }

        public TitleProtectionViewModel(IReadOnlyList<BibtexEntry> entries)
        {
            this.entries = entries;
            foreach (var entry in entries)
            {
                Items.Add(new TitleProtectionItemViewModel(this, entry));
            }

            RefreshSuggestions();
        }

        partial void OnProtectTermsChanged(bool value)
        {
            RefreshSuggestions();
        }

        partial void OnProtectTitleCaseChanged(bool value)
        {
            RefreshSuggestions();
        }

        public List<EntryFieldChange> CreateChanges()
        {
            return Items
                .Select(item => item.CreateChange())
                .Where(change => change != null)
                .Select(change => change!.Value)
                .ToList();
        }

        internal void RefreshState()
        {
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(HasChanges));
            OnPropertyChanged(nameof(CanApply));
        }

        private void RefreshSuggestions()
        {
            IReadOnlyList<TitleProtectionTerm> terms = [];
            if (ProtectTerms)
            {
                try
                {
                    terms = TitleProtectionTerms.Load();
                }
                catch (Exception ex)
                {
                    NotificationCenter.Error(I18n.Format("Message.CouldNotReadTitleProtectionTerms", ex.Message));
                }
            }

            foreach (var item in Items)
            {
                item.SuggestedTitle = item.HasTitle
                    ? BibtexTitleProtection.Protect(
                        item.OriginalTitle,
                        terms,
                        ProtectTerms,
                        ProtectTitleCase)
                    : item.OriginalTitle;
            }

            RefreshState();
        }

        [RelayCommand]
        private void OpenTermsFile()
        {
            try
            {
                TitleProtectionTerms.EnsureFileExists();
                UriProcessor.StartProcess(AppPaths.TitleProtectionTermsPath);
            }
            catch (Exception ex)
            {
                NotificationCenter.Error(I18n.Format("Message.CouldNotOpenTitleProtectionTerms", ex.Message));
            }
        }
    }
}
