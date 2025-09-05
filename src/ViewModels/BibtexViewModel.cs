using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using Litenbib.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class BibtexViewModel: ViewModelBase
    {
        public string Header { get; set; }

        public string FullPath { get; set; }

        public UndoRedoManager UndoRedoManager { get; set; }

        public ObservableCollection<BibtexEntry> BibtexEntries { get; set; }
        public DataGridCollectionView BibtexView { get; }

        [ObservableProperty]
        private BibtexEntry _showingEntry;
        partial void OnShowingEntryChanged(BibtexEntry value)
        { DeleteBibtexCommand.NotifyCanExecuteChanged(); }

        public List<(int, BibtexEntry)>? SelectedIndexItems { get; set; }

        public void SetSelectedItems(IEnumerable<BibtexEntry> entries)
        {
            SelectedIndexItems = [];
            foreach (var entry in entries)
            { SelectedIndexItems.Add((BibtexEntries.IndexOf(entry), entry)); }
        }

        private string filterText = string.Empty;
        private string[] filters = [];

        public string FilterText
        {
            get => filterText;
            set
            {
                if (filterText == value) { return; }
                filterText = value;
                if (!string.IsNullOrEmpty(value))
                { filters = value.Split(' '); }
                BibtexView.Refresh();
            }
        }

        private int oldSelectionStart = -1;
        private int selectionStart = -1;
        public int SelectionStart
        {
            set
            {
                oldSelectionStart = selectionStart;
                selectionStart = value;
                SetIsTailSelected();
            }
        }

        private int oldSelectionEnd = -1;
        private int selectionEnd = -1;
        public int SelectionEnd
        {
            set
            {
                oldSelectionEnd = selectionEnd;
                selectionEnd = value;
                SetIsTailSelected();
            }
        }

        private bool isTailSelected = false;

        public TextBox? CurrentTextBox { set { UndoRedoManager.NewEditedBox = value; } }

        public static ObservableCollection<string> TypeList
        {
            get => ["Article", "Book", "Booklet", "Conference",
                "InBook", "InCollection", "InProceedings", "Manual",
                "MastersThesis", "Misc", "PhdThesis", "Proceedings",
                "TechReport", "Unpublished"];
        }

        public BibtexViewModel(string header, string fullPath, string filecontent)
        {
            Header = header;
            FullPath = fullPath;
            BibtexEntries = new ObservableCollection<BibtexEntry>(BibtexParser.Parse(filecontent));
            foreach (var entry in BibtexEntries)
            { entry.UndoRedoPropertyChanged += OnEntryPropertyChanged; }
            BibtexView = new(BibtexEntries)
            {
                Filter = entry => FilterBibtex(entry as BibtexEntry)
            };
            ShowingEntry = BibtexEntry.Null;
            UndoRedoManager = new();
        }

        public void ChangeShowing(object o)
        {
            if (o is not BibtexEntry entry) { return; }
            ShowingEntry = entry;
        }

        public async Task AddBibtexEntry(Window window)
        {
            AddEntryWindow dialog = new();

            // 5. 显示对话框并等待结果 (ShowDialog 需要传入父窗口引用)
            var result = await dialog.ShowDialog<bool>(window); // 等待对话框关闭并获取 DialogResult

            if (result == true) // 如果用户点击了 OK
            {
                if (dialog.DataContext == null) { return; }
                // 通过对话框的公共属性获取返回值
                string bibtex = ((AddEntryViewModel)dialog.DataContext).BibtexText;
                foreach (BibtexEntry entry in BibtexParser.Parse(bibtex))
                {
                    UndoRedoManager.AddAction(new AddEntryAction(BibtexEntries, entry, BibtexEntries.Count));
                    BibtexEntries.Add(entry);
                }
            }
        }

        private bool FilterBibtex(BibtexEntry? entry)
        {
            if (string.IsNullOrEmpty(filterText)) { return true; }
            if (entry == null) { return false; }
            foreach (string s in filters)
            {
                if (!string.IsNullOrEmpty(s) && entry.BibTeX.Contains(s, StringComparison.OrdinalIgnoreCase))
                { return true; }
            }
            return false;
        }

        private void SetIsTailSelected()
        {
            if (UndoRedoManager.LastEditedBox is TextBox tb)
            {
                if (string.IsNullOrEmpty(tb.Text))
                { isTailSelected = 0 == selectionEnd || 0 ==  selectionStart; }
                else
                { isTailSelected = tb.Text.Length == selectionEnd || tb.Text.Length == selectionStart;}
            }
        }

        private async void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当前观察：在TextBox中操作之后如果鼠标在最后，会先改变NewSelectionStart
            // 如果不是在最后，会先改变TextBox.Selected，之后才Delay才
            if (sender is not BibtexEntry item || e.PropertyName == null) { return; }
            if (e is PropertyChangedEventArgsEx extendedArgs)
            {
                if (isTailSelected)
                {
                    var action = new EntryChangeAction(item, e.PropertyName,
                        (string?)extendedArgs.OldValue, (string?)extendedArgs.NewValue,
                        int.Max(oldSelectionStart, oldSelectionEnd), selectionStart);
                    UndoRedoManager.AddAction(action);
                }
                else
                {
                    int oldEnd = selectionEnd;
                    await Task.Delay(1);
                    // 创建并添加操作到管理器
                    var action = new EntryChangeAction(item, e.PropertyName,
                        (string?)extendedArgs.OldValue, (string?)extendedArgs.NewValue,
                        oldEnd, selectionStart);
                    UndoRedoManager.AddAction(action);
                }
                NotifyCanUndoRedo();
            }
        }

        private void NotifyCanUndoRedo()
        {
            UndoBibtexCommand.NotifyCanExecuteChanged();
            RedoBibtexCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoBibtex()
        {
            UndoRedoManager.Undo();
            NotifyCanUndoRedo();
        }

        private bool CanUndo() => UndoRedoManager.CanUndo;

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void RedoBibtex()
        {
            UndoRedoManager.Redo();
            NotifyCanUndoRedo();
        }
        private bool CanRedo() => UndoRedoManager.CanRedo;

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void DeleteBibtex()
        {
            // 把那些东西全部删除掉
            if (SelectedIndexItems == null || SelectedIndexItems.Count == 0) { return; }
            UndoRedoManager.AddAction(new DeleteEntriesAction(BibtexEntries, SelectedIndexItems));
            foreach (var item in SelectedIndexItems)
            { 
                if (ShowingEntry == item.Item2)
                { ShowingEntry = BibtexEntry.Null; }
                BibtexEntries.Remove(item.Item2);
            }
            NotifyCanUndoRedo();
        }
        private bool CanDelete() => ShowingEntry != null && UndoRedoManager.NewEditedBox == null;


        [RelayCommand]
        private void ToLink(object o)
        {
            if (o is BibtexEntry entry)
            {
                string url = entry.DOI == "" ? entry.Url : "https://doi.org/" + entry.DOI;
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch
                {
                    // 跨平台兼容处理
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
