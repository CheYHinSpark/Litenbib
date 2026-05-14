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
        private static readonly string ConfigDirectory = AppPaths.ConfigDirectory;
        private static readonly string LocalConfigPath = AppPaths.LocalConfigPath;

        // 主题色
        [ObservableProperty]
        private bool _themeIndex = false;

        public List<BibtexEntry> CopiedBibtex = [];

        public ObservableCollection<BibtexViewModel> BibtexTabs { get; set; }

        public static ObservableCollection<ToastNotification> Notifications => NotificationCenter.Messages;

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
            CloseOtherTabsCommand.NotifyCanExecuteChanged();
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
                AppSettingsState.Apply(new AppSettings());
                Application.Current!.RequestedThemeVariant = ThemeIndex ? ThemeVariant.Light : ThemeVariant.Dark;
                return null;
            }
            try
            {
                string jsonString = await File.ReadAllTextAsync(LocalConfigPath);
                var config = JsonSerializer.Deserialize<LocalConfig>(jsonString);
                AppSettingsState.Apply(config?.Settings);
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
            {
                NotificationCenter.Error($"Could not read local config: {ex.Message}");
                Debug.WriteLine($"Error deserializing JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                NotificationCenter.Error($"Could not restore recent files: {ex.Message}");
                Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
            return null;
        }

        public async Task SaveLocalConfig(Window? window = null)
        {
            var config = new LocalConfig
            {
                ThemeIndex = ThemeIndex,
                Settings = AppSettingsState.Current.Copy(),
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
            {
                NotificationCenter.Error($"Could not save local config: {ex.Message}");
                Debug.WriteLine($"Error serializing to JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                NotificationCenter.Error($"Could not save local config: {ex.Message}");
                Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task DropProcess(List<IStorageItem> files)
        {
            var storageFiles = files.OfType<IStorageFile>().ToList();

            // 如果没有Tab打开，先打开文件中的所有bib文件
            if (SelectedFile == null)
            {
                foreach (var file in storageFiles)
                {
                    if (Path.GetExtension(file.Name).Equals(".bib", StringComparison.OrdinalIgnoreCase))
                    {
                        await OpenFile(file);
                    }
                }
            }

            var pdfFiles = storageFiles
                .Where(file => Path.GetExtension(file.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (pdfFiles.Count == 0)
            {
                return;
            }

            if (SelectedFile == null)
            {
                NotificationCenter.Info("Open or create a .bib file before importing PDFs");
                return;
            }

            foreach (var file in pdfFiles)
            {
                await SelectedFile.ExtractPdf(GetStoragePath(file));
            }
        }

        private async Task OpenFile(IStorageFile file)
        {
            string fullPath = GetStoragePath(file);
            await OpenFile(fullPath, file.Name);
        }

        private static string GetStoragePath(IStorageItem file)
        {
            return file.Path.IsFile
                ? file.Path.LocalPath
                : Uri.UnescapeDataString(file.Path.AbsolutePath);
        }

        private async Task OpenFile(string fullPath, string? fileName = null)
        {
            var existed = BibtexTabs.FirstOrDefault(b => string.Equals(b.FullPath, fullPath, StringComparison.OrdinalIgnoreCase));
            if (existed != null)
            {
                SelectedFile = existed;
                return;
            }

            try
            {
                var fileContent = await File.ReadAllTextAsync(fullPath);
                int newMode = SelectedFile == null ? 0 : SelectedFile.FilterMode;
                var newBVM = new BibtexViewModel(fileName ?? Path.GetFileName(fullPath), fullPath, fileContent, newMode);
                BibtexTabs.Add(newBVM);
                SelectedFile = newBVM;
                RefreshRecentFiles();
                NotificationCenter.Info($"Opened {newBVM.Header}");
            }
            catch (Exception ex)
            {
                NotificationCenter.Error($"Could not open {Path.GetFileName(fullPath)}: {ex.Message}");
            }
        }

        private static string GetWindowsCopyPath(string sourcePath)
        {
            string directory = Path.GetDirectoryName(sourcePath)
                ?? throw new InvalidOperationException("The source directory is invalid.");
            string fileName = GetWindowsCopyBaseName(Path.GetFileNameWithoutExtension(sourcePath));
            string extension = Path.GetExtension(sourcePath);

            string candidate = Path.Combine(directory, $"{fileName} - Copy{extension}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }

            for (int index = 2; ; index++)
            {
                candidate = Path.Combine(directory, $"{fileName} - Copy ({index}){extension}");
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        private static string GetWindowsCopyBaseName(string fileName)
        {
            const string simpleCopySuffix = " - Copy";
            if (fileName.EndsWith(simpleCopySuffix, StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^simpleCopySuffix.Length];
            }

            const string numberedCopySuffixStart = " - Copy (";
            if (!fileName.EndsWith(')'))
            {
                return fileName;
            }

            int suffixStart = fileName.LastIndexOf(numberedCopySuffixStart, StringComparison.OrdinalIgnoreCase);
            if (suffixStart < 0)
            {
                return fileName;
            }

            int numberStart = suffixStart + numberedCopySuffixStart.Length;
            string numberText = fileName[numberStart..^1];
            return int.TryParse(numberText, out int copyNumber) && copyNumber >= 2
                ? fileName[..suffixStart]
                : fileName;
        }

        private static async Task<bool> PromptSaveIfEdited(BibtexViewModel tab, string actionDescription)
        {
            if (!tab.Edited)
            {
                return true;
            }

            var box = MessageBoxManager.GetMessageBoxStandard(
                "Unsaved Changes",
                $"{tab.Header} has unsaved changes. Do you want to save it before {actionDescription}?",
                ButtonEnum.YesNoCancel);
            var result = await box.ShowAsync();
            if (result == ButtonResult.Cancel)
            {
                return false;
            }

            if (result == ButtonResult.Yes)
            {
                return await tab.SaveCurrentAsync();
            }

            return true;
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
        private async Task OpenSettings(Window? window)
        {
            if (window == null) { return; }
            SettingsView dialog = new(AppSettingsState.Current);
            var result = await dialog.ShowDialog<bool>(window);
            if (result != true || dialog.DataContext is not SettingsViewModel vm) { return; }

            AppSettingsState.Apply(vm.ToSettings());
            foreach (var tab in BibtexTabs)
            {
                tab.RefreshGeneratedBibtex();
            }
            await SaveLocalConfig(window);
            NotificationCenter.Info("Settings saved");
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
                NotificationCenter.Error($"Could not create {file.Name}: {ex.Message}");
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
            NotificationCenter.Info($"Created {newBVM.Header}");
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
                NotificationCenter.Error($"Recent file not found: {recent.FileName}");
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
                if (!await PromptSaveIfEdited(tab, "closing it"))
                {
                    return;
                }

                BibtexTabs.Remove(tab);
                RefreshRecentFiles();
            }
        }

        [RelayCommand]
        private async Task DuplicateTab(BibtexViewModel? tab)
        {
            if (tab == null || !BibtexTabs.Contains(tab))
            {
                return;
            }

            if (!await PromptSaveIfEdited(tab, "duplicating it"))
            {
                return;
            }

            string sourcePath;
            try
            {
                sourcePath = Path.GetFullPath(tab.FullPath);
            }
            catch (Exception)
            {
                NotificationCenter.Error($"Could not duplicate {tab.Header}: invalid file path");
                return;
            }

            if (!File.Exists(sourcePath))
            {
                NotificationCenter.Error($"Could not duplicate {tab.Header}: source file not found");
                return;
            }

            try
            {
                string duplicatePath = GetWindowsCopyPath(sourcePath);
                File.Copy(sourcePath, duplicatePath);
                await OpenFile(duplicatePath, Path.GetFileName(duplicatePath));
            }
            catch (Exception ex)
            {
                NotificationCenter.Error($"Could not duplicate {tab.Header}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanCloseOtherTabs))]
        private async Task CloseOtherTabs(BibtexViewModel? tab)
        {
            if (tab == null || !BibtexTabs.Contains(tab))
            {
                return;
            }

            var tabsToClose = BibtexTabs.Where(item => item != tab).ToList();
            foreach (var item in tabsToClose)
            {
                if (!await PromptSaveIfEdited(item, "closing it"))
                {
                    return;
                }
            }

            foreach (var item in tabsToClose)
            {
                BibtexTabs.Remove(item);
            }

            SelectedFile = tab;
            RefreshRecentFiles();
        }

        private bool CanCloseOtherTabs(BibtexViewModel? tab) => tab != null && BibtexTabs.Contains(tab) && BibtexTabs.Count > 1;

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
            if (await SaveAllFilesAsync())
            {
                NotificationCenter.Info("All edited files saved");
            }
        }

        public async Task<bool> SaveAllFilesAsync()
        {
            foreach (var item in BibtexTabs)
            {
                if (item.Edited)
                {
                    if (!await item.SaveCurrentAsync())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CanSaveAll() => SelectedFile != null;

        [RelayCommand]
        private static void OpenWebsite(string url)
        { UriProcessor.StartProcess(url); }

        [RelayCommand]
        private static void DismissNotification(ToastNotification? notification)
        { NotificationCenter.Dismiss(notification); }

        #endregion Command
    }
}
