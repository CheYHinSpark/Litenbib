using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input.Platform;
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
        private string _statusMessageTag = "hide";

        [ObservableProperty]
        private bool _hasExternalChanges;

        public DateTime? LastDiskWriteTimeUtc { get; private set; }

        public bool Edited { get => UndoRedoManager.Edited; }

        public UndoRedoManager UndoRedoManager { get; set; }

        public ObservableCollection<WarningError> Warnings { get; set; }

        public string WarningHint
        { get { return Warnings.Count == 0 ? string.Empty : I18n.Format("Warning.Hint", Warnings.Count); } }

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
                    DeleteBibtexByKeyCommand.NotifyCanExecuteChanged();
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
                OnPropertyChanged(nameof(FilterFieldOption));
                RefreshFilter();
            }
        }

        public LocalizedOption FilterFieldOption
        {
            get => LocalizationManager.GetFilterFieldOption(FilterField);
            set
            {
                if (value != null)
                {
                    FilterField = value.Value;
                }
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
            : this(header, fullPath, BibtexParser.Parse(filecontent), filterMode)
        {
        }

        public BibtexViewModel(string header, string fullPath, IEnumerable<BibtexEntry> entries, int filterMode = 0)
        {
            Header = Uri.UnescapeDataString(header);
            FullPath = Uri.UnescapeDataString(fullPath);
            LastDiskWriteTimeUtc = File.Exists(FullPath) ? File.GetLastWriteTimeUtc(FullPath) : null;
            BibtexEntries = new ObservableRangeCollection<BibtexEntry>(entries);
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

        public void RefreshLocalizedText()
        {
            OnPropertyChanged(nameof(FilterFieldOption));
            OnPropertyChanged(nameof(WarningHint));
            foreach (var warning in Warnings)
            {
                _ = warning.HintString;
            }
            OnPropertyChanged(nameof(Warnings));
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
            if (PdfMetadataExtractor.TryResolvePdfFullPath(pdfFile) is not string fullPath)
            { return; }
            string fileName = Path.GetFileName(fullPath);

            ShowStatus(I18n.Format("Message.ReadingFile", fileName));
            PdfImportResult pdfImport = await Task.Run(() => PdfMetadataExtractor.ExtractImportResult(fullPath));

            BibtexEntry? entry = null;
            bool resolvedByAi = false;
            if (!string.IsNullOrWhiteSpace(pdfImport.LookupQuery))
            {
                var resolvedEntries = await LinkResolver.ResolveEntriesAsync(pdfImport.LookupQuery, maxCandidates: 1);
                entry = resolvedEntries.FirstOrDefault();
            }
            else
            { NotificationCenter.Info(I18n.Format("Message.NoDoiOrArxivFound", fileName)); }

            if (entry == null && AppSettingsState.Current.UseAiPdfImportFallback)
            {
                ShowStatus(I18n.Format("Message.AskingAiToRead", fileName));
                entry = await AiBibtexExtractor.ExtractFromPdfFirstPageAsync(pdfImport.RawText);
                if (entry != null)
                {
                    resolvedByAi = true;
                    NotificationCenter.Info(I18n.Format("Message.AiExtractedMetadata", fileName));
                }
            }

            if (entry == null)
            {
                entry = pdfImport.CreateFallbackEntry();
                NotificationCenter.Info(I18n.Format("Message.AddedPlaceholderEntry", fileName));
            }
            else if (!resolvedByAi)
            { NotificationCenter.Info(I18n.Format("Message.ResolvedMetadata", fileName)); }

            pdfImport.PrepareEntry(entry);
            await Dispatcher.UIThread.InvokeAsync(() => AddImportedPdfEntry(entry));
            ShowStatus(I18n.Format("Message.ImportedFile", fileName));
        }

        private void AddImportedPdfEntry(BibtexEntry entry)
        {
            int index = BibtexEntries.Count;
            BibtexEntries.Add(entry);
            entry.UndoRedoPropertyChanged += OnEntryPropertyChanged;
            UndoRedoManager.AddAction(new AddEntriesAction(BibtexEntries, [(index, entry)]));
            NotifyCanUndoRedo();
            FocusFirstVisibleAddedEntry([entry]);
        }

        private void FocusFirstVisibleAddedEntry(IReadOnlyList<BibtexEntry> addedEntries)
        {
            BibtexEntry? entryToFocus = addedEntries.FirstOrDefault(FilterBibtex);
            if (entryToFocus == null)
            {
                NotificationCenter.Info(I18n.Get("Message.AddedEntryHiddenByFilter"));
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
                { changed = true; }
            }

            if (changed)
            {
                UndoRedoManager.Edited = true;
                NotifyCanUndoRedo();
            }

            return changed;
        }

        public void ApplyUndoRedoLimit()
        {
            UndoRedoManager.ApplyLimit();
            NotifyCanUndoRedo();
        }

        public void ShowStatus(string message)
        {
            int version = ++_statusVersion;
            _ = Dispatcher.UIThread.InvokeAsync(() => StatusMessageTag = "hide");
            StatusMessage = message;
            _ = Dispatcher.UIThread.InvokeAsync(() => StatusMessageTag = "show");
            _ = ClearStatusLaterAsync(version);
        }

        private async Task ClearStatusLaterAsync(int version)
        {
            await Task.Delay(3000);
            if (version == _statusVersion && !string.IsNullOrWhiteSpace(StatusMessage))
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (version == _statusVersion)
                    {
                        StatusMessageTag = "hiding";
                        await Task.Delay(200);
                        StatusMessage = string.Empty;
                        StatusMessageTag = "hide";
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
            { return true; }

            var box = MessageBoxManager.GetMessageBoxStandard(
                I18n.Get("Dialog.FileChangedOnDisk.Title"),
                I18n.Get("Message.FileChangedOnDisk"),
                ButtonEnum.YesNo);
            var result = await box.ShowAsync();
            if (result == ButtonResult.Yes)
            { return true; }

            NotificationCenter.Info(I18n.Get("Message.SaveCanceledExternalChange"));
            return false;
        }

        private static async Task<bool> ConfirmSelectedEntryBatchOperationAsync(string title, string message)
        {
            if (!AppSettingsState.Current.RequireBatchOperationConfirmation)
            { return true; }

            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.YesNo);
            return await box.ShowAsync() == ButtonResult.Yes;
        }

        private bool IsCurrentFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(FullPath))
            { return false; }

            try
            {
                return string.Equals(
                    Path.GetFullPath(path),
                    Path.GetFullPath(FullPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            { return false; }
        }

        private static bool TryGetValidatedSavePath(string? path, out string validatedPath, out string errorMessage)
        {
            validatedPath = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = I18n.Get("Save.Validation.EmptyPath");
                return false;
            }

            try
            {
                validatedPath = Path.GetFullPath(path.Trim());
            }
            catch (Exception)
            {
                errorMessage = I18n.Get("Save.Validation.InvalidPath");
                return false;
            }

            string? directory = Path.GetDirectoryName(validatedPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                errorMessage = I18n.Get("Save.Validation.InvalidDirectory");
                return false;
            }

            if (!string.Equals(Path.GetExtension(validatedPath), ".bib", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = I18n.Get("Save.Validation.MustUseBib");
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
                await ShowMessage(I18n.Get("Dialog.ReloadFailed.Title"), I18n.Get("Message.SourceFileMissing"));
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
            ShowStatus(I18n.Get("Message.ReloadedFromDisk"));
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
            if (version != _diagnosticsVersion) { return; }

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

        private void ApplyEntryFieldsChangeAction(EntryFieldsChangeAction action)
        {
            action.Apply();
            UndoRedoManager.AddAction(action);
            NotifyCanUndoRedo();
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

        #endregion Method

        #region Command
        [RelayCommand]
        private async Task AddBibtexEntry(object o)
        {
            if (o is not Window window) { return; }
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
                    NotificationCenter.Info(I18n.Get("Message.NoBibtexEntriesAdded"));
                    return;
                }
                UndoRedoManager.AddAction(new AddEntriesAction(BibtexEntries, index_entries));
                NotifyCanUndoRedo();
                FocusFirstVisibleAddedEntry(addedEntries);
            }
        }

        public async Task<bool> SaveCurrentAsync()
        {
            return await SaveBibtexToPath(FullPath);
        }

        private async Task<bool> SaveBibtexToPath(string targetPath)
        {
            if (!TryGetValidatedSavePath(targetPath, out string validatedPath, out string errorMessage))
            {
                NotificationCenter.Error(errorMessage);
                await ShowMessage(I18n.Get("Dialog.SaveFailed.Title"), errorMessage);
                return false;
            }

            if (!await ConfirmOverwriteExternalChangesAsync(validatedPath))
            {
                return false;
            }

            Debug.WriteLine($"Saving...{validatedPath}");
            try
            {
                await WriteBibtexFileAsync(validatedPath);
                MarkSaved(validatedPath);
                ShowStatus(I18n.Format("Message.SavedFile", Header));
                return true;
            }
            catch (Exception ex)
            {
                NotificationCenter.Error(I18n.Format("Message.CouldNotSaveNamedFile", Path.GetFileName(validatedPath), ex.Message));
                await ShowMessage(
                    I18n.Get("Dialog.SaveFailed.Title"),
                    I18n.Format("Message.CouldNotSaveFile", ex.Message));
                return false;
            }
        }

        private async Task WriteBibtexFileAsync(string validatedPath)
        {
            string? directory = Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var writer = new StreamWriter(validatedPath, append: false, new UTF8Encoding(false), bufferSize: 65536);
            foreach (var entry in BibtexEntries)
            {
                await writer.WriteAsync(entry.BibTeX + "\n");
            }
        }

        private void MarkSaved(string validatedPath)
        {
            FullPath = validatedPath;
            Header = Path.GetFileName(validatedPath);
            LastDiskWriteTimeUtc = File.GetLastWriteTimeUtc(validatedPath);
            HasExternalChanges = false;
            UndoRedoManager.Edited = false;
            OnPropertyChanged(nameof(Edited));
            SaveBibtexCommand.NotifyCanExecuteChanged();
        }

        public async Task<bool> SaveBibtexAs(Window window)
        {
            string suggestedFileName = string.IsNullOrWhiteSpace(FullPath)
                ? "library.bib"
                : Path.GetFileName(FullPath);

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = I18n.Get("Picker.SaveBibtexFileAs"),
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "bib",
                ShowOverwritePrompt = true,
                FileTypeChoices =
                [
                    new FilePickerFileType(I18n.Get("FileType.BibtexFiles"))
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

        [RelayCommand(CanExecute = nameof(CanDeleteByKey))]
        private void DeleteBibtexByKey()
        { Delete(); }
        private bool CanDeleteByKey() => ShowingEntry != null
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
            string sourceName = source switch
            {
                MergeSearchSource.Doi => "DOI",
                MergeSearchSource.Dblp => "DBLP",
                MergeSearchSource.Crossref => "Crossref",
                MergeSearchSource.Title => "title",
                _ => "bibliography"
            };
            NotificationCenter.Info(I18n.Format("Message.SearchingSource", sourceName));
            var list = await LinkResolver.SearchMergeCandidatesAsync(targetEntry, source);
            if (list.Count == 0)
            {
                NotificationCenter.Info(I18n.Format("Message.NoSourceCandidatesFound", sourceName));
                return;
            }

            int targetIndex = BibtexEntries.IndexOf(targetEntry);
            if (targetIndex < 0)
            {
                NotificationCenter.Error(I18n.Get("Message.SearchResultIgnoredOriginalRemoved"));
                return;
            }

            NotificationCenter.Info(I18n.Format("Message.FoundSourceCandidates", list.Count, sourceName));
            list.Insert(0, targetEntry);
            CompareEntryView dialog = new(list);
            var result = await dialog.ShowDialog<bool>(window);
            if (result == true)
            {
                if (dialog.DataContext is not CompareEntryViewModel cevm) { return; }
                if (!ReplaceEntriesWithMerged([targetEntry], cevm.MergedEntry))
                {
                    NotificationCenter.Error(I18n.Get("Message.MergeCanceledOriginalRemoved"));
                    return;
                }
            }
        }

        [RelayCommand]
        private async Task MergeEntries(object? sender)
        {
            if (sender is not MainWindow window) { return; }
            if (SelectedIndexItems == null || SelectedIndexItems.Count < 2) { return; }
            var list = SelectedIndexItems.Select(t => t.Item2).ToList();
            CompareEntryView dialog = new(list);
            var result = await dialog.ShowDialog<bool>(window);
            if (result == true)
            {
                if (dialog.DataContext is not CompareEntryViewModel cevm) { return; }
                if (!ReplaceEntriesWithMerged(list, cevm.MergedEntry))
                {
                    NotificationCenter.Error(I18n.Get("Message.MergeCanceledOriginalRemoved"));
                }
            }
        }

        private bool ReplaceEntriesWithMerged(IReadOnlyList<BibtexEntry> oldEntries, BibtexEntry mergedEntry)
        {
            List<(int Index, BibtexEntry Entry)> oldIndexEntries = oldEntries
                .Select(entry => (Index: BibtexEntries.IndexOf(entry), Entry: entry))
                .Where(item => item.Index >= 0)
                .OrderBy(item => item.Index)
                .ToList();

            if (oldIndexEntries.Count != oldEntries.Count || oldIndexEntries.Count == 0)
            { return false; }

            int insertIndex = oldIndexEntries[0].Index;
            foreach (var (_, entry) in oldIndexEntries)
            { BibtexEntries.Remove(entry); }

            BibtexEntries.Insert(insertIndex, mergedEntry);
            mergedEntry.UndoRedoPropertyChanged += OnEntryPropertyChanged;
            UndoRedoManager.AddAction(new ReplaceEntriesAction(
                BibtexEntries,
                oldIndexEntries.Select(item => (item.Index, item.Entry)).ToList(),
                [(insertIndex, mergedEntry)]));
            ShowingEntry = mergedEntry;
            NotifyCanUndoRedo();
            return true;
        }

        [RelayCommand]
        private async Task ApplyBatchFieldDelete(object? sender)
        {
            if (sender is not MainWindow window || SelectedIndexItems == null || SelectedIndexItems.Count == 0) { return; }
            var selectedEntries = SelectedIndexItems.Select(t => t.Item2).ToList();
            BatchFieldDeleteView dialog = new();
            var result = await dialog.ShowDialog<bool>(window);
            if (result != true || dialog.DataContext is not BatchFieldDeleteViewModel vm) { return; }

            List<string> selectedFields = vm.GetSelectedFieldNames();
            if (selectedFields.Count == 0)
            {
                ShowStatus(I18n.Get("Message.NoFieldsSelected"));
                return;
            }

            List<EntryFieldChange> changes = [];
            foreach (var entry in selectedEntries)
            {
                changes.AddRange(vm.CreateChanges(entry));
            }

            EntryFieldsChangeAction action = new(changes);
            if (!action.HasChanges)
            {
                ShowStatus(I18n.Get("Message.NoSelectedFieldsFound"));
                return;
            }

            string fieldSummary = string.Join(", ", selectedFields);
            string title = vm.KeepSelectedFieldsOnly
                ? I18n.Get("Dialog.KeepSelectedFieldsOnly.Title")
                : I18n.Get("Dialog.DeleteSelectedFields.Title");
            string message = vm.KeepSelectedFieldsOnly
                ? I18n.Format("Dialog.KeepSelectedFieldsOnly.Message", fieldSummary, selectedEntries.Count, changes.Count)
                : I18n.Format("Dialog.DeleteSelectedFields.Message", fieldSummary, selectedEntries.Count, changes.Count);
            if (!await ConfirmSelectedEntryBatchOperationAsync(title, message))
            {
                ShowStatus(I18n.Get("Message.BatchFieldDeletionCanceled"));
                return;
            }

            ApplyEntryFieldsChangeAction(action);
            ShowStatus(vm.KeepSelectedFieldsOnly
                ? I18n.Format("Message.KeptFields", selectedFields.Count, changes.Count)
                : I18n.Format("Message.DeletedFieldValues", changes.Count, selectedEntries.Count));
        }

        [RelayCommand]
        private async Task NormalizeSelectedVenueNames(object? sender)
        {
            if (sender is not MainWindow window || SelectedIndexItems == null || SelectedIndexItems.Count == 0) { return; }
            var selectedEntries = SelectedIndexItems
                .OrderBy(item => item.Item1)
                .Select(item => item.Item2)
                .ToList();

            VenueNameNormalizationView dialog = new(selectedEntries);
            var result = await dialog.ShowDialog<bool>(window);
            if (result != true || dialog.DataContext is not VenueNameNormalizationViewModel vm) { return; }

            List<EntryFieldChange> changes = vm.CreateChanges();
            EntryFieldsChangeAction action = new(changes);
            if (!action.HasChanges)
            {
                ShowStatus(I18n.Get("Message.NoVenueNamesChanged"));
                return;
            }

            ApplyEntryFieldsChangeAction(action);
            ShowStatus(I18n.Format("Message.UpdatedVenueNames", changes.Count));
        }

        [RelayCommand]
        private async Task CleanupSelectedEntries()
        {
            if (SelectedIndexItems == null || SelectedIndexItems.Count == 0) { return; }
            var selectedEntries = SelectedIndexItems.Select(t => t.Item2).ToList();
            List<EntryFieldChange> changes = BibtexBatchOperations.CreateCleanupChanges(selectedEntries);

            EntryFieldsChangeAction action = new(changes);
            if (!action.HasChanges)
            {
                ShowStatus(I18n.Get("Message.NoSelectedEntriesNeededCleanup"));
                return;
            }
            if (!await ConfirmSelectedEntryBatchOperationAsync(
                I18n.Get("Dialog.CleanSelectedEntries.Title"),
                I18n.Format("Dialog.CleanSelectedEntries.Message", selectedEntries.Count)))
            {
                ShowStatus(I18n.Get("Message.CleanCanceled"));
                return;
            }

            ApplyEntryFieldsChangeAction(action);
            ShowStatus(I18n.Format("Message.CleanedSelectedEntries", selectedEntries.Count));
        }

        [RelayCommand]
        private async Task GenerateSelectedCitationKeys()
        {
            if (SelectedIndexItems == null || SelectedIndexItems.Count == 0) { return; }

            List<BibtexEntry> selectedEntries = SelectedIndexItems
                .OrderBy(item => item.Item1)
                .Select(item => item.Item2)
                .ToList();
            List<EntryFieldChange> changes = BibtexBatchOperations.CreateCitationKeyChanges(selectedEntries, BibtexEntries);

            EntryFieldsChangeAction action = new(changes);
            if (!action.HasChanges)
            {
                ShowStatus(I18n.Get("Message.NoSelectedEntriesChanged"));
                return;
            }
            if (!await ConfirmSelectedEntryBatchOperationAsync(
                I18n.Get("Dialog.GenerateCitationKeys.Title"),
                I18n.Format("Dialog.GenerateCitationKeys.Message", changes.Count)))
            {
                ShowStatus(I18n.Get("Message.GenerateCitationKeysCanceled"));
                return;
            }

            ApplyEntryFieldsChangeAction(action);
            ShowStatus(I18n.Format("Message.GeneratedCitationKeys", changes.Count));
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

        [RelayCommand]
        private void GenerateCitationKey()
        {
            if (ShowingEntry == null) { return; }
            string key = ShowingEntry.BuildCitationKey();
            if (string.IsNullOrWhiteSpace(key)) { return; }
            ShowingEntry.CitationKey = key;
        }

        [RelayCommand]
        private void ToShowingLink(string s)
        {
            if (ShowingEntry == null) { return; }
            if (s == "DOI")
            { UriProcessor.StartProcess("https://doi.org/" + ShowingEntry.DOI); }
            else if (s == "Url")
            { UriProcessor.StartProcess(ShowingEntry.Url); }
        }

        [RelayCommand]
        private static void ToLink(object o)
        {
            if (o is not BibtexEntry entry) { return; }
            UriProcessor.StartProcess(entry.DOI == "" ? entry.Url : "https://doi.org/" + entry.DOI);
        }

        [RelayCommand]
        private static void ToFile(object o)
        {
            if (o is not BibtexEntry entry) { return; }
            string path = BibtexEntry.ResolveFilePath(entry.File);
            if (string.IsNullOrWhiteSpace(path)) { return; }
            UriProcessor.StartProcess(path);
        }


        [RelayCommand]
        private void CopyBibtexText(object o)
        {
            if (ShowingEntry == null) { return; }
            if (o is Window w) { w.Clipboard?.SetTextAsync(ShowingEntry.BibTeX); }
        }

        [RelayCommand]
        private void CopyCitationKey(object? o)
        {
            if (ShowingEntry == null) { return; }
            if (o is Window w) { w.Clipboard?.SetTextAsync(ShowingEntry.CitationKey); }
        }
        #endregion Command
    }
}
