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
        [ObservableProperty]
        private string _header;

        [ObservableProperty]
        private string _fullPath;

        public bool Edited { get => UndoRedoManager.Edited; }

        public UndoRedoManager UndoRedoManager { get; set; }

        public ObservableCollection<WarningError> WarningErrors { get; set; }

        public string WarningHint
        {
            get
            {
                if (WarningErrors == null || WarningErrors.Count == 0)
                {
                    return string.Empty;
                }
                return $"{WarningErrors.Count} warnings or errors";
            }
        }

        [ObservableProperty]
        private int _hasError = -1;

        [ObservableProperty]
        private bool _isChecking;

        public EventHandler? CheckingEvent;

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
            get => filterText;
            set
            {
                SetProperty(ref filterText, value);
                RefreshFilter();
            }
        }

        private int filterMode = 0;
        public int FilterMode
        {
            get => filterMode;
            set
            {
                if (value < 0) { return; }  // necessary
                SetProperty(ref filterMode, value);
                RefreshFilter();
            }
        }

        private string filterField = "Whole";
        public string FilterField
        {
            get => filterField;
            set
            {
                SetProperty(ref filterField, value);
                RefreshFilter();
            }
        }

        public bool IsFiltering { get; set; }

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
            WarningErrors = null!;
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
                List<(int, BibtexEntry)> index_entries = [];
                int c = BibtexEntries.Count;
                foreach (BibtexEntry entry in BibtexParser.Parse(bibtex))
                {
                    index_entries.Add((c, entry));
                    BibtexEntries.Add(entry);
                    c++;
                }
                UndoRedoManager.AddAction(new AddEntriesAction(BibtexEntries, index_entries));
                NotifyCanUndoRedo();
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
            string target_s = filterField switch
            {
                "Author" => entry.Author,
                "Title" => entry.Title,
                "Citation Key" => entry.CitationKey,
                _ => entry.BibTeX,
            };
            // -1是特殊情形
            if (filterMode == -1)
            {  return filterText == target_s; }
            // 或
            else if (filterMode == 1)
            {
                foreach (string s in filters)
                {
                    if (!string.IsNullOrEmpty(s) && target_s.Contains(s, StringComparison.OrdinalIgnoreCase))
                    { return true; }
                }
            }
            // 且
            else
            {
                foreach (string s in filters)
                {
                    if (!string.IsNullOrEmpty(s) && !target_s.Contains(s, StringComparison.OrdinalIgnoreCase))
                    { return false; }
                }
                return true;
            }
            return false;
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

        #region Method

        private async void CheckErrors()
        {
            var result = await WarningErrorChecker.CheckBibtex(BibtexEntries);
            WarningErrors = new ObservableCollection<WarningError>(result.Item1);
            HasError = result.Item2;
            OnPropertyChanged(nameof(WarningErrors));
            OnPropertyChanged(nameof(WarningHint));
        }

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

        private void NotifyCanUndoRedo()
        {
            UndoBibtexCommand.NotifyCanExecuteChanged();
            RedoBibtexCommand.NotifyCanExecuteChanged();
            SaveBibtexCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(Edited));
            CheckErrors();
        }
        private void SetIsTailSelected()
        {
            if (UndoRedoManager.NewEditedBox is TextBox tb)
            {
                if (string.IsNullOrEmpty(tb.Text))
                { isTailSelected = 0 == selectionEnd || 0 == selectionStart; }
                else
                { isTailSelected = tb.Text.Length == selectionEnd || tb.Text.Length == selectionStart; }
            }
        }
        #endregion


        #region Command

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoBibtex()
        {
            _suppressShowingEntry = true;
            UndoRedoManager.Undo();
            _suppressShowingEntry = false;
            ShowingEntry = _holdShowingEntry ?? null;
            NotifyCanUndoRedo();
        }

        private bool CanUndo() => UndoRedoManager.CanUndo && !IsFiltering;

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void RedoBibtex()
        {
            _suppressShowingEntry = true;
            UndoRedoManager.Redo();
            _suppressShowingEntry = false;
            ShowingEntry = _holdShowingEntry ?? null;
            NotifyCanUndoRedo();
        }
        private bool CanRedo() => UndoRedoManager.CanRedo && !IsFiltering;

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void DeleteBibtex()
        { Delete(); }
        private bool CanDelete() => ShowingEntry != null;

        [RelayCommand(CanExecute = nameof(CanDeleteKey))]
        private void DeleteBibtexKey()
        { Delete(); }
        private bool CanDeleteKey() => ShowingEntry != null
            && UndoRedoManager.NewEditedBox == null && !IsFiltering;

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
        private async Task CopyBibtexText(object sender)
        {
            if (sender is Control c)
            {
                // 通过当前控件获取UI上下文
                var clipboard = TopLevel.GetTopLevel(c)?.Clipboard;
                if (clipboard != null && ShowingEntry != null)
                { await clipboard.SetTextAsync(ShowingEntry.BibTeX); }
            }
        }

        [RelayCommand]
        private void CheckWarningError(object sender)
        {
            if (sender is not WarningError we) { return; }
            if (we.Class == WarningErrorClass.SameCitationKey)
            {
                List<BibtexEntry> removed = [we.SourceEntries[0]];
                BibtexEntry toRemoved = we.SourceEntries[0];
                int minIndex = BibtexEntries.IndexOf(toRemoved);
                for (int i = 1; i < we.SourceEntries.Count; ++i)
                {
                    int n = BibtexEntries.IndexOf(we.SourceEntries[i]);
                    if (n < minIndex)
                    {
                        removed.Add(toRemoved);
                        BibtexEntries.Remove(toRemoved);
                        toRemoved = we.SourceEntries[i];
                        minIndex = n;
                    }
                    else
                    {
                        removed.Add(we.SourceEntries[i]);
                        BibtexEntries.Remove(we.SourceEntries[i]);
                    }
                }
                for (int i = 1; i < we.SourceEntries.Count; ++i)
                {
                    BibtexEntries.Insert(minIndex + i, removed[i]);
                }
                string tempText = FilterText;
                int tempMode = FilterMode;
                string tempField = FilterField;
                filterText = we.FieldName;
                filterMode = -1;
                filterField = "Citation Key";
                RefreshFilter();
                CheckingEvent?.Invoke(null, EventArgs.Empty);
                FilterText = tempText;
                FilterMode = tempMode;
                FilterField = tempField;
                RefreshFilter();
                ShowingEntry = toRemoved;
            }
            else
            {
                ShowingEntry = we.SourceEntries[0];
            }
            IsChecking = false;
        }


        [RelayCommand]
        private async Task CopyBibtex(object? sender)
        {
            if (SelectedIndexItems == null || sender is not MainWindowViewModel mwvm
                || IsFiltering || UndoRedoManager.NewEditedBox != null)
            { return; }
            await Task.Run(() =>
            {
                var list = SelectedIndexItems.Select(t => t.Item2);
                mwvm.CopiedBibtex = [];
                foreach (var item in list)
                {
                    if (item is BibtexEntry entry)
                    {
                        BibtexEntry e = new();
                        e.CopyFromBibtex(entry);
                        mwvm.CopiedBibtex.Add(e);
                    }
                }
            });
            Debug.WriteLine($"copied {mwvm.CopiedBibtex.Count} entries");
        }

        [RelayCommand]
        private void PasteBibtex(object? sender)
        {
            if (sender is not MainWindowViewModel mwvm
                || IsFiltering || UndoRedoManager.NewEditedBox != null)
            { return; }
            int c = ShowingEntry != null ? BibtexEntries.IndexOf(ShowingEntry) + 1 : BibtexEntries.Count;
            List<(int, BibtexEntry)> index_entries = [];
            foreach (var entry in mwvm.CopiedBibtex)
            {
                BibtexEntry e = new();
                e.CopyFromBibtex(entry);
                index_entries.Add((c, e));
                BibtexEntries.Insert(c, e);
                c++;
            }
            UndoRedoManager.AddAction(new AddEntriesAction(BibtexEntries, index_entries));
            NotifyCanUndoRedo();
        }
        #endregion
    }
}
