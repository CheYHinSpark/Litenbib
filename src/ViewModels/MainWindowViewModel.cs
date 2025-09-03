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

        private string filterText = string.Empty;
        public string FilterText
        {
            get => filterText;
            set
            {
                filterText = value;
                if (SelectecdBibtex != null)
                { SelectecdBibtex.FilterText = filterText; }
            }
        }

        [ObservableProperty]
        private BibtexViewModel? _selectecdBibtex;

        partial void OnSelectecdBibtexChanged(BibtexViewModel? oldValue, BibtexViewModel? newValue)
        { OnPropertyChanged(nameof(ShowToolBar)); }

        public bool ShowToolBar { get => SelectecdBibtex != null; }

        partial void OnSelectecdBibtexChanged(BibtexViewModel? value)
        {
            // inform Commands to update
            AddBibtexEntryCommand.NotifyCanExecuteChanged();
            SaveFileCommand.NotifyCanExecuteChanged();

            if (value != null)
            { value.FilterText = filterText; }
        }


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

            //// 从当前控件获取 TopLevel。或者，您也可以使用 Window 引用。
            //      var topLevel = TopLevel.GetTopLevel(this);

            //          // 启动异步操作以打开对话框。
            //          var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            //          {
            //              Title = "Save Text File"
            //          });

            //          if (file is not null)
            //          {
            //              // 打开文件的写入流。
            //              await using var stream = await file.OpenWriteAsync();
            //              using var streamWriter = new StreamWriter(stream);
            //              // 将一些内容写入文件。
            //              await streamWriter.WriteLineAsync("Hello World!");
            //          }
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

        [RelayCommand(CanExecute = nameof(CanSaveFile))]
        private async Task SaveFile()
        {

        }
        private bool CanSaveFile() => SelectecdBibtex != null;

        [RelayCommand]
        private void UndoBibtex()
        {
            if (SelectecdBibtex == null) { return; }
            SelectecdBibtex.UndoRedoManager.Undo();
        }
        [RelayCommand]
        private void RedoBibtex()
        {
            if (SelectecdBibtex == null) { return; }
            SelectecdBibtex.UndoRedoManager.Redo();
        }
    }
}
