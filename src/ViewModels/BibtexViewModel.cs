using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Shapes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Litenbib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class BibtexViewModel: ViewModelBase
    {
        public string Header { get; set; }

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

        public BibtexViewModel()
        {
            Header = "refs.bib";
            BibtexEntries = new ObservableCollection<BibtexEntry>(BibtexParser.Parse(BibFile.Read("refs.bib")));
            _showingEntry = new("", "");
        }
        public BibtexViewModel(string path)
        {
            var list = path.Split('\\');
            if (list.Length == 1) { list = path.Split('/'); }
            Header = list[^1];
            BibtexEntries = new ObservableCollection<BibtexEntry>(BibtexParser.Parse(BibFile.Read(path)));
            _showingEntry = new("", "");
        }
        public BibtexViewModel(string header, string filecontent)
        {
            Header = header;
            BibtexEntries = new ObservableCollection<BibtexEntry>(BibtexParser.Parse(filecontent));
            _showingEntry = new("", "");
        }


        public void ChangeShowing(object i)
        {
            if (i is not BibtexEntry entry) { return; }
            ShowingEntry = entry;
        }
    }
}
