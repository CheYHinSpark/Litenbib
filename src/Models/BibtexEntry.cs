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


        // showing fields
        public bool Selected { get; set; }
        public string Type_Show { get => type; }
        public string Author_Editor
        {
            get
            {
                string author = GetValue(nameof(Author));
                return author == "" ? GetValue(nameof(Editor)) : author;
            }
        }
        public string Title_Show { get => GetValue("title"); }
        public string Journal_Booktitle
        {
            get
            {
                string journal = GetValue(nameof(Journal));
                return journal == "" ? GetValue(nameof(Booktitle)) : journal;
            }
        }
        public string Year_Show { get => GetValue("year"); }

        // editable fields
        public string Type
        {
            get => type;
            set
            {
                type = value;
                OnPropertyChanged(nameof(Type));
                OnPropertyChanged(nameof(Type_Show));
            }
        }
        public string CitationKey
        {
            get => citationKey;
            set
            {
                citationKey = value;
                OnPropertyChanged(nameof(CitationKey));
            }
        }
        public string Author
        {
            get => GetValue("author");
            set => SetValue("author", value, [nameof(Author), nameof(Author_Editor)]);
        }
        public string Title
        {
            get => GetValue("title");
            set => SetValue("title", value, [nameof(Title), nameof(Title_Show)]);
        }
        public string Year
        {
            get => GetValue("year");
            set => SetValue("year", value, [nameof(Year), nameof(Year_Show)]);
        }
        public string Month
        {
            get => GetValue("month");
            set => SetValue("month", value, [nameof(Month)]);
        }
        
        public string Editor
        {
            get => GetValue("editor");
            set => SetValue("editor", value, [nameof(Editor), nameof(Author_Editor)]);
        }
        public string Journal
        {
            get => GetValue("journal");
            set => SetValue("journal", value, [nameof(Journal), nameof(Journal_Booktitle)]);
        }
        public string Volume
        {
            get => GetValue("volume");
            set => SetValue("volume", value, [nameof(Volume)]);
        }
        public string Number
        {
            get => GetValue("number");
            set => SetValue("number", value, [nameof(Number)]);
        }
        public string Pages
        {
            get => GetValue("pages");
            set => SetValue("pages", value, [nameof(Pages)]);
        }
        public string Publisher
        {
            get => GetValue("publisher");
            set => SetValue("publisher", value, [nameof(Publisher)]);
        }
        public string Booktitle
        {
            get => GetValue("booktitle");
            set => SetValue("booktitle", value, [nameof(Booktitle), nameof(Journal_Booktitle)]);
        }
        public string Address
        {
            get => GetValue("address");
            set => SetValue("address", value, [nameof(Address)]);
        }
        public string School
        {
            get => GetValue("school");
            set => SetValue("school", value, [nameof(School)]);
        }
        public string Edition
        {
            get => GetValue("edition");
            set => SetValue("edition", value, [nameof(Edition)]);
        }
        public string Chapter
        {
            get => GetValue("chapter");
            set => SetValue("chapter", value, [nameof(Chapter)]);
        }
        public string Note
        {
            get => GetValue("note");
            set => SetValue("note", value, [nameof(Note)]);
        }

        public string DOI
        {
            get => GetValue("doi");
            set => SetValue("doi", value, [nameof(DOI)]);
        }
        public string Url
        {
            get => GetValue("url");
            set => SetValue("url", value, [nameof(Url)]);
        }
        public string ISSN
        {
            get => GetValue("issn");
            set => SetValue("issn", value, [nameof(ISSN)]);
        }
        public string File
        {
            get => GetValue("file");
            set => SetValue("file", value, [nameof(File)]);
        }

        public string Abstract
        {
            get => GetValue("abstract");
            set => SetValue("abstract", value, [nameof(Abstract)]);
        }
        public string Keywords
        {
            get => GetValue("keywords");
            set => SetValue("keywords", value, [nameof(Keywords)]);
        }
        public string Comment
        {
            get => GetValue("comment");
            set => SetValue("comment", value, [nameof(Comment)]);
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private string GetValue(string k)
        { return Fields.TryGetValue(k, out string? value) ? value : ""; }

        private void SetValue(string k, string v, string[]? additionalNotifications = null)
        {
            Fields[k] = v;
            if (additionalNotifications != null)
            {
                foreach (var notification in additionalNotifications)
                { OnPropertyChanged(notification); }
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Debug.WriteLine("Changing " + propertyName);
        }

        public static BibtexEntry FromDOI(string doi)
        {
            BibtexEntry entry = new("Unknown" ,doi);
            entry.Fields["doi"] = doi;
            // TODO: 解析DOI
            return entry;
        }

        public string ToBibtex()
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
}
