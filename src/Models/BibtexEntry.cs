using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Litenbib.Models
{
    public partial class BibtexEntry(string entryType = "", string citationKey = "") : INotifyPropertyChanged
    {
        private static List<string> NonFieldsProperties = ["Fields", "EntryType", "CitationKey", "BibTeX"];
        private static readonly Dictionary<string, string> EntryTypeTitleNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["article"] = "Article",
            ["book"] = "Book",
            ["booklet"] = "Booklet",
            ["conference"] = "Conference",
            ["inbook"] = "InBook",
            ["incollection"] = "InCollection",
            ["inproceedings"] = "InProceedings",
            ["manual"] = "Manual",
            ["mastersthesis"] = "MastersThesis",
            ["misc"] = "Misc",
            ["phdthesis"] = "PhdThesis",
            ["proceedings"] = "Proceedings",
            ["techreport"] = "TechReport",
            ["unpublished"] = "Unpublished",
        };
        private string entryType = entryType;
        private string citationKey = citationKey;
        private string bibtex = "";
        public Dictionary<string, string> Fields = new(StringComparer.OrdinalIgnoreCase);

        public string AuthorAndEditor => $"{Author}{Editor}";
        public string JournalAndBooktitleAndPublisher => $"{Journal}{Booktitle}{Publisher}";


        #region Fields
        public string EntryType
        {
            get => entryType;
            set
            {
                if (!entryType.Equals(value, StringComparison.OrdinalIgnoreCase) && value != null)
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
                        else { Fields[propertyName.ToLower()] = v; }
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
        }

        public static BibtexEntry FromDOI(string doi)
        {
            BibtexEntry entry = new("", doi);
            entry.Fields["doi"] = doi;
            // TODO: 解析DOI
            return entry;
        }

        private int MaxFieldLength()
        {
            int maxFieldLength = 0;
            foreach (var k in Fields.Keys)
            { maxFieldLength = Math.Max(maxFieldLength, k.Length); }
            return maxFieldLength;
        }

        // 如果不是直接更新BibTeX，表示从其他属性修改的。不触发BibTeX的Undo
        public void UpdateBibtex(string? newBibtex = null, bool isSilent = false)
        {
            if (newBibtex == null)
            {
                int maxFieldLength = MaxFieldLength();
                string indent = new(' ', AppSettingsState.Current.FieldIndentSpaces);
                StringBuilder builder = new();
                builder.Append('@').Append(FormatEntryType(entryType)).Append('{').Append(citationKey).Append(",\n");
                foreach (KeyValuePair<string, string> kvp in Fields)
                {
                    builder.Append(indent)
                        .Append(kvp.Key)
                        .Append(' ', maxFieldLength - kvp.Key.Length)
                        .Append(" = {")
                        .Append(kvp.Value)
                        .Append("},\n");
                }
                bibtex = builder.Append("}\n").ToString();
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

        public string ExportBibtex(int author_format = 0, int max_authors = -1, string ending = "")
        {
            int maxFieldLength = MaxFieldLength();
            string indent = new(' ', AppSettingsState.Current.FieldIndentSpaces);
            StringBuilder builder = new();
            builder.Append('@').Append(FormatEntryType(entryType)).Append('{').Append(citationKey).Append(",\n");
            int abbreviation = author_format / 2;
            author_format = author_format % 2;
            foreach (KeyValuePair<string, string> kvp in Fields)
            {
                if (kvp.Key == "author")
                {
                    string[]? authors = kvp.Value.Split(" and ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    max_authors = max_authors == -1 ? authors.Length : max_authors;
                    for (int i = int.Min(max_authors, authors.Length) - 1; i >= 0; --i)
                    {
                        var a = Models.Author.GetFamilyGiven(authors[i], abbreviation);
                        authors[i] = author_format == 0 ? $"{a.Item2} {a.Item1}" : $"{a.Item1}, {a.Item2}";
                    }
                    string author = string.Join(" and ", authors[0..int.Min(max_authors, authors.Length)])
                        + (authors.Length > max_authors ? ending : "");
                    builder.Append(indent)
                        .Append("author")
                        .Append(' ', maxFieldLength - kvp.Key.Length)
                        .Append(" = {")
                        .Append(author)
                        .Append("},\n");
                }
                else
                {
                    builder.Append(indent)
                        .Append(kvp.Key)
                        .Append(' ', maxFieldLength - kvp.Key.Length)
                        .Append(" = {")
                        .Append(kvp.Value)
                        .Append("},\n");
                }
            }
            return builder.Append("}\n").ToString();
        }

        private static string FormatEntryType(string type)
        {
            return AppSettingsState.Current.EntryTypeCaseStyle switch
            {
                EntryTypeCaseStyles.TitleCase => EntryTypeTitleNames.TryGetValue(type, out string? titleName)
                    ? titleName
                    : FirstCharToUpper(type),
                EntryTypeCaseStyles.Uppercase => type.ToUpperInvariant(),
                _ => type.ToLowerInvariant(),
            };
        }

        private static string FirstCharToUpper(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
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
                if (BibtexEntry.NonFieldsProperties.Contains(propertyName)) { continue; }

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


        public static BibtexEntry CopyFrom(BibtexEntry e)
        {
            BibtexEntry entry = new(e.entryType, e.citationKey);
            foreach (var kvp in e.Fields)
            { entry.Fields[kvp.Key] = kvp.Value; }
            entry.bibtex = e.bibtex;
            return entry;
        }


        #region Command
        [RelayCommand]
        private void GenerateCitationKey()
        {
            Debug.WriteLine("Generating Citation Key");
            string key = BuildCitationKey();
            if (string.IsNullOrWhiteSpace(key)) { return; }
            CitationKey = key;
        }

        private string BuildCitationKey()
        {
            var (familyName, givenInitials) = GetFirstAuthorNameParts();
            string template = AppSettingsState.Current.CitationKeyTemplate;
            string rawKey = template
                .Replace("{firstauthor}", familyName, StringComparison.OrdinalIgnoreCase)
                .Replace("{giveninitials}", givenInitials, StringComparison.OrdinalIgnoreCase)
                .Replace("{year}", Year, StringComparison.OrdinalIgnoreCase)
                .Replace("{shorttitle}", GetShortTitleToken(), StringComparison.OrdinalIgnoreCase);

            string key = CitationKeyInvalidCharRegex().Replace(rawKey, "_");
            key = MultipleUnderscoreRegex().Replace(key, "_").Trim('_');
            return key;
        }

        private (string FamilyName, string GivenInitials) GetFirstAuthorNameParts()
        {
            string names = !string.IsNullOrWhiteSpace(Author) ? Author : Editor;
            if (string.IsNullOrWhiteSpace(names))
            {
                return (string.Empty, string.Empty);
            }

            string firstAuthor = names.Split(" and ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstAuthor))
            {
                return (string.Empty, string.Empty);
            }

            string familyName;
            string[] givenNames;
            if (firstAuthor.Contains(','))
            {
                var parts = firstAuthor.Split(',', 2);
                familyName = parts[0].Trim();
                givenNames = parts.Length > 1 ? parts[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];
            }
            else
            {
                var nameParts = firstAuthor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                familyName = nameParts.Length == 0 ? string.Empty : nameParts[^1];
                givenNames = nameParts.Length > 1 ? nameParts[..^1] : [];
            }

            StringBuilder initials = new();
            foreach (var name in givenNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (initials.Length > 0)
                    {
                        initials.Append('_');
                    }
                    initials.Append(name[0]);
                }
            }

            return (familyName, initials.ToString());
        }

        private string GetShortTitleToken()
        {
            var words = CitationKeyWordRegex().Matches(Title.Replace("{", "").Replace("}", ""))
                .Cast<Match>()
                .Select(match => match.Value.ToLowerInvariant())
                .Where(word => word.Length > 2)
                .Take(3);
            return string.Join("_", words);
        }

        [RelayCommand]
        private void ToShowingLink(string s)
        {
            if (s == "DOI")
            { UriProcessor.StartProcess("https://doi.org/" + DOI); }
            else if (s == "Url")
            { UriProcessor.StartProcess(Url); }
        }

        [RelayCommand]
        private void ToLink()
        { UriProcessor.StartProcess(DOI == "" ? Url : "https://doi.org/" + DOI); }

        [RelayCommand]
        private void ToFile()
        {
            string path = File.Replace("\\:", ":");
            // 使用 Regex.Match 查找匹配项
            Match match = FileRegex().Match(path);
            if (match.Success)
            { path = match.Groups[1].Value; }
            UriProcessor.StartProcess(path);
        }

        [RelayCommand]
        private void CopyBibtexText(object o)
        { if (o is Window w) { w.Clipboard?.SetTextAsync(BibTeX); } }

        [RelayCommand]
        private void CopyCitationKey(object? o)
        { if (o is Window w) { w.Clipboard?.SetTextAsync(CitationKey); } }
        #endregion Command

        [GeneratedRegex("^:(.+):(.+)$", RegexOptions.Compiled)]
        private static partial Regex FileRegex();

        [GeneratedRegex(@"[^A-Za-z0-9:_\-]+", RegexOptions.Compiled)]
        private static partial Regex CitationKeyInvalidCharRegex();

        [GeneratedRegex(@"_+", RegexOptions.Compiled)]
        private static partial Regex MultipleUnderscoreRegex();

        [GeneratedRegex(@"[A-Za-z0-9]+", RegexOptions.Compiled)]
        private static partial Regex CitationKeyWordRegex();
    }
}
