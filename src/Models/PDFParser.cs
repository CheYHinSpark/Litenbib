using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace Litenbib.Models
{
    public class ExtractedMetadata
    {
        public string Doi { get; set; } = "";
        public string ArxivId { get; set; } = "";
        public string RawText { get; set; } = string.Empty;
    }

    public class PdfImportResult(string pdfFilePath, ExtractedMetadata metadata)
    {
        public string PdfFilePath { get; } = pdfFilePath;

        public ExtractedMetadata Metadata { get; } = metadata;

        public string LookupQuery { get; } = CreateLookupQuery(metadata);

        public string RawText => Metadata.RawText;

        public BibtexEntry CreateFallbackEntry()
        {
            string fileTitle = Path.GetFileNameWithoutExtension(PdfFilePath);
            BibtexEntry entry = new("misc", CreateFallbackCitationKey(fileTitle))
            { Title = fileTitle, };

            string doi = CleanIdentifier(Metadata.Doi);
            if (!string.IsNullOrWhiteSpace(doi))
            { entry.DOI = doi; }

            string arxivId = NormalizeArxivId(Metadata.ArxivId);
            if (!string.IsNullOrWhiteSpace(arxivId))
            {
                entry.Url = $"https://arxiv.org/abs/{arxivId}";
                entry.Note = $"arXiv:{arxivId}";
            }

            return entry;
        }

        public void PrepareEntry(BibtexEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.EntryType))
            { entry.EntryType = "misc"; }

            if (string.IsNullOrWhiteSpace(entry.CitationKey))
            { entry.CitationKey = CreateFallbackCitationKey(Path.GetFileNameWithoutExtension(PdfFilePath)); }

            if (string.IsNullOrWhiteSpace(entry.DOI))
            {
                string doi = CleanIdentifier(Metadata.Doi);
                if (!string.IsNullOrWhiteSpace(doi))
                { entry.DOI = doi; }
            }

            if (string.IsNullOrWhiteSpace(entry.Url))
            {
                string arxivId = NormalizeArxivId(Metadata.ArxivId);
                if (!string.IsNullOrWhiteSpace(arxivId))
                { entry.Url = $"https://arxiv.org/abs/{arxivId}"; }
            }

            entry.File = FormatImportedPdfFileValue(PdfFilePath);
        }

        private static string CreateLookupQuery(ExtractedMetadata metadata)
        {
            List<string> parts = [];

            string doi = CleanIdentifier(metadata.Doi);
            if (!string.IsNullOrWhiteSpace(doi))
            { parts.Add(doi); }

            string arxivId = NormalizeArxivId(metadata.ArxivId);
            if (!string.IsNullOrWhiteSpace(arxivId))
            { parts.Add(arxivId); }

            return string.Join('\n', parts);
        }

        private static string FormatImportedPdfFileValue(string pdfFile)
        {
            return AppSettingsState.Current.PdfFilePathStyle switch
            {
                PdfFilePathStyles.None => string.Empty,
                PdfFilePathStyles.JabRef => $":{pdfFile.Replace(":", "\\:").Replace(";", "\\;")}:PDF",
                _ => pdfFile,
            };
        }

        private static string CreateFallbackCitationKey(string value)
        {
            StringBuilder builder = new();
            foreach (char c in value)
            {
                if (char.IsAsciiLetterOrDigit(c) || c == ':' || c == '_' || c == '-')
                { builder.Append(c); }
                else if (builder.Length == 0 || builder[^1] != '_')
                { builder.Append('_'); }
            }

            string key = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(key) ? "pdf_import" : key;
        }

        private static string CleanIdentifier(string value)
        { return (value ?? string.Empty).Trim().TrimEnd('.', ',', ';', ':', ')', ']', '}'); }

        private static string NormalizeArxivId(string value)
        {
            string arxivId = CleanIdentifier(value);
            if (arxivId.StartsWith("arXiv:", StringComparison.OrdinalIgnoreCase))
            { arxivId = arxivId[6..].Trim(); }

            int categoryIndex = arxivId.IndexOf(' ');
            if (categoryIndex >= 0)
            { arxivId = arxivId[..categoryIndex].Trim(); }

            return arxivId;
        }
    }
    
    public partial class PdfMetadataExtractor
    {
        public static PdfImportResult ExtractImportResult(string pdfFilePath)
        { return new(pdfFilePath, Extract(pdfFilePath)); }
        
        public static string? TryResolvePdfFullPath(string pdfFile)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(pdfFile);
            }
            catch (Exception)
            {
                NotificationCenter.Error(I18n.Get("Message.ImportPdfInvalidPath"));
                return null;
            }

            if (!File.Exists(fullPath))
            {
                NotificationCenter.Error(I18n.Format("Message.ImportPdfFileNotFound", fullPath));
                return null;
            }
            return fullPath;
        }

        public static ExtractedMetadata Extract(string pdfFilePath)
        {
            var metadata = new ExtractedMetadata();

            if (!File.Exists(pdfFilePath))
            { return metadata; }

            try
            {
                // 1. PdfPig Load file
                using PdfDocument document = PdfDocument.Open(pdfFilePath);
                if (document.NumberOfPages == 0)
                { return metadata; }

                // 2. Extract Page 1
                var page = document.GetPage(1); // PdfPig start from 1
                metadata.RawText = ContentOrderTextExtractor.GetText(page);

                // 3. Parse
                ParseMetadata(metadata);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PDF Parse failed: {ex.Message}");
                metadata.RawText = $"Error: {ex.Message}";
            }

            return metadata;
        }

        private static void ParseMetadata(ExtractedMetadata metadata)
        {
            string text = metadata.RawText;

            // --- 1. DOI and arXiv ---

            // 匹配 DOI: 10.xxxx/xxxx (标准 DOI 格式)
            var doiMatch = DoiRegex().Match(text);
            if (doiMatch.Success)
            {
                // 提取捕获组 1 的值，并去除末尾的标点符号
                metadata.Doi = doiMatch.Groups[1].Value.TrimEnd('.', ',', ' ');
            }

            // 匹配 arXiv ID: arXiv:XXXX.XXXXX (包括可选的版本号和分类)
            // 例如：arXiv:2301.12345v1 [cs.AI]
            var arxivMatch = ArxivRegex().Match(text);
            if (arxivMatch.Success)
            { metadata.ArxivId = arxivMatch.Groups[1].Value.Trim(); }
        }

        [GeneratedRegex(@"(10\.\d{4,9}\/[^\s]+)", RegexOptions.IgnoreCase)]
        private static partial Regex DoiRegex();

        [GeneratedRegex(@"(arXiv:\s*\d{4}\.\d{4,5}(v\d+)?)", RegexOptions.IgnoreCase)]
        private static partial Regex ArxivRegex();
    }
}
