using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using Litenbib.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // 添加静态只读 JsonSerializerOptions 实例以供重用
        private static readonly JsonSerializerOptions CachedJsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Litenbib");
        private static readonly string LocalConfigPath = Path.Combine(ConfigDirectory, "localconfig.json");

        // 主题色
        [ObservableProperty]
        private bool _themeIndex = false;

        public List<BibtexEntry> CopiedBibtex = [];

        public ObservableCollection<BibtexViewModel> BibtexTabs { get; set; }

        [ObservableProperty]
        private BibtexViewModel? _selectedFile;

        public ObservableCollection<RecentFileState> RecentFiles { get; } = [];
        partial void OnSelectedFileChanged(BibtexViewModel? value)
        {
            // inform Commands to update
            OnPropertyChanged(nameof(ShowToolBar));
            AddBibtexEntryCommand.NotifyCanExecuteChanged();
            SaveAllCommand.NotifyCanExecuteChanged();
        }

        public bool ShowToolBar { get => SelectedFile != null; }

        public static ObservableCollection<string> FilterFieldList
        { get => ["Whole", "Author", "Title", "Citation Key"]; }

        public bool NeedSave
        {
            get
            {
                foreach (var item in BibtexTabs)
                { if (item.Edited) { return true; } }
                return false;
            }
        }

        public MainWindowViewModel()
        {
            BibtexTabs = [];
        }

        private void RefreshRecentFiles()
        {
            RecentFiles.Clear();
            foreach (var tab in BibtexTabs)
            {
                RecentFiles.Add(new RecentFileState
                {
                    FilePath = tab.FullPath,
                    FileName = tab.Header,
                    FilterMode = tab.FilterMode,
                    FilterField = tab.FilterField,
                    FilterText = tab.FilterText,
                });
            }
            OnPropertyChanged(nameof(RecentFiles));
        }

        public async Task CopyBibtexEntries(IEnumerable<BibtexEntry> list)
        {
            await Task.Run(() =>
            {
                CopiedBibtex = [];
                foreach (var item in list)
                {
                    if (item is BibtexEntry entry)
                    {
                        BibtexEntry e = BibtexEntry.CopyFrom(entry);
                        CopiedBibtex.Add(e);
                    }
                }
            });
        }

        public async Task<LocalConfig?> LoadLocalConfig()
        {
            if (!File.Exists(LocalConfigPath))
            {
                Application.Current!.RequestedThemeVariant = ThemeIndex ? ThemeVariant.Light : ThemeVariant.Dark;
                return null;
            }
            try
            {
                string jsonString = await File.ReadAllTextAsync(LocalConfigPath);
                var config = JsonSerializer.Deserialize<LocalConfig>(jsonString);
                ThemeIndex = config?.ThemeIndex ?? false;
                Application.Current!.RequestedThemeVariant = ThemeIndex ? ThemeVariant.Light : ThemeVariant.Dark;
                if (config?.RecentFiles != null && config.RecentFiles.Count > 0)
                {
                    foreach (var fileState in config.RecentFiles)
                    {
                        if (!File.Exists(fileState.FilePath))
                        {
                            continue;
                        }
                        string fileContent = await File.ReadAllTextAsync(fileState.FilePath);
                        var newBVM = new BibtexViewModel(
                            Path.GetFileName(fileState.FilePath),
                            fileState.FilePath,
                            fileContent,
                            fileState.FilterMode)
                        {
                            FilterField = string.IsNullOrWhiteSpace(fileState.FilterField) ? "Whole" : fileState.FilterField,
                            FilterText = fileState.FilterText ?? string.Empty,
                        };
                        BibtexTabs.Add(newBVM);
                    }
                    RefreshRecentFiles();
                    if (BibtexTabs.Count > 0)
                    {
                        int selectedIndex = config.SelectedTabIndex;
                        if (selectedIndex < 0 || selectedIndex >= BibtexTabs.Count)
                        {
                            selectedIndex = BibtexTabs.Count - 1;
                        }
                        SelectedFile = BibtexTabs[selectedIndex];
                    }
                }
                return config;
            }
            catch (JsonException ex)
            { Debug.WriteLine($"Error deserializing JSON: {ex.Message}"); }
            catch (Exception ex)
            { Debug.WriteLine($"An unexpected error occurred: {ex.Message}"); }
            return null;
        }

        public async Task SaveLocalConfig(Window? window = null)
        {
            var config = new LocalConfig
            {
                ThemeIndex = ThemeIndex,
                SelectedTabIndex = SelectedFile == null ? -1 : BibtexTabs.IndexOf(SelectedFile),
                RecentFiles = [.. BibtexTabs.Select(b => new RecentFileState
                {
                    FilePath = b.FullPath,
                    FileName = b.Header,
                    FilterMode = b.FilterMode,
                    FilterField = b.FilterField,
                    FilterText = b.FilterText,
                })]
            };
            if (window != null)
            {
                config.WindowState = window.WindowState;
                if (window.WindowState == WindowState.Normal)
                {
                    config.WindowWidth = window.Width;
                    config.WindowHeight = window.Height;
                    config.WindowPositionX = window.Position.X;
                    config.WindowPositionY = window.Position.Y;
                }
            }
            try
            {
                Directory.CreateDirectory(ConfigDirectory);
                await File.WriteAllTextAsync(LocalConfigPath, JsonSerializer.Serialize(config, CachedJsonOptions));
            }
            catch (JsonException ex)
            { Debug.WriteLine($"Error serializing to JSON: {ex.Message}"); }
            catch (Exception ex)
            { Debug.WriteLine($"An unexpected error occurred: {ex.Message}"); }
        }

        private async void ExtractPdf(string pdfFile)
        {
            var x = PdfMetadataExtractor.Extract(pdfFile);
            await Task.Delay(1000);
            Debug.WriteLine(x.Doi);
            Debug.WriteLine(x.ArxivId);
        }

        public async void DropProcess(List<IStorageItem> files)
        {
            // 如果没有Tab打开，
            if (SelectedFile == null)
            {
                // 打开文件中的所有bib文件
                foreach (var file in files)
                {
                    if (file is IStorageFile f && f.Name.EndsWith(".bib"))
                    {
                        await OpenFile(f);
                    }
                }
            }
            // 如果有文件打开，调用到文件里面去
            else
            {
                foreach (var file in files)
                {
                    if (file is IStorageFile f && f.Name.EndsWith(".pdf"))
                    {
                        await SelectedFile.ExtractPdf(Uri.UnescapeDataString(file.Path.AbsolutePath));
                    }
                }
            }
            
        }

        private async Task OpenFile(IStorageFile file)
        {
            string fullPath = Uri.UnescapeDataString(file.Path.AbsolutePath);
            await OpenFile(fullPath, file.Name);
        }

        private async Task OpenFile(string fullPath, string? fileName = null)
        {
            var existed = BibtexTabs.FirstOrDefault(b => string.Equals(b.FullPath, fullPath, StringComparison.OrdinalIgnoreCase));
            if (existed != null)
            {
                SelectedFile = existed;
                return;
            }

            var fileContent = await File.ReadAllTextAsync(fullPath);
            int newMode = SelectedFile == null ? 0 : SelectedFile.FilterMode;
            var newBVM = new BibtexViewModel(fileName ?? Path.GetFileName(fullPath), fullPath, fileContent, newMode);
            BibtexTabs.Add(newBVM);
            SelectedFile = newBVM;
            RefreshRecentFiles();
        }

        #region Command
        [RelayCommand]
        private void ChangeTheme()
        {
            ThemeIndex = !ThemeIndex;
            Application.Current!.RequestedThemeVariant = ThemeIndex? ThemeVariant.Light : ThemeVariant.Dark;
        }

        [RelayCommand]
        private async Task ExportFile(Window? window)
        {
            if (window == null || SelectedFile == null) { return; }
            ExportView dialog = new([.. SelectedFile.BibtexEntries], SelectedFile.FullPath);
            await dialog.ShowDialog<bool>(window);
        }

        [RelayCommand]
        private async Task NewFile(Window? window)
        {
            if (window == null) { return; }
            //// 启动异步操作以打开对话框。
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Create BibTeX File",
                SuggestedFileName = "new.bib",
                DefaultExtension = "bib",
                ShowOverwritePrompt = true,
                // 可选：添加文件类型过滤器
                FileTypeChoices =
                [
                    new FilePickerFileType("BibTeX Files")
                    {
                        Patterns = ["*.bib"]
                    },
                    FilePickerFileTypes.All
                ]
            });

            if (file == null) { return; }

            string fullPath = Uri.UnescapeDataString(file.Path.AbsolutePath);
            try
            {
                using var writer = new StreamWriter(fullPath, append: false, encoding: new UTF8Encoding(false), bufferSize: 65536);
                await writer.WriteAsync(string.Empty);
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Create File Failed", $"Could not create file.\n{ex.Message}", ButtonEnum.Ok);
                await box.ShowAsync();
                return;
            }
            int newMode = SelectedFile == null ? 0 : SelectedFile.FilterMode;
            var newBVM = new BibtexViewModel(file.Name, fullPath, string.Empty, newMode);
            BibtexTabs.Add(newBVM);
            SelectedFile = newBVM;
            RefreshRecentFiles();
        }

        [RelayCommand]
        private async Task OpenFile(Window? window)
        {
            if (window == null) { return; }
            //// 启动异步操作以打开对话框。
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open BibTeX File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("BibTeX Files")
                    {
                        Patterns = ["*.bib"]
                    },
                    FilePickerFileTypes.All
                ]
            });

            foreach (var file in files)
            {
                await OpenFile(file);
            }
        }

        [RelayCommand]
        private async Task OpenRecentFile(RecentFileState? recent)
        {
            if (recent == null || string.IsNullOrWhiteSpace(recent.FilePath)) { return; }
            if (!File.Exists(recent.FilePath))
            {
                RecentFiles.Remove(recent);
                return;
            }

            var existed = BibtexTabs.FirstOrDefault(b => string.Equals(b.FullPath, recent.FilePath, StringComparison.OrdinalIgnoreCase));
            if (existed != null)
            {
                SelectedFile = existed;
                return;
            }

            await OpenFile(recent.FilePath, string.IsNullOrWhiteSpace(recent.FileName) ? null : recent.FileName);
        }

        [RelayCommand]
        private async Task CloseTab(BibtexViewModel? tab)
        {
            if (tab != null && BibtexTabs.Contains(tab))
            {
                if (tab.Edited)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        "Warning", "This file have been edited, but not saved. Do you want to save it?",
                        ButtonEnum.YesNoCancel);
                    var result = await box.ShowAsync();
                    if (result == ButtonResult.Cancel)
                    { return; }
                    else if (result == ButtonResult.Yes)
                    { await tab.SaveBibtexCommand.ExecuteAsync(null); }
                }
                BibtexTabs.Remove(tab);
                RefreshRecentFiles();
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddBibtexEntry))]
        private async Task AddBibtexEntry(Window window)
        {
            if (SelectedFile != null)
            {
                // 创建对话框实例，并传入参数
                await SelectedFile.AddBibtexEntry(window);
            }
        }

        [RelayCommand]
        private async Task SaveFileAs(Window? window)
        {
            if (window == null || SelectedFile == null) { return; }
            await SelectedFile.SaveBibtexAs(window);
            RefreshRecentFiles();
        }

        private bool CanAddBibtexEntry() => SelectedFile != null;

        [RelayCommand(CanExecute = nameof(CanSaveAll))]
        private async Task SaveAll()
        {
            foreach (var item in BibtexTabs)
            {
                if (item.Edited)
                {
                    await item.SaveBibtexCommand.ExecuteAsync(null);
                }
            }
        }

        private bool CanSaveAll() => SelectedFile != null;

        [RelayCommand]
        private static void OpenWebsite(string url)
        { UriProcessor.StartProcess(url); }

        #endregion Command
    }
}
