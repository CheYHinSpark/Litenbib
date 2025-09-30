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
        //public string Title { get; set; } = "Unknown";
        //public string Authors { get; set; } = "Unknown";
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

            //// --- 2. 标题和作者提取 (启发式方法，精度较低) ---


            //var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            //string titleCandidate = "未找到";
            //int titleIndex = -1;

            //// 尝试从前几行中找到最长的一行作为标题 (启发式)
            //for (int i = 0; i < Math.Min(lines.Length, 5); i++)
            //{
            //    string line = lines[i].Trim();
            //    // 排除过短的行或明显的日期/标识符行
            //    if (line.Length > 20 && !line.ToLower().Contains("arxiv") && !line.ToLower().Contains("date"))
            //    {
            //        if (line.Length > titleCandidate.Length)
            //        {
            //            titleCandidate = line;
            //            titleIndex = i;
            //        }
            //    }
            //}

            //if (titleCandidate != "未找到")
            //{
            //    metadata.Title = titleCandidate;

            //    // 假设作者信息紧跟在标题之后
            //    if (titleIndex != -1 && lines.Length > titleIndex + 1)
            //    {
            //        // 简单地取标题后的一两行作为作者的候选文本
            //        metadata.Authors = lines[titleIndex + 1].Trim();

            //        // 尝试清理作者行：移除邮箱和机构信息
            //        // 此正则表达式仅为示例，可能需要根据实际论文格式调整
            //        string cleanAuthorLine = Regex.Replace(metadata.Authors, @"[\w\.-]+@[\w\.-]+\s*(\(.+?\))?", "").Trim();

            //        if (!string.IsNullOrWhiteSpace(cleanAuthorLine) && cleanAuthorLine.Length > 5)
            //        {
            //            metadata.Authors = cleanAuthorLine;
            //        }
            //    }
            //}
        }

        [GeneratedRegex(@"(10\.\d{4,9}\/[^\s]+)", RegexOptions.IgnoreCase)]
        private static partial Regex DoiRegex();

        [GeneratedRegex(@"(arXiv:\s*\d{4}\.\d{4,5}(v\d+)?)", RegexOptions.IgnoreCase)]
        private static partial Regex ArxivRegex();
    }
}
