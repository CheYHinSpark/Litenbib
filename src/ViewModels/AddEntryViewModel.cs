using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class AddEntryViewModel: ViewModelBase
    {
        public int HeaderHeight { get; } = 40;

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
            var x = await DoiResolver.Doi2BibTeXAsync(DoiText);
            BibtexText = x ?? "";
            HintText = x == null ? "Resolve failed." : "Resolve successed.";
        }
    }
}
