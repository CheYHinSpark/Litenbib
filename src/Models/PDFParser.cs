using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Litenbib.Models
{
    public class ExtractedMetadata
    {
        public string Doi { get; set; } = "";
        public string ArxivId { get; set; } = "";
        public string RawText { get; set; } = string.Empty;
    }
    public partial class PdfMetadataExtractor
    {
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
