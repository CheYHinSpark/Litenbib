using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExCSS;
using Litenbib.Models;
using Litenbib.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
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

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasExternalChanges;

        public DateTime? LastDiskWriteTimeUtc { get; private set; }

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

        public event EventHandler<BibtexEntry>? FocusEntryRequested;

        public ObservableRangeCollection<BibtexEntry> BibtexEntries { get; set; }

        public DataGridCollectionView BibtexView { get; }

        private int _statusVersion = 0;

        private int _diagnosticsVersion = 0;

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
            LastDiskWriteTimeUtc = File.Exists(FullPath) ? File.GetLastWriteTimeUtc(FullPath) : null;
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
            CheckErrorsNow();
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
                NotifyCanUndoRedo(debounceDiagnostics: true);
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

            var result = await dialog.ShowDialog<bool>(window);
            if (result == true && dialog.DataContext is AddEntryViewModel aevm)
            {
                string bibtex = aevm.BibtexText;
                List<(int, BibtexEntry)> index_entries = [];
                List<BibtexEntry> addedEntries = [];
                int c = BibtexEntries.Count;
                foreach (BibtexEntry entry in BibtexParser.Parse(bibtex))
                {
                    index_entries.Add((c, entry));
                    addedEntries.Add(entry);
                    BibtexEntries.Add(entry);
                    entry.UndoRedoPropertyChanged += OnEntryPropertyChanged;
                    c++;
                }
                if (addedEntries.Count == 0)
                {
                    NotificationCenter.Info("No BibTeX entries were added");
                    return;
                }
                UndoRedoManager.AddAction(new AddEntriesAction(BibtexEntries, index_entries));
                NotifyCanUndoRedo();
                FocusFirstVisibleAddedEntry(addedEntries);
            }
        }

        private void FocusFirstVisibleAddedEntry(IReadOnlyList<BibtexEntry> addedEntries)
        {
            BibtexEntry? entryToFocus = addedEntries.FirstOrDefault(FilterBibtex);
            if (entryToFocus == null)
            {
                NotificationCenter.Info("Added entry, but it is hidden by the current filter");
                return;
            }

            ShowingEntry = entryToFocus;
            FocusEntryRequested?.Invoke(this, entryToFocus);
        }

        public bool RefreshGeneratedBibtex()
        {
            bool changed = false;
            foreach (var entry in BibtexEntries)
            {
                string oldBibtex = entry.BibTeX;
                entry.UpdateBibtex(isSilent: true);
                if (!string.Equals(oldBibtex, entry.BibTeX, StringComparison.Ordinal))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                UndoRedoManager.Edited = true;
                NotifyCanUndoRedo();
            }

            return changed;
        }

        public void ShowStatus(string message)
        {
            int version = ++_statusVersion;
            StatusMessage = message;
            _ = ClearStatusLaterAsync(version);
        }

        private async Task ClearStatusLaterAsync(int version)
        {
            await Task.Delay(3000);
            if (version == _statusVersion && !string.IsNullOrWhiteSpace(StatusMessage))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (version == _statusVersion)
                    {
                        StatusMessage = string.Empty;
                    }
                });
            }
        }

        private static async Task ShowMessage(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowAsync();
        }

        private async Task<bool> ConfirmOverwriteExternalChangesAsync(string validatedPath)
        {
            if (!IsCurrentFilePath(validatedPath) || !DetectExternalModification())
            {
                return true;
            }

            var box = MessageBoxManager.GetMessageBoxStandard(
                "File Changed On Disk",
                "This file has changed on disk since it was opened or last saved. Saving now will overwrite those external changes.\n\nDo you want to overwrite the disk file?",
                ButtonEnum.YesNo);
            var result = await box.ShowAsync();
            if (result == ButtonResult.Yes)
            {
                return true;
            }

            NotificationCenter.Info("Save canceled: file changed on disk");
            return false;
        }

        private bool IsCurrentFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(FullPath))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    Path.GetFullPath(path),
                    Path.GetFullPath(FullPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryGetValidatedSavePath(string? path, out string validatedPath, out string errorMessage)
        {
            validatedPath = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "The file path is empty.";
                return false;
            }

            try
            {
                validatedPath = Path.GetFullPath(path.Trim());
            }
            catch (Exception)
            {
                errorMessage = "The file path is invalid.";
                return false;
            }

            string? directory = Path.GetDirectoryName(validatedPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                errorMessage = "The target directory is invalid.";
                return false;
            }

            if (!string.Equals(Path.GetExtension(validatedPath), ".bib", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Only .bib files can be saved.";
                return false;
            }

            return true;
        }

        public bool DetectExternalModification()
        {
            if (string.IsNullOrWhiteSpace(FullPath) || !File.Exists(FullPath) || LastDiskWriteTimeUtc == null)
            {
                HasExternalChanges = false;
                return false;
            }

            var currentWriteTime = File.GetLastWriteTimeUtc(FullPath);
            HasExternalChanges = currentWriteTime > LastDiskWriteTimeUtc.Value.AddMilliseconds(1);
            return HasExternalChanges;
        }

        public async Task ReloadFromDiskAsync()
        {
            if (string.IsNullOrWhiteSpace(FullPath) || !File.Exists(FullPath))
            {
                await ShowMessage("Reload Failed", "The source file no longer exists.");
                return;
            }

            string fileContent = await File.ReadAllTextAsync(FullPath);
            var parsed = BibtexParser.Parse(fileContent);
            foreach (var entry in BibtexEntries)
            {
                entry.UndoRedoPropertyChanged -= OnEntryPropertyChanged;
            }
            BibtexEntries.Clear();
            foreach (var entry in parsed)
            {
                BibtexEntries.Add(entry);
                entry.UndoRedoPropertyChanged += OnEntryPropertyChanged;
            }
            UndoRedoManager = new();
            HasExternalChanges = false;
            LastDiskWriteTimeUtc = File.GetLastWriteTimeUtc(FullPath);
            ShowingEntry = BibtexEntries.FirstOrDefault();
            NotifyCanUndoRedo();
            ShowStatus("Reloaded from disk");
        }

        [RelayCommand]
        private async Task ReloadFromDisk()
        {
            await ReloadFromDiskAsync();
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

        private void ScheduleCheckErrors()
        {
            int version = ++_diagnosticsVersion;
            _ = CheckErrorsLaterAsync(version);
        }

        private void CheckErrorsNow()
        {
            int version = ++_diagnosticsVersion;
            _ = CheckErrorsAsync(version);
        }

        private async Task CheckErrorsLaterAsync(int version)
        {
            await Task.Delay(500);
            if (version != _diagnosticsVersion)
            {
                return;
            }

            await CheckErrorsAsync(version);
        }

        private async Task CheckErrorsAsync(int version)
        {
            var result = await WarningErrorChecker.CheckBibtex(BibtexEntries);
            if (version != _diagnosticsVersion)
            {
                return;
            }

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

        private void NotifyCanUndoRedo(bool debounceDiagnostics = false)
        {
            UndoBibtexCommand.NotifyCanExecuteChanged();
            RedoBibtexCommand.NotifyCanExecuteChanged();
            SaveBibtexCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(Edited));
            if (debounceDiagnostics)
            {
                ScheduleCheckErrors();
            }
            else
            {
                CheckErrorsNow();
            }
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
            else if (UndoRedoManager.NewEditedBox is IUndoRedoTextHost textHost)
            {
                if (textHost.TextLength == 0)
                { isTailSelected = 0 == selectionEnd || 0 == selectionStart; }
                else
                { isTailSelected = textHost.TextLength == selectionEnd || textHost.TextLength == selectionStart; }
            }
        }

        private static string GetPropertyNameForField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return fieldName;
            }

            return fieldName.ToLowerInvariant() switch
            {
                "doi" => "DOI",
                "isbn" => "ISBN",
                "issn" => "ISSN",
                "url" => "Url",
                _ => char.ToUpperInvariant(fieldName[0]) + fieldName[1..]
            };
        }
        #endregion Method

        #region Command
        public async Task<bool> SaveCurrentAsync()
        {
            return await SaveBibtexToPath(FullPath);
        }

        private async Task<bool> SaveBibtexToPath(string targetPath)
        {
            if (!TryGetValidatedSavePath(targetPath, out string validatedPath, out string errorMessage))
            {
                NotificationCenter.Error(errorMessage);
                await ShowMessage("Save Failed", errorMessage);
                return false;
            }

            if (!await ConfirmOverwriteExternalChangesAsync(validatedPath))
            {
                return false;
            }

            Debug.WriteLine($"Saving...{validatedPath}");
            try
            {
                string? directory = Path.GetDirectoryName(validatedPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var writer = new StreamWriter(validatedPath, append: false, new UTF8Encoding(false), bufferSize: 65536);
                foreach (var entry in BibtexEntries)
                {
                    await writer.WriteAsync(entry.BibTeX + "\n");
                }
                FullPath = validatedPath;
                Header = Path.GetFileName(validatedPath);
                LastDiskWriteTimeUtc = File.GetLastWriteTimeUtc(validatedPath);
                HasExternalChanges = false;
                UndoRedoManager.Edited = false;
                OnPropertyChanged(nameof(Edited));
                SaveBibtexCommand.NotifyCanExecuteChanged();
                ShowStatus($"Saved {Header}");
                return true;
            }
            catch (Exception ex)
            {
                NotificationCenter.Error($"Could not save {Path.GetFileName(validatedPath)}: {ex.Message}");
                await ShowMessage("Save Failed", $"Could not save file.\n{ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveBibtexAs(Window window)
        {
            string suggestedFileName = string.IsNullOrWhiteSpace(FullPath)
                ? "library.bib"
                : Path.GetFileName(FullPath);

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save BibTeX File As",
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "bib",
                ShowOverwritePrompt = true,
                FileTypeChoices =
                [
                    new FilePickerFileType("BibTeX Files")
                    {
                        Patterns = ["*.bib"]
                    },
                    FilePickerFileTypes.All
                ]
            });

            if (file == null) { return false; }
            return await SaveBibtexToPath(Uri.UnescapeDataString(file.Path.AbsolutePath));
        }

        [RelayCommand(CanExecute = nameof(Edited))]
        private async Task SaveBibtex()
        {
            await SaveCurrentAsync();
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
            await OpenMergeCandidatesDialog(sender, MergeSearchSource.Super);
        }

        [RelayCommand]
        private async Task FindMergeByDoi(object? sender)
        {
            await OpenMergeCandidatesDialog(sender, MergeSearchSource.Doi);
        }

        [RelayCommand]
        private async Task FindMergeByDblp(object? sender)
        {
            await OpenMergeCandidatesDialog(sender, MergeSearchSource.Dblp);
        }

        [RelayCommand]
        private async Task FindMergeByCrossref(object? sender)
        {
            await OpenMergeCandidatesDialog(sender, MergeSearchSource.Crossref);
        }

        [RelayCommand]
        private async Task FindMergeByTitle(object? sender)
        {
            await OpenMergeCandidatesDialog(sender, MergeSearchSource.Title);
        }

        private async Task OpenMergeCandidatesDialog(object? sender, MergeSearchSource source)
        {
            if (ShowingEntry == null || sender is not MainWindow window) { return; }
            var targetEntry = ShowingEntry;
            string sourceName = GetMergeSearchSourceName(source);
            NotificationCenter.Info($"Searching {sourceName}...");
            var list = await LinkResolver.SearchMergeCandidatesAsync(targetEntry, source);
            if (list.Count == 0)
            {
                NotificationCenter.Info($"No {sourceName} candidates found");
                return;
            }

            int targetIndex = BibtexEntries.IndexOf(targetEntry);
            if (targetIndex < 0)
            {
                NotificationCenter.Error("Search result ignored: the original entry was removed");
                return;
            }

            NotificationCenter.Info($"Found {list.Count} {sourceName} candidate(s)");
            list.Insert(0, targetEntry);
            CompareEntryView dialog = new(list);
            var result = await dialog.ShowDialog<bool>(window);
            if (result == true)
            {
                if (dialog.DataContext is not CompareEntryViewModel cevm) { return; }
                targetIndex = BibtexEntries.IndexOf(targetEntry);
                if (targetIndex < 0)
                {
                    NotificationCenter.Error("Merge canceled: the original entry was removed");
                    return;
                }

                var oldEntry = targetEntry;
                BibtexEntries.Insert(targetIndex, cevm.MergedEntry);
                cevm.MergedEntry.UndoRedoPropertyChanged += OnEntryPropertyChanged;
                BibtexEntries.Remove(oldEntry);
                UndoRedoManager.AddAction(new ReplaceEntriesAction(BibtexEntries, [(targetIndex, oldEntry)], [(targetIndex, cevm.MergedEntry)]));
                ShowingEntry = cevm.MergedEntry;
                NotifyCanUndoRedo();
            }
        }

        private static string GetMergeSearchSourceName(MergeSearchSource source)
        {
            return source switch
            {
                MergeSearchSource.Doi => "DOI",
                MergeSearchSource.Dblp => "DBLP",
                MergeSearchSource.Crossref => "Crossref",
                MergeSearchSource.Title => "title",
                _ => "bibliography"
            };
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

        [RelayCommand]
        private async Task ApplyBatchFieldEdit(object? sender)
        {
            if (sender is not MainWindow window || SelectedIndexItems == null || SelectedIndexItems.Count == 0) { return; }
            BatchFieldEditView dialog = new();
            var result = await dialog.ShowDialog<bool>(window);
            if (result != true || dialog.DataContext is not BatchFieldEditViewModel vm) { return; }

            List<EntryFieldChange> changes = [];
            foreach (var (_, entry) in SelectedIndexItems)
            {
                var change = vm.CreateChange(entry);
                if (change == null)
                {
                    continue;
                }

                changes.Add(change.Value);
                entry.SetValueSilent(change.Value.PropertyName, change.Value.NewValue);
            }

            EntryFieldsChangeAction action = new(changes);
            if (!action.HasChanges)
            {
                ShowStatus("No selected entries changed");
                return;
            }
            UndoRedoManager.AddAction(action);
            NotifyCanUndoRedo();
            ShowStatus($"Updated {SelectedIndexItems.Count} selected entries");
        }

        [RelayCommand]
        private void CleanupSelectedEntries()
        {
            if (SelectedIndexItems == null || SelectedIndexItems.Count == 0) { return; }
            List<EntryFieldChange> changes = [];
            foreach (var (_, entry) in SelectedIndexItems)
            {
                changes.AddRange(CleanupEntry(entry));
            }

            EntryFieldsChangeAction action = new(changes);
            if (!action.HasChanges)
            {
                ShowStatus("No selected entries needed cleanup");
                return;
            }
            UndoRedoManager.AddAction(action);
            NotifyCanUndoRedo();
            ShowStatus($"Cleaned {SelectedIndexItems.Count} selected entries");
        }

        private static List<EntryFieldChange> CleanupEntry(BibtexEntry entry)
        {
            List<EntryFieldChange> changes = [];
            var keys = entry.Fields.Keys.ToList();
            foreach (var key in keys)
            {
                var oldValue = entry.Fields[key];
                var value = oldValue;
                value = value.Replace("\r", " ").Replace("\n", " ").Trim();
                while (value.Contains("  "))
                {
                    value = value.Replace("  ", " ");
                }

                if (key.Equals("doi", StringComparison.OrdinalIgnoreCase))
                {
                    value = BibtexDiagnostics.NormalizeDoi(value);
                }
                if (key.Equals("url", StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Trim();
                }

                string? newValue = string.IsNullOrWhiteSpace(value) ? null : value;
                if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                {
                    continue;
                }

                string propertyName = GetPropertyNameForField(key);
                changes.Add(new EntryFieldChange(entry, propertyName, oldValue, newValue));
                entry.SetValueSilent(propertyName, newValue);
            }
            return changes;
        }

        [RelayCommand]
        private void OpenInFileManager()
        {
            string? directory = Path.GetDirectoryName(FullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                UriProcessor.StartProcess(directory);
            }
        }
        #endregion Command
    }
}
