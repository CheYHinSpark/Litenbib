using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class AddEntryViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _doiText;
        [ObservableProperty]
        private string _bibtexText;
        [ObservableProperty]
        private string _hintText;

        public AddEntryViewModel()
        {
            DoiText = "";
            BibtexText = "";
            HintText = "";
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

            List<string> resolvedBibtex = [];
            List<string> hints = [];
            foreach (string input in inputs)
            {
                var result = await LinkResolver.ResolveAsync(input, 3);
                if (result.Success)
                {
                    resolvedBibtex.AddRange(result.Candidates.Select(c => c.BibTeX));
                    hints.Add($"{input}: {result.Candidates.Count} candidate(s)");
                }
                else
                {
                    hints.Add($"{input}: not found");
                }
            }

            BibtexText = string.Join("\n\n", resolvedBibtex.Distinct());
            HintText = string.Join(" | ", hints);
        }
    }
}
