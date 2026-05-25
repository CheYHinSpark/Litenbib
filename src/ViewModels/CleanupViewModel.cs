using CommunityToolkit.Mvvm.ComponentModel;
using Litenbib.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Litenbib.ViewModels
{
    public partial class CleanupRuleOptionViewModel : ViewModelBase
    {
        private readonly CleanupViewModel owner;

        public BibtexCleanupRuleDefinition Definition { get; }

        public string Id => Definition.Id;

        public string DisplayName => I18n.Get(Definition.DisplayNameKey);

        public string Description => I18n.Get(Definition.DescriptionKey);

        [ObservableProperty]
        private bool _isSelected;

        public CleanupRuleOptionViewModel()
            : this(null!, new BibtexCleanupRuleDefinition(
                BibtexCleanupRuleIds.Whitespace,
                "Cleanup.Rule.Whitespace",
                "Cleanup.Rule.Whitespace.Description",
                true))
        { }

        public CleanupRuleOptionViewModel(CleanupViewModel owner, BibtexCleanupRuleDefinition definition)
        {
            this.owner = owner;
            Definition = definition;
            _isSelected = definition.DefaultEnabled;
        }

        partial void OnIsSelectedChanged(bool value)
        {
            owner?.RefreshPreview();
        }
    }

    public class CleanupChangePreviewViewModel(EntryFieldChange change) : ViewModelBase
    {
        public string CitationKey => string.IsNullOrWhiteSpace(change.Entry.CitationKey)
            ? I18n.Get("Common.NoCitationKey")
            : change.Entry.CitationKey;

        public string FieldName => change.PropertyName;

        public string OldValue => FormatValue(change.OldValue);

        public string NewValue => FormatValue(change.NewValue);

        private static string FormatValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value;
        }
    }

    public partial class CleanupViewModel : ViewModelBase, ITaskDialogContentViewModel
    {
        private readonly IReadOnlyList<BibtexEntry> entries;

        public ObservableCollection<CleanupRuleOptionViewModel> RuleOptions { get; } = [];

        public ObservableCollection<CleanupChangePreviewViewModel> PreviewChanges { get; } = [];

        public string Title => I18n.Get("Cleanup.Title");

        public string Heading => I18n.Get("Cleanup.Heading");

        public string StatusMessage => I18n.Format("Cleanup.Status", entries.Count, PreviewChanges.Count);

        public bool HasChanges => PreviewChanges.Count > 0;

        public bool CanApply => HasChanges;

        public CleanupViewModel() : this([]) { }

        public CleanupViewModel(IReadOnlyList<BibtexEntry> entries)
        {
            this.entries = entries;
            foreach (var rule in BibtexBatchOperations.CleanupRules)
            {
                RuleOptions.Add(new CleanupRuleOptionViewModel(this, rule));
            }

            RefreshPreview();
        }

        public List<EntryFieldChange> CreateChanges()
        {
            return BibtexBatchOperations.CreateCleanupChanges(entries, GetSelectedRuleIds());
        }

        internal void RefreshPreview()
        {
            PreviewChanges.Clear();
            foreach (var change in CreateChanges())
            {
                PreviewChanges.Add(new CleanupChangePreviewViewModel(change));
            }

            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(HasChanges));
            OnPropertyChanged(nameof(CanApply));
        }

        private IEnumerable<string> GetSelectedRuleIds()
        {
            return RuleOptions
                .Where(option => option.IsSelected)
                .Select(option => option.Id);
        }
    }
}
