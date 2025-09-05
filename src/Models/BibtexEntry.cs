using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Litenbib.Models
{
    public class BibtexEntry(string type, string citationKey): INotifyPropertyChanged
    {
        private string type = type;
        private string citationKey = citationKey;
        public Dictionary<string, string> Fields = new(StringComparer.OrdinalIgnoreCase);

        // editable fields
        public string Type
        {
            get => type;
            set
            {
                string t = type;
                type = value;
                OnPropertyChanged(nameof(Type), t, value);
            }
        }
        public string CitationKey
        {
            get => citationKey;
            set
            {
                string t = citationKey;
                citationKey = value;
                OnPropertyChanged(nameof(CitationKey), t, value);
            }
        }
        public string Author
        {
            get => GetValue("author");
            set => SetValue("author", value, nameof(Author));
        }
        public string Title
        {
            get => GetValue("title");
            set => SetValue("title", value, nameof(Title));
        }
        public string Year
        {
            get => GetValue("year");
            set => SetValue("year", value, nameof(Year));
        }
        public string Month
        {
            get => GetValue("month");
            set => SetValue("month", value, nameof(Month));
        }
        
        public string Editor
        {
            get => GetValue("editor");
            set => SetValue("editor", value, nameof(Editor));
        }
        public string Journal
        {
            get => GetValue("journal");
            set => SetValue("journal", value, nameof(Journal));
        }
        public string Volume
        {
            get => GetValue("volume");
            set => SetValue("volume", value, nameof(Volume));
        }
        public string Number
        {
            get => GetValue("number");
            set => SetValue("number", value, nameof(Number));
        }
        public string Pages
        {
            get => GetValue("pages");
            set => SetValue("pages", value, nameof(Pages));
        }
        public string Publisher
        {
            get => GetValue("publisher");
            set => SetValue("publisher", value, nameof(Publisher));
        }
        public string Booktitle
        {
            get => GetValue("booktitle");
            set => SetValue("booktitle", value, nameof(Booktitle));
        }
        public string Address
        {
            get => GetValue("address");
            set => SetValue("address", value, nameof(Address));
        }
        public string School
        {
            get => GetValue("school");
            set => SetValue("school", value, nameof(School));
        }
        public string Edition
        {
            get => GetValue("edition");
            set => SetValue("edition", value, nameof(Edition));
        }
        public string Chapter
        {
            get => GetValue("chapter");
            set => SetValue("chapter", value, nameof(Chapter));
        }
        public string Note
        {
            get => GetValue("note");
            set => SetValue("note", value, nameof(Note));
        }

        public string DOI
        {
            get => GetValue("doi");
            set => SetValue("doi", value, nameof(DOI));
        }
        public string Url
        {
            get => GetValue("url");
            set => SetValue("url", value, nameof(Url));
        }
        public string ISSN
        {
            get => GetValue("issn");
            set => SetValue("issn", value, nameof(ISSN));
        }
        public string File
        {
            get => GetValue("file");
            set => SetValue("file", value, nameof(File));
        }

        public string Abstract
        {
            get => GetValue("abstract");
            set => SetValue("abstract", value, nameof(Abstract));
        }
        public string Keywords
        {
            get => GetValue("keywords");
            set => SetValue("keywords", value, nameof(Keywords));
        }
        public string Comment
        {
            get => GetValue("comment");
            set => SetValue("comment", value, nameof(Comment));
        }

        public string BibTeX
        {
            get
            {
                int maxFieldLength = 0;
                foreach (var k in Fields.Keys)
                { maxFieldLength = Math.Max(maxFieldLength, k.Length); }
                string s = $"@{type}{{{citationKey},\r\n";
                foreach (KeyValuePair<string, string> kvp in Fields)
                {
                    s += $"    {kvp.Key}";
                    s += new string(' ', maxFieldLength - kvp.Key.Length);
                    s += $" = {{{kvp.Value}}},\r\n";
                }
                return s + "}\r\n";
            }
        }

        // 通用的属性变化事件，用于通知其他控件的显示
        public event PropertyChangedEventHandler? PropertyChanged;
        // 用于UndoRedo功能
        public event PropertyChangedEventHandler? UndoRedoPropertyChanged;

        private string GetValue(string k)
        { return Fields.TryGetValue(k, out string? value) ? value : ""; }

        private void SetValue(string k, string v, string notification)
        {
            string? t = Fields.TryGetValue(k, out string? value) ? value : null;
            Fields[k] = v;
            OnPropertyChanged(notification, t, v);
        }

        public void SetValueSilent(string propertyName, string? v)
        {
            switch (propertyName)
            {
                case "":
                    return;
                case "Type":
                    type = v ?? "";
                    break;
                case "CitationKey":
                    citationKey = v ?? "";
                    break;
                default:
                    {
                        if (string.IsNullOrEmpty(v))
                        { Fields.Remove(propertyName.ToLower()); }
                        else{ Fields[propertyName.ToLower()] = v; }
                    }
                    break;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BibTeX)));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null, string? oldValue = null, string? newValue = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            UndoRedoPropertyChanged?.Invoke(this, new PropertyChangedEventArgsEx(propertyName, oldValue, newValue));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BibTeX)));
            Debug.WriteLine("Changing " + propertyName);
        }

        public static BibtexEntry FromDOI(string doi)
        {
            BibtexEntry entry = new("Unknown" ,doi);
            entry.Fields["doi"] = doi;
            // TODO: 解析DOI
            return entry;
        }

        public static BibtexEntry Null => new("", "");
    }
}
