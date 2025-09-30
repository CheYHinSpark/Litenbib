using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExCSS;
using Litenbib.Models;
using Litenbib.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class BibtexViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _header;

        [ObservableProperty]
        private string _fullPath;

        public bool Edited { get => UndoRedoManager.Edited; }

        public UndoRedoManager UndoRedoManager { get; set; }

        public ObservableCollection<WarningError> Warnings { get; set; }

        public string WarningHint
        { get { return Warnings.Count == 0 ? string.Empty : $"{Warnings.Count} warnings or errors"; } }

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
            get => ["Article", "Book", "Booklet", "Conference", "InBook", "InCollection", "InProceedings",
                "Manual", "MastersThesis", "Misc", "PhdThesis", "Proceedings", "TechReport", "Unpublished"];
        }

        public BibtexViewModel(string header, string fullPath, string filecontent, int filterMode = 0)
        {
            Header = Uri.UnescapeDataString(header);
            FullPath = Uri.UnescapeDataString(fullPath);
            BibtexEntries = new ObservableRangeCollection<BibtexEntry>(BibtexParser.Parse(filecontent));
            Warnings = [];
            foreach (var entry in BibtexEntries)
            { entry.UndoRedoPropertyChanged += OnEntryPropertyChanged; }
            BibtexView = new(BibtexEntries)
            {
                Filter = entry => FilterBibtex(entry as BibtexEntry)
            };
            _holdShowingEntry = null!;
            UndoRedoManager = new();
            FilterMode = filterMode;
            CheckErrors();
        }

        #region Event
        private async void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当前观察：在TextBox中操作之后如果鼠标在最后，会先改变NewSelectionStart
            // 如果不是在最后，会先改变TextBox.Selected，之后才Delay才
            if (sender is not BibtexEntry item || e.PropertyName == null) { return; }
            if (e is PropertyChangedEventArgsEx extendedArgs)
            {
                // 创建并添加操作到管理器
                if (isTailSelected)
                {
                    UndoRedoManager.AddAction(new EntryChangeAction(item, e.PropertyName,
                        (string?)extendedArgs.OldValue, (string?)extendedArgs.NewValue,
                        int.Max(oldSelectionStart, oldSelectionEnd), selectionStart));
                }
                else
                {
                    int oldEnd = selectionEnd;
                    await Task.Delay(1);
                    UndoRedoManager.AddAction(new EntryChangeAction(item, e.PropertyName,
                        (string?)extendedArgs.OldValue, (string?)extendedArgs.NewValue,
                        oldEnd, selectionStart));
                }
                NotifyCanUndoRedo();
            }
        }
        #endregion Event

        #region Method
        public async Task ExtractPdf(string pdfFile)
        {
            var x = PdfMetadataExtractor.Extract(pdfFile);
            await Task.Delay(1000);
            Debug.WriteLine(x.Doi);
            Debug.WriteLine(x.ArxivId);
        }

        public async Task AddBibtexEntry(Window window)
        {
            AddEntryView dialog = new();

            // 显示对话框并等待结果 (ShowDialog 需要传入父窗口引用)
            var result = await dialog.ShowDialog<bool>(window); // 等待对话框关闭并获取 DialogResult
            // 如果用户点击了 OK
            if (result == true && dialog.DataContext is AddEntryViewModel aevm)
            {
                // 通过对话框的公共属性获取返回值
                string bibtex = aevm.BibtexText;
                List<(int, BibtexEntry)> index_entries = [];
                int c = BibtexEntries.Count;
                foreach (BibtexEntry entry in BibtexParser.Parse(bibtex))
                {
                    index_entries.Add((c, entry));
                    BibtexEntries.Add(entry);
                    entry.UndoRedoPropertyChanged += OnEntryPropertyChanged;
                    c++;
                }
                UndoRedoManager.AddAction(new AddEntriesAction(BibtexEntries, index_entries));
                NotifyCanUndoRedo();
            }
        }

        private void RefreshFilter()
        {
            if (string.IsNullOrWhiteSpace(filterText))
            { filters = []; }
            else
            { filters = filterMode == 2 ? [filterText] : filterText.Split(' '); }
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
            { return filterText == target_s; }
            // 或 且
            else
            {
                bool b = filterMode == 1;
                foreach (string s in filters)
                {
                    if (!string.IsNullOrEmpty(s) && target_s.Contains(s, StringComparison.OrdinalIgnoreCase) == b)
                    { return b; }
                }
                return !b;
            }
        }

        private async void CheckErrors()
        {
            var result = await WarningErrorChecker.CheckBibtex(BibtexEntries);
            Warnings = new ObservableCollection<WarningError>(result.Item1);
            HasError = result.Item2;
            OnPropertyChanged(nameof(Warnings));
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
        #endregion Method

        #region Command
        [RelayCommand(CanExecute = nameof(Edited))]
        private async Task SaveBibtex()
        {
            Debug.WriteLine($"Saving...{FullPath}");
            using var writer = new StreamWriter(FullPath, append: false, new UTF8Encoding(false), bufferSize: 65536); // 缓冲区大小设置为64KB
            foreach (var entry in BibtexEntries)
            { await writer.WriteAsync(entry.BibTeX + "\n"); }
            UndoRedoManager.Edited = false;
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
            { ShowingEntry = we.SourceEntries[0]; }
            IsChecking = false;
        }

        [RelayCommand(CanExecute = nameof(CanCopyPasteCutBibtex))]
        private async Task CopyBibtex(object? sender)
        {
            if (SelectedIndexItems != null && sender is MainWindowViewModel mwvm)
            { await mwvm.CopyBibtexEntries(SelectedIndexItems.Select(t => t.Item2)); }
        }

        [RelayCommand(CanExecute = nameof(CanCopyPasteCutBibtex))]
        private void PasteBibtex(object? sender)
        {
            if (sender is not MainWindowViewModel mwvm)
            { return; }
            int c = ShowingEntry != null ? BibtexEntries.IndexOf(ShowingEntry) + 1 : BibtexEntries.Count;
            List<(int, BibtexEntry)> index_entries = [];
            foreach (var entry in mwvm.CopiedBibtex)
            {
                BibtexEntry e = BibtexEntry.CopyFrom(entry);
                index_entries.Add((c, e));
                BibtexEntries.Insert(c, e);
                c++;
            }
            UndoRedoManager.AddAction(new AddEntriesAction(BibtexEntries, index_entries));
            NotifyCanUndoRedo();
        }

        [RelayCommand(CanExecute = nameof(CanCopyPasteCutBibtex))]
        private void CutBibtex(object? sender)
        {
            if (SelectedIndexItems != null && sender is MainWindowViewModel mwvm)
            {
                mwvm.CopiedBibtex = [.. SelectedIndexItems.Select(t => BibtexEntry.CopyFrom(t.Item2))];
                Delete();
            }
        }

        private bool CanCopyPasteCutBibtex() => !(IsFiltering || UndoRedoManager.NewEditedBox != null);

        [RelayCommand]
        private async Task GetDblpFromDoi(object? sender)
        {
            if (ShowingEntry == null || sender is not MainWindow window) { return; }
            //await LinkResolver.GetDblpFromDoi(ShowingEntry.DOI);
            var list = await LinkResolver.GetDblpFromTitle(ShowingEntry.Title);
            if (list.Count == 0) { return; }
            Debug.WriteLine("accepted");
            list.Insert(0, ShowingEntry);
            CompareEntryView dialog = new(list);
            // 显示对话框并等待结果 (ShowDialog 需要传入父窗口引用)
            var result = await dialog.ShowDialog<bool>(window); // 等待对话框关闭并获取 DialogResult
            if (result == true)
            {
                if (dialog.DataContext is not CompareEntryViewModel cevm) { return; }
                int i = BibtexEntries.IndexOf(ShowingEntry);
                var oldEntry = ShowingEntry;
                BibtexEntries.Insert(i, cevm.MergedEntry);
                cevm.MergedEntry.UndoRedoPropertyChanged += OnEntryPropertyChanged;
                BibtexEntries.Remove(oldEntry);
                UndoRedoManager.AddAction(new ReplaceEntriesAction(BibtexEntries, [(i, oldEntry)], [(i, cevm.MergedEntry)]));
                ShowingEntry = cevm.MergedEntry;
                NotifyCanUndoRedo();
            }
        }

        [RelayCommand]
        private async Task MergeEntries(object? sender)
        {
            if (sender is not MainWindow window) { return; }
            if (SelectedIndexItems == null || SelectedIndexItems.Count < 2) { return; }
            var list = SelectedIndexItems.Select(t => t.Item2).ToList();
            CompareEntryView dialog = new(list);
            // 显示对话框并等待结果 (ShowDialog 需要传入父窗口引用)
            var result = await dialog.ShowDialog<bool>(window); // 等待对话框关闭并获取 DialogResult
            if (result == true)
            {
                if (dialog.DataContext is not CompareEntryViewModel cevm) { return; }
                int i = BibtexEntries.IndexOf(list[0]);
                List<(int, BibtexEntry)> removed = [];
                foreach (var entry in list)
                {
                    removed.Add((BibtexEntries.IndexOf(entry), entry));
                    BibtexEntries.Remove(entry);
                }
                BibtexEntries.Insert(i, cevm.MergedEntry);
                cevm.MergedEntry.UndoRedoPropertyChanged += OnEntryPropertyChanged;
                UndoRedoManager.AddAction(new ReplaceEntriesAction(BibtexEntries, removed, [(i, cevm.MergedEntry)]));
                ShowingEntry = cevm.MergedEntry;
                NotifyCanUndoRedo();
            }
        }
        #endregion Command
    }
}
