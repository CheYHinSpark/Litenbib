using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Litenbib.Models
{
    internal class BibtexEntry(string type, string citationKey)
    {
        public string Type = type;
        public string CitationKey = citationKey;
        public Dictionary<string, string> Fields = new(StringComparer.OrdinalIgnoreCase);

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
            string s = $"@{Type}{{{CitationKey},\r\n";
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
