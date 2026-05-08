using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using Litenbib.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
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
                if (value < 0) { return; }
                SetProperty(ref authorFormat, value);
            }
        }

        private int authorClip = 0;
        public int AuthorClip
        {
            get => authorClip;
            set
            {
                if (value < 0) { return; }
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

        private static bool TryGetValidatedPath(string? path, out string validatedPath, out string errorMessage)
        {
            validatedPath = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "Please choose an export path.";
                return false;
            }

            try
            {
                validatedPath = System.IO.Path.GetFullPath(path.Trim());
            }
            catch (Exception)
            {
                errorMessage = "The export path is invalid.";
                return false;
            }

            string? directory = System.IO.Path.GetDirectoryName(validatedPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                errorMessage = "The export directory is invalid.";
                return false;
            }

            string extension = System.IO.Path.GetExtension(validatedPath);
            if (!string.Equals(extension, ".bib", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "The export file must use the .bib extension.";
                return false;
            }

            return true;
        }

        private static async Task ShowMessage(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowAsync();
        }

        [RelayCommand]
        private async Task BrowsePath(object? sender)
        {
            if (sender is not Window window) { return; }

            string suggestedFileName = string.IsNullOrWhiteSpace(Path)
                ? "export.bib"
                : System.IO.Path.GetFileName(Path);

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Choose Export Path",
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "bib",
                ShowOverwritePrompt = true,
                FileTypeChoices =
                [
                    new FilePickerFileType("BibTeX Files")
                    {
                        Patterns = ["*.bib"]
                    },
                    FilePickerFileTypes.All
                ]
            });

            if (file != null)
            {
                Path = Uri.UnescapeDataString(file.Path.AbsolutePath);
            }
        }

        [RelayCommand]
        private async Task Export(object? sender)
        {
            if (sender is not ExportView window) { return; }

            if (!TryGetValidatedPath(Path, out string validatedPath, out string errorMessage))
            {
                NotificationCenter.Error(errorMessage);
                await ShowMessage("Export Failed", errorMessage);
                return;
            }

            if (authorClip != 0 && MaxAuthors <= 0)
            {
                NotificationCenter.Error("Max authors must be greater than 0.");
                await ShowMessage("Export Failed", "Max authors must be greater than 0.");
                return;
            }

            try
            {
                string? directory = System.IO.Path.GetDirectoryName(validatedPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string endingText = authorClip == 0 ? string.Empty : " " + Ending.Trim();
                using var writer = new StreamWriter(validatedPath, append: false, new UTF8Encoding(false), bufferSize: 65536);
                if (authorClip == 0)
                {
                    foreach (var entry in Entries)
                    {
                        await writer.WriteAsync(entry.ExportBibtex(authorFormat) + "\n");
                    }
                }
                else
                {
                    foreach (var entry in Entries)
                    {
                        await writer.WriteAsync(entry.ExportBibtex(authorFormat, MaxAuthors, endingText) + "\n");
                    }
                }

                NotificationCenter.Info($"Exported {Entries.Count} entries");
                await ShowMessage("Export Succeeded", $"Exported {Entries.Count} entries to:\n{validatedPath}");
                window.Close(true);
            }
            catch (Exception ex)
            {
                NotificationCenter.Error($"Could not export {System.IO.Path.GetFileName(validatedPath)}: {ex.Message}");
                await ShowMessage("Export Failed", $"Could not export file.\n{ex.Message}");
            }
        }
    }
}
