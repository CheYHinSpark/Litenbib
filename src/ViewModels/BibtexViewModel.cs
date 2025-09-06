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
using Newtonsoft.Json.Linq;
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
using static System.Net.Mime.MediaTypeNames;

namespace Litenbib.ViewModels
{
    public partial class BibtexViewModel: ViewModelBase
    {
        public string Header { get; set; }

        public string FullPath { get; set; }

        public bool Edited { get => UndoRedoManager.Edited; }

        public UndoRedoManager UndoRedoManager { get; set; }

        public ObservableRangeCollection<BibtexEntry> BibtexEntries { get; set; }
        
        public DataGridCollectionView BibtexView { get; }

        private bool _suppressShowingEntry = false;
        private BibtexEntry _holdShowingEntry;

        private BibtexEntry? _showingEntry;
        public BibtexEntry? ShowingEntry
        {
            get => _showingEntry;
            set
            {
                if (_suppressShowingEntry)
                { _holdShowingEntry = value!; }
                else
                {
                    SetProperty(ref _showingEntry, value);
                    _holdShowingEntry = value!;
                    DeleteBibtexCommand.NotifyCanExecuteChanged();
                    DeleteBibtexKeyCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public List<(int, BibtexEntry)>? SelectedIndexItems { get; set; }

        public void SetSelectedItems(IEnumerable<BibtexEntry> entries)
        {
            SelectedIndexItems = [];
            foreach (var e in entries)
            { SelectedIndexItems.Add((BibtexEntries.IndexOf(e), e)); }
        }

        private string[] filters = [];
        private string filterText = string.Empty;

        public string FilterText
        {
            set
            {
                SetProperty(ref filterText, value);
                RefreshFilter();
            }
        }

        private int filterMode = -1;
        public int FilterMode
        {
            get => filterMode;
            set
            {
                if (value < 0) { return; }
                SetProperty(ref filterMode, value);
                RefreshFilter();
            }
        }

        private bool isFiltering = false;
        public bool IsFiltering
        { set { SetProperty(ref isFiltering, value); } }

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

        public BibtexViewModel(string header, string fullPath, string filecontent, int filterMode = 0)
        {
            Header = header;
            FullPath = fullPath;
            BibtexEntries = new ObservableRangeCollection<BibtexEntry>(BibtexParser.Parse(filecontent));
            foreach (var entry in BibtexEntries)
            { entry.UndoRedoPropertyChanged += OnEntryPropertyChanged; }
            BibtexView = new(BibtexEntries)
            {
                Filter = entry => FilterBibtex(entry as BibtexEntry)
            };
            _holdShowingEntry = null!;
            UndoRedoManager = new();
            FilterMode = filterMode;
        }

        public async Task AddBibtexEntry(Window window)
        {
            AddEntryWindow dialog = new();

            // 显示对话框并等待结果 (ShowDialog 需要传入父窗口引用)
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



        private void RefreshFilter()
        {
            if (string.IsNullOrEmpty(filterText))
            { filters = []; }
            else
            {
                if (filterMode == 2)
                { filters = [filterText]; }
                else
                { filters = filterText.Split(' '); }
            }
            BibtexView.Refresh();
        }

        private bool FilterBibtex(BibtexEntry? entry)
        {
            // Mode=0 and // Mode=1 or // Mode=2 all
            if (string.IsNullOrEmpty(filterText)) { return true; }
            if (entry == null) { return false; }
            if (filterMode == 1)
            {
                foreach (string s in filters)
                {
                    if (!string.IsNullOrEmpty(s) && entry.BibTeX.Contains(s, StringComparison.OrdinalIgnoreCase))
                    { return true; }
                }
            }
            else
            {
                foreach (string s in filters)
                {
                    if (!string.IsNullOrEmpty(s) && !entry.BibTeX.Contains(s, StringComparison.OrdinalIgnoreCase))
                    { return false; }
                }
                return true;
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
            SaveBibtexCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(Edited));
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoBibtex()
        {
            _suppressShowingEntry = true;
            UndoRedoManager.Undo();
            _suppressShowingEntry = false;
            ShowingEntry = _holdShowingEntry ?? null;
            NotifyCanUndoRedo();
        }

        private bool CanUndo() => UndoRedoManager.CanUndo && !isFiltering;

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void RedoBibtex()
        {
            _suppressShowingEntry = true;
            UndoRedoManager.Redo();
            _suppressShowingEntry = false;
            ShowingEntry = _holdShowingEntry ?? null;
            NotifyCanUndoRedo();
        }
        private bool CanRedo() => UndoRedoManager.CanRedo && !isFiltering;

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void DeleteBibtex()
        { Delete(); }
        private bool CanDelete() => ShowingEntry != null;

        [RelayCommand(CanExecute = nameof(CanDeleteKey))]
        private void DeleteBibtexKey()
        { Delete(); }
        private bool CanDeleteKey() => ShowingEntry != null 
            && UndoRedoManager.NewEditedBox == null && !isFiltering;

        private void Delete()
        {
            // 把那些东西全部删除掉
            if (SelectedIndexItems == null || SelectedIndexItems.Count == 0) { return; }
            UndoRedoManager.AddAction(new DeleteEntriesAction(BibtexEntries, SelectedIndexItems));
            _suppressShowingEntry = true;
            BibtexEntries.RemoveRange(SelectedIndexItems.Select(t => t.Item2));
            _suppressShowingEntry = false;
            ShowingEntry = _holdShowingEntry ?? null;
            NotifyCanUndoRedo();
        }

        [RelayCommand(CanExecute = nameof(Edited))]
        private async Task SaveBibtex()
        {
            using var writer = new StreamWriter(FullPath, append: false, encoding: Encoding.UTF8, bufferSize: 65536); // 缓冲区大小设置为64KB
            foreach (var entry in BibtexEntries)
            { await writer.WriteAsync(entry.BibTeX); }
            UndoRedoManager.Edited = false;
            OnPropertyChanged(nameof(Edited));
        }

        [RelayCommand]
        private static void ToLink(object o)
        {
            if (o is BibtexEntry entry)
            {
                string url = entry.DOI == "" ? entry.Url : "https://doi.org/" + entry.DOI;
                try
                { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch
                {
                    // 跨平台兼容处理
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    { Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true }); }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    { Process.Start("xdg-open", url); }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    { Process.Start("open", url); }
                    else
                    { throw; }
                }
            }
        }

        [RelayCommand]
        private async Task CopyBibtex(object sender)
        {
            if (sender is Control c)
            {
                // 通过当前控件获取UI上下文
                var clipboard = TopLevel.GetTopLevel(c)?.Clipboard;
                if (clipboard != null && ShowingEntry != null)
                { await clipboard.SetTextAsync(ShowingEntry.BibTeX); }
            }
        }
    }
}
