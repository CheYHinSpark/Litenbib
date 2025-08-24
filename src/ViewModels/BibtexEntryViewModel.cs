using Litenbib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public class BibtexEntryViewModel: ViewModelBase
    {
        private ObservableCollection<BibtexEntry> bibtexEntries;

        internal ObservableCollection<BibtexEntry> BibtexEntries { get => bibtexEntries; }

        public BibtexEntryViewModel()
        {
            //var bibtex = "@Article{Bhatnagar_1954,\r\n" +
            //        "  author    = {Bhatnagar, P. L. and Gross, E. P. and Krook, M.},\r\n" +
            //        "  journal   = {Physical Review},\r\n" +
            //        "  title     = {A Model for Collision Processes in Gases. \\rm{I}. Small Amplitude Processes in Charged and Neutral One-Component Systems},\r\n" +
            //        "  year      = {1954},\r\n" +
            //        "  issn      = {0031-899X},\r\n" +
            //        "  month     = may,\r\n" +
            //        "  number    = {3},\r\n" +
            //        "  pages     = {511--525},\r\n" +
            //        "  volume    = {94},\r\n" +
            //        "  doi       = {10.1103/physrev.94.511},\r\n" +
            //        "  publisher = {American Physical Society (APS)},\r\n" +
            //        "}";
            init();
        }

        private void init()
        {
            bibtexEntries = new ObservableCollection<BibtexEntry>(BibtexParser.Parse(BibFile.Read("refs.bib")));
        }
    }
}
