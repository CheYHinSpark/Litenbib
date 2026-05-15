using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class AddEntryCandidateViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isSelected = true;

        public string Source { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CitationKey { get; set; } = string.Empty;
        public string BibTeX { get; set; } = string.Empty;
    }

    public partial class AddEntryViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _doiText;
        [ObservableProperty]
        private string _bibtexText;
        [ObservableProperty]
        private string _hintText;

        public ObservableCollection<AddEntryCandidateViewModel> Candidates { get; } = [];

        public AddEntryViewModel()
        {
            DoiText = "";
            BibtexText = "";
            HintText = "";
        }

        partial void OnBibtexTextChanged(string value)
        {
            if (Candidates.Count == 0)
            {
                return;
            }

            string selectedBibtex = string.Join("\n\n", Candidates.Where(c => c.IsSelected).Select(c => c.BibTeX));
            if (value != selectedBibtex)
            {
                Candidates.Clear();
            }
        }

        [RelayCommand]
        private async Task ParseDoi()
        {
            var inputs = DoiText.Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            if (inputs.Count == 0)
            {
                HintText = I18n.Get("AddEntry.Hint.InputRequired");
                return;
            }

            Candidates.Clear();
            List<string> hints = [];
            HashSet<string> seen = new();
            NotificationCenter.Info(I18n.Format("Message.SearchingBibliographyQueries", inputs.Count));

            foreach (string input in inputs)
            {
                var result = await LinkResolver.ResolveAsync(input, 5);
                if (result.Success)
                {
                    int added = 0;
                    foreach (var candidate in result.Candidates)
                    {
                        if (string.IsNullOrWhiteSpace(candidate.BibTeX) || !seen.Add(candidate.BibTeX))
                        {
                            continue;
                        }

                        Candidates.Add(new AddEntryCandidateViewModel
                        {
                            Source = candidate.Source,
                            Query = candidate.Query,
                            Title = string.IsNullOrWhiteSpace(candidate.Title) ? I18n.Get("Common.Untitled") : candidate.Title,
                            CitationKey = candidate.Entry?.CitationKey ?? string.Empty,
                            BibTeX = candidate.BibTeX,
                            IsSelected = true,
                        });
                        added++;
                    }
                    hints.Add(I18n.Format("AddEntry.LookupCandidateCount", input, added));
                }
                else
                {
                    hints.Add(I18n.Format("AddEntry.LookupNotFound", input));
                }
            }

            UpdateSelectedBibtex();
            HintText = Candidates.Count == 0
                ? string.Join(" | ", hints)
                : I18n.Format("AddEntry.Hint.CandidatesReady", Candidates.Count);
            if (Candidates.Count == 0)
            {
                NotificationCenter.Info(I18n.Get("Message.NoBibliographyCandidatesFound"));
            }
            else
            {
                NotificationCenter.Info(I18n.Format("Message.FoundBibliographyCandidates", Candidates.Count));
            }
        }

        [RelayCommand]
        private async Task GenerateBibtexWithAi()
        {
            string input = DoiText.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                HintText = I18n.Get("AddEntry.Hint.InputAiRequired");
                return;
            }

            Candidates.Clear();
            BibtexText = string.Empty;
            HintText = I18n.Get("AddEntry.Hint.AskingAi");
            NotificationCenter.Info(I18n.Get("Message.GeneratingBibtexWithAi"));

            List<BibtexEntry> entries = await AiBibtexExtractor.ExtractEntriesFromReferenceTextAsync(input);
            HashSet<string> seen = new();
            foreach (BibtexEntry entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.BibTeX) || !seen.Add(entry.BibTeX))
                {
                    continue;
                }

                Candidates.Add(new AddEntryCandidateViewModel
                {
                    Source = I18n.Get("AddEntry.Source.Ai"),
                    Query = I18n.Get("AddEntry.Source.PastedText"),
                    Title = string.IsNullOrWhiteSpace(entry.Title) ? I18n.Get("Common.Untitled") : entry.Title,
                    CitationKey = entry.CitationKey,
                    BibTeX = entry.BibTeX,
                    IsSelected = true,
                });
            }

            UpdateSelectedBibtex();
            if (Candidates.Count == 0)
            {
                HintText = I18n.Get("AddEntry.Hint.NoValidAiEntries");
                NotificationCenter.Info(I18n.Get("Message.NoValidAiBibtexEntriesGenerated"));
                return;
            }

            HintText = I18n.Format("AddEntry.Hint.AiCandidatesReady", Candidates.Count);
            NotificationCenter.Info(I18n.Format("Message.GeneratedBibtexCandidates", Candidates.Count));
        }

        [RelayCommand]
        private void SelectAllCandidates()
        {
            foreach (var candidate in Candidates)
            {
                candidate.IsSelected = true;
            }
            UpdateSelectedBibtex();
        }

        [RelayCommand]
        private void ClearCandidateSelection()
        {
            foreach (var candidate in Candidates)
            {
                candidate.IsSelected = false;
            }
            UpdateSelectedBibtex();
        }

        [RelayCommand]
        private void RefreshSelectedBibtex()
        {
            UpdateSelectedBibtex();
        }

        private void UpdateSelectedBibtex()
        {
            BibtexText = string.Join("\n\n", Candidates.Where(c => c.IsSelected).Select(c => c.BibTeX));
        }
    }
}
