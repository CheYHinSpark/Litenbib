using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
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
        public int HeaderHeight { get; } = 40;

        public ObservableCollection<BibtexViewModel>? BibtexViewers { get; }

        public MainWindowViewModel()
        {
            BibtexViewers = [new BibtexViewModel()];
        }

        [RelayCommand]
        private static async Task OpenFile(Window? window)
        {
            if (window == null) { return; }
            //// 启动异步操作以打开对话框。
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("BibTeX Files")
                    {
                        Patterns = ["*.bib"],
                    },
                    FilePickerFileTypes.All
                ]
            });

            foreach (var file in files)
            {
                // 打开第一个文件的读取流。
                await using var stream = await file.OpenReadAsync();
                using var streamReader = new StreamReader(stream);
                // 将文件的所有内容作为文本读取。
                var fileContent = await streamReader.ReadToEndAsync();
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
    }
}
