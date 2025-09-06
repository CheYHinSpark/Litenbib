using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using Litenbib.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Litenbib.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<BibtexViewModel> BibtexViewers { get; set; }

        [ObservableProperty]
        private BibtexViewModel? _selectecdBibtex;
        partial void OnSelectecdBibtexChanged(BibtexViewModel? value)
        {
            // inform Commands to update
            OnPropertyChanged(nameof(ShowToolBar));
            AddBibtexEntryCommand.NotifyCanExecuteChanged();
            SaveAllCommand.NotifyCanExecuteChanged();
        }

        public bool ShowToolBar { get => SelectecdBibtex != null; }

        public MainWindowViewModel()
        {
            BibtexViewers = [];
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
                //BibtexViewers.Add(new BibtexViewModel(file.Path.AbsolutePath));
                // 打开第一个文件的读取流。
                await using var stream = await file.OpenReadAsync();
                using var streamReader = new StreamReader(stream);
                //// 将文件的所有内容作为文本读取。
                var fileContent = await streamReader.ReadToEndAsync();
                BibtexViewers.Add(new BibtexViewModel(file.Name, file.Path.AbsolutePath, fileContent));
            }
        }

        [RelayCommand]
        private void CloseTab(BibtexViewModel? tab)
        {
            if (BibtexViewers == null) { return; }
            if (tab != null && BibtexViewers.Contains(tab))
            { BibtexViewers.Remove(tab); }
        }

        [RelayCommand(CanExecute = nameof(CanAddBibtexEntry))]
        private async Task AddBibtexEntry(Window window)
        {
            if (SelectecdBibtex == null)
            {
                Debug.WriteLine("No opened bib file");
                return;
            }
            // 创建对话框实例，并传入参数
            await SelectecdBibtex.AddBibtexEntry(window);
        }

        private bool CanAddBibtexEntry() => SelectecdBibtex != null;

        [RelayCommand(CanExecute = nameof(CanSaveAll))]
        private async Task SaveAll()
        { }
        private bool CanSaveAll() => SelectecdBibtex != null;
    }
}
