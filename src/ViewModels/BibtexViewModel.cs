using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Shapes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using Litenbib.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.ViewModels
{
    public partial class BibtexViewModel: ViewModelBase
    {
        public string Header { get; set; }

        public string FullPath { get; set; }

        public bool AllSelected {  get; set; }

        public UndoRedoManager UndoRedoManager { get; set; }

        public ObservableCollection<BibtexEntry> BibtexEntries { get; set; }
        public DataGridCollectionView BibtexView { get; }

        [ObservableProperty]
        private BibtexEntry _showingEntry;

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
            _showingEntry = new("", "");
            UndoRedoManager = new();
        }

        public void ChangeShowing(object i)
        {
            if (i is not BibtexEntry entry) { return; }
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
                { BibtexEntries.Add(entry); }
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

        private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var item = sender as BibtexEntry;
            if (item == null || e.PropertyName == null) { return; }
            if (e is PropertyChangedEventArgsEx extendedArgs)
            {
                // 创建并添加操作到管理器
                var action = new EntryChangeAction(item, e.PropertyName, (string?)extendedArgs.OldValue, (string?)extendedArgs.NewValue);
                UndoRedoManager.AddAction(action);
            }
        }

        //[RelayCommand]
        //public void UndoEdit()
        //{
        //    UndoRedoManager.Undo();
        //}
        //[RelayCommand]
        //public void RedoEdit()
        //{
        //    UndoRedoManager.Redo();
        //}
    }
}
