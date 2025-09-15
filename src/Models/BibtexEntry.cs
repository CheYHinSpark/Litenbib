using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Litenbib.Models
{
    public class BibtexEntry(string entryType = "", string citationKey = ""): INotifyPropertyChanged
    {
        private string entryType = entryType;
        private string citationKey = citationKey;
        private string bibtex = "";
        public Dictionary<string, string> Fields = new(StringComparer.OrdinalIgnoreCase);

        #region Fields
        public string EntryType
        {
            get => entryType;
            set
            {
                if (entryType != value && value != null)
                {
                    string t = entryType;
                    entryType = value;
                    OnPropertyChanged(nameof(EntryType), t, value);
                }
            }
        }
        public string CitationKey
        {
            get => citationKey;
            set
            {
                if (citationKey != value)
                {
                    string t = citationKey;
                    citationKey = value;
                    OnPropertyChanged(nameof(CitationKey), t, value);
                }
            }
        }

        #region important fields
        public string Author { get => GetValue("author"); set => SetValue(nameof(Author), value); }
        public string Title { get => GetValue("title"); set => SetValue(nameof(Title), value); }
        public string Year { get => GetValue("year"); set => SetValue(nameof(Year), value); }
        public string BibTeX { get => bibtex; set => UpdateBibtex(value); }
        #endregion

        #region other fields sorted by abc
        public string Abstract { get => GetValue("abstract"); set => SetValue(nameof(Abstract), value); }
        public string Address { get => GetValue("address"); set => SetValue(nameof(Address), value); }
        public string Annote { get => GetValue("annote"); set => SetValue(nameof(Annote), value); }
        public string Booktitle { get => GetValue("booktitle"); set => SetValue(nameof(Booktitle), value); }
        public string Chapter { get => GetValue("chapter"); set => SetValue(nameof(Chapter), value); }
        public string Comment { get => GetValue("comment"); set => SetValue(nameof(Comment), value); }
        public string Crossref { get => GetValue("crossref"); set => SetValue(nameof(Crossref), value); }
        public string DOI { get => GetValue("doi"); set => SetValue(nameof(DOI), value); }
        public string Edition { get => GetValue("edition"); set => SetValue(nameof(Edition), value); }
        public string Editor { get => GetValue("editor"); set => SetValue(nameof(Editor), value); }
        public string File { get => GetValue("file"); set => SetValue(nameof(File), value); }
        public string Howpublished { get => GetValue("howpublished"); set => SetValue(nameof(Howpublished), value); }
        public string Institution { get => GetValue("institution"); set => SetValue(nameof(Institution), value); }
        public string ISBN { get => GetValue("isbn"); set => SetValue(nameof(ISBN), value); }
        public string ISSN { get => GetValue("issn"); set => SetValue(nameof(ISSN), value); }
        public string Journal { get => GetValue("journal"); set => SetValue(nameof(Journal), value); }
        public string Key { get => GetValue("key"); set => SetValue(nameof(Key), value); }
        public string Keywords { get => GetValue("keywords"); set => SetValue(nameof(Keywords), value); }
        public string Month { get => GetValue("month"); set => SetValue(nameof(Month), value); }
        public string Note { get => GetValue("note"); set => SetValue(nameof(Note), value); }
        public string Number { get => GetValue("number"); set => SetValue(nameof(Number), value); }
        public string Organization { get => GetValue("organization"); set => SetValue(nameof(Organization), value); }
        public string Pages { get => GetValue("pages"); set => SetValue(nameof(Pages), value); }
        public string Publisher { get => GetValue("publisher"); set => SetValue(nameof(Publisher), value); }
        public string School { get => GetValue("school"); set => SetValue(nameof(School), value); }
        public string Series { get => GetValue("series"); set => SetValue(nameof(Series), value); }
        public string Type { get => GetValue("type"); set => SetValue(nameof(Type), value); }
        public string Url { get => GetValue("url"); set => SetValue(nameof(Url), value); }
        public string Volume { get => GetValue("volume"); set => SetValue(nameof(Volume), value); }
        #endregion
        #endregion

        // 通用的属性变化事件，用于通知其他控件的显示
        public event PropertyChangedEventHandler? PropertyChanged;
        // 用于UndoRedo的功能
        public event PropertyChangedEventHandler? UndoRedoPropertyChanged;

        // Get Field元素
        private string GetValue(string k)
        { return Fields.TryGetValue(k, out string? value) ? value : ""; }

        // Set Field元素
        private void SetValue(string k, string v)
        {
            string kl = k.ToLower();
            string? t = Fields.TryGetValue(kl, out string? value) ? value : null;
            if (v == t) { return; }
            Fields[kl] = v;
            if (string.IsNullOrWhiteSpace(v))
            { Fields.Remove(kl); }
            OnPropertyChanged(k, t, v);
        }

        // 不触发UndoRedo的修改
        public void SetValueSilent(string propertyName, string? v)
        {
            switch (propertyName)
            {
                case "":
                    return;
                case "BibTeX":
                    { UpdateBibtex(v, true); }
                    return;
                case "EntryType":
                    entryType = v ?? "";
                    break;
                case "CitationKey":
                    citationKey = v ?? "";
                    break;
                default:
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        { Fields.Remove(propertyName.ToLower()); }
                        else{ Fields[propertyName.ToLower()] = v; }
                    }
                    break;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            UpdateBibtex();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null, string? oldValue = null, string? newValue = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            UndoRedoPropertyChanged?.Invoke(this, new PropertyChangedEventArgsEx(propertyName, oldValue, newValue));
            if (propertyName != nameof(BibTeX)) { UpdateBibtex(); }
            Debug.WriteLine("Changing " + propertyName);
            Debug.WriteLine(BibTeX);
        }

        public static BibtexEntry FromDOI(string doi)
        {
            BibtexEntry entry = new("" ,doi);
            entry.Fields["doi"] = doi;
            // TODO: 解析DOI
            return entry;
        }

        // 如果不是直接更新BibTeX，表示从其他属性修改的。不触发BibTeX的Undo
        public void UpdateBibtex(string? newBibtex = null, bool isSilent = false)
        {
            if (newBibtex == null)
            {
                int maxFieldLength = 0;
                foreach (var k in Fields.Keys)
                { maxFieldLength = Math.Max(maxFieldLength, k.Length); }
                string s = $"@{entryType}{{{citationKey},\r\n";
                foreach (KeyValuePair<string, string> kvp in Fields)
                { s += $"    {kvp.Key}" + new string(' ', maxFieldLength - kvp.Key.Length) + $" = {{{kvp.Value}}},\r\n"; }
                bibtex = s + "}\r\n";
            }
            else
            {
                // 直接修改的BibTeX，需要反过来更新其他东西
                string oldBibtex = bibtex;
                bibtex = newBibtex;
                if (oldBibtex == newBibtex) { return; }
                if (!isSilent)
                { UndoRedoPropertyChanged?.Invoke(this, new PropertyChangedEventArgsEx(nameof(BibTeX), oldBibtex, newBibtex)); }
                BibtexEntry? entry = BibtexParser.ParseBibTeX(newBibtex);
                if (entry != null) { CopyFromBibtex(entry); }
                Debug.WriteLine("Changing BibTeX");
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BibTeX)));
        }

        public void CopyFromBibtex(BibtexEntry entry)
        {
            if (entryType != entry.EntryType)
            {
                string t = entryType;
                entryType = entry.EntryType;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EntryType)));
            }
            if (citationKey != entry.CitationKey)
            {
                string t = citationKey;
                citationKey = entry.CitationKey;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CitationKey)));
            }

            var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .Select(p => p.Name);

            foreach (var propertyName in properties)
            {
                if (propertyName == "EntryType" || propertyName == "CitationKey" || propertyName == "BibTeX")
                { continue; }
                string property = propertyName.ToLower();
                entry.Fields.TryGetValue(property, out string? value);
                Fields.TryGetValue(property, out string? oldValue);
                if (value != oldValue)
                {
                    Fields[property] = value!;
                    if (string.IsNullOrWhiteSpace(value))
                    { Fields.Remove(property); }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
