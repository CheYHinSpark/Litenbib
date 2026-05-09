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
                HintText = "Please input DOI / arXiv / OpenReview / title / URL, one per line.";
                return;
            }

            Candidates.Clear();
            List<string> hints = [];
            HashSet<string> seen = new();
            NotificationCenter.Info($"Searching {inputs.Count} bibliography query(s)...");

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
                            Title = string.IsNullOrWhiteSpace(candidate.Title) ? "(Untitled)" : candidate.Title,
                            CitationKey = candidate.Entry?.CitationKey ?? string.Empty,
                            BibTeX = candidate.BibTeX,
                            IsSelected = true,
                        });
                        added++;
                    }
                    hints.Add($"{input}: {added} candidate(s)");
                }
                else
                {
                    hints.Add($"{input}: not found");
                }
            }

            UpdateSelectedBibtex();
            HintText = Candidates.Count == 0
                ? string.Join(" | ", hints)
                : $"{Candidates.Count} candidate(s) ready. Select what to import.";
            if (Candidates.Count == 0)
            {
                NotificationCenter.Info("No bibliography candidates found");
            }
            else
            {
                NotificationCenter.Info($"Found {Candidates.Count} bibliography candidate(s)");
            }
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
