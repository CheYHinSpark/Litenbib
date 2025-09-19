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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Litenbib.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // 添加静态只读 JsonSerializerOptions 实例以供重用
        private static readonly JsonSerializerOptions CachedJsonOptions = new() { WriteIndented = true };
        private readonly string localConfig = "localconfig.json";

        public List<BibtexEntry> CopiedBibtex = [];

        public ObservableCollection<BibtexViewModel> BibtexViewers { get; set; }

        [ObservableProperty]
        private BibtexViewModel? _selectedFile;
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
                foreach (var item in BibtexViewers)
                { if (item.Edited) { return true; } }
                return false;
            }
        }

        public MainWindowViewModel()
        {
            BibtexViewers = [];
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

        public async Task OpenFileInit()
        {
            // 读取局部配置文件
            if (!File.Exists(localConfig))
            { return; }

            try
            {
                // 读取 JSON 文件的所有内容
                string jsonString = await File.ReadAllTextAsync(localConfig);

                // 反序列化 JSON 字符串到 Config 对象
                var config = JsonSerializer.Deserialize<LocalConfig>(jsonString);

                // 如果反序列化成功且列表不为空，返回它
                if (config?.RecentFiles != null && config?.RecentFiles.Count > 0)
                {
                    foreach (var filePath in config.RecentFiles)
                    {
                        if (File.Exists(filePath))
                        {
                            string fileContent = await File.ReadAllTextAsync(filePath);
                            var newBVM = new BibtexViewModel(Path.GetFileName(filePath), filePath, fileContent, 0);
                            BibtexViewers.Add(newBVM);
                            SelectedFile = newBVM;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Error deserializing JSON: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
                return;
            }
        }

        public void SaveLocalConfig()
        {
            var config = new LocalConfig { RecentFiles = [.. BibtexViewers.Select(b => b.FullPath)] };
            Debug.WriteLine($"Saving {config.RecentFiles.Count} recent files to local config.");
            try
            {
                // 使用缓存的 JsonSerializerOptions 实例
                string jsonString = JsonSerializer.Serialize(config, CachedJsonOptions);
                File.WriteAllText(localConfig, jsonString);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Error serializing to JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

        #region Command
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

            using var writer = new StreamWriter(file.Path.AbsolutePath, append: false, encoding: Encoding.UTF8, bufferSize: 65536); // 缓冲区大小设置为64KB
            await writer.WriteAsync(string.Empty);
            int newMode = SelectedFile == null ? 0 : SelectedFile.FilterMode;
            var newBVM = new BibtexViewModel(file.Name, file.Path.AbsolutePath, string.Empty, newMode);
            BibtexViewers.Add(newBVM);
            SelectedFile = newBVM;
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
                // 打开文件的读取流。
                await using var stream = await file.OpenReadAsync();
                using var streamReader = new StreamReader(stream);
                //// 将文件的所有内容作为文本读取。
                var fileContent = await streamReader.ReadToEndAsync();
                int newMode = SelectedFile == null ? 0 : SelectedFile.FilterMode;
                var newBVM = new BibtexViewModel(file.Name, file.Path.AbsolutePath, fileContent, newMode);
                BibtexViewers.Add(newBVM);
                SelectedFile = newBVM;
            }
        }

        [RelayCommand]
        private async Task CloseTab(BibtexViewModel? tab)
        {
            if (BibtexViewers == null) { return; }
            if (tab != null && BibtexViewers.Contains(tab))
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
                    { await Task.Run(() => tab.SaveBibtexCommand); }
                }
                BibtexViewers.Remove(tab);
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

        private bool CanAddBibtexEntry() => SelectedFile != null;

        [RelayCommand(CanExecute = nameof(CanSaveAll))]
        private async Task SaveAll()
        {
            List<Task> tasks = [];
            foreach (var item in BibtexViewers)
            {
                if (item.Edited)
                { tasks.Add(Task.Run(() => item.SaveBibtexCommand)); }
            }
            await Task.WhenAll(tasks);
        }

        private bool CanSaveAll() => SelectedFile != null;
        #endregion Command
    }
}
