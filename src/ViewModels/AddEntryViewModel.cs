using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System.Collections.Generic;
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
            var dois = DoiText.Split('\n');
            List<Task<string>> tasks = [];
            foreach (var doi in dois)
            {
                if (!string.IsNullOrWhiteSpace(doi))
                { tasks.Add(LinkResolver.GetBibTeXAsync(doi)); }
            }
            BibtexText = string.Join("\n\n", await Task.WhenAll(tasks));
            HintText = string.IsNullOrWhiteSpace(BibtexText) ? "Resolve failed." : "Resolve successed.";
        }
    }
}
