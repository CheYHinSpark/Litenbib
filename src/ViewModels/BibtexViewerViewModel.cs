using Avalonia.Controls;
using Avalonia.Controls.Selection;
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
        public bool AllSelected {  get; set; }
        public ObservableCollection<BibtexEntry> BibtexEntries { get; set; }

        [ObservableProperty]
        private BibtexEntry _showingEntry;


        public static ObservableCollection<string> TypeList
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

        public void ChangeShowing(object i)
        {
            if (i is not BibtexEntry entry) { return; }
            ShowingEntry = entry;
        }
    }
}
