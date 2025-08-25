using CommunityToolkit.Mvvm.ComponentModel;
using Litenbib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class BibtexViewerViewModel: ViewModelBase
    {
        public ObservableCollection<BibtexEntry> BibtexEntries { get; set; }

        [ObservableProperty]
        private BibtexEntry _showingEntry;

        [ObservableProperty]
        private int _selectedIndex;

        public ObservableCollection<string> TypeList
        {
            get => ["Article", "Book", "Booklet", "Conference",
                "InBook", "InCollection", "InProceedings", "Manual",
                "MastersThesis", "Misc", "PhdThesis", "Proceedings",
                "TechReport", "Unpublished"];
        }
        
        public BibtexViewerViewModel()
        {
            BibtexEntries = new ObservableCollection<BibtexEntry>(BibtexParser.Parse(BibFile.Read("refs.bib")));
            _showingEntry = new("", "");
        }

        internal void ChangeShowing(int i)
        {
            if (i < 0 || i >= BibtexEntries.Count) { return; }
            ShowingEntry = BibtexEntries[i];
            SelectedIndex = i;
        }
    }
}
