using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using Litenbib.Views;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class ExportViewModel(List<BibtexEntry> list = null!, string path = "") : ViewModelBase
    {
        [ObservableProperty]
        private string _path = GenerateNewPath(path);

        public List<BibtexEntry> Entries = list ?? [];

        private int authorFormat = 0;
        public int AuthorFormat
        {
            get => authorFormat;
            set
            {
                if (value < 0) { return; }  // necessary
                SetProperty(ref authorFormat, value);
            }
        }

        private int authorClip = 0;
        public int AuthorClip
        {
            get => authorClip;
            set
            {
                if (value < 0) { return; }  // necessary
                SetProperty(ref authorClip, value);
            }
        }

        [ObservableProperty]
        private int _maxAuthors = 5;

        [ObservableProperty]
        private string _ending = "and others";

        private static string GenerateNewPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length < 5)
            { return "export.bib"; }
            string s = path[..^4];
            string ext = path[^4..];
            return $"{s}_export{ext}";
        }

        [RelayCommand]
        private async Task Export(object? sender)
        {
            if (sender is not ExportView window) { return; }

            window.Close();
            Ending = " " + Ending.Trim();
            using var writer = new StreamWriter(Path, append: false, new UTF8Encoding(false), bufferSize: 65536); // 缓冲区大小设置为64KB
            if (authorClip == 0)
            {
                foreach (var entry in Entries)
                { await writer.WriteAsync(entry.ExportBibtex(authorFormat) + "\n"); }
            }
            else
            {
                foreach (var entry in Entries)
                { await writer.WriteAsync(entry.ExportBibtex(authorFormat, MaxAuthors, Ending) + "\n"); }
            }
        }
    }
}
