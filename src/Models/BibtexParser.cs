using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Litenbib.Models
{
    internal partial class BibtexParser
    {
        // 解析BibTeX字符串，返回条目列表
        public static List<BibtexEntry> Parse(string bibTeXText)
        {
            var entries = new List<BibtexEntry>();

            // 移除注释
            bibTeXText = RemoveComments(bibTeXText);

            // 查找所有条目
            var entryMatches = BibtexEntryRegex().Matches(bibTeXText);

            foreach (Match entryMatch in entryMatches)
            {
                string entryType = entryMatch.Groups[1].Value;
                string citationKey = entryMatch.Groups[2].Value.Trim();

                // 找到当前条目的开始和结束位置
                int startPos = entryMatch.Index;
                int endPos = FindMatchingBrace(bibTeXText, startPos + entryMatch.Length - 1);

                if (endPos == -1) continue; // 如果没有找到匹配的括号，跳过

                // 提取条目内容
                string entryContent = bibTeXText[(startPos + entryMatch.Length)..endPos].Trim();

                // 创建条目对象
                BibtexEntry entry = new(entryType, citationKey);

                // 解析字段
                ParseFields(entryContent, entry);

                entry.UpdateBibtex(isSilent: true);

                entries.Add(entry);
            }

            return entries;
        }

        public static BibtexEntry? ParseBibTeX(string bibTeXText)
        {
            // 移除注释
            bibTeXText = RemoveComments(bibTeXText);

            // 查找所有条目
            var entryMatch = BibtexEntryRegex().Match(bibTeXText);

            string entryType = entryMatch.Groups[1].Value;
            string citationKey = entryMatch.Groups[2].Value.Trim();

            // 找到当前条目的开始和结束位置
            int startPos = entryMatch.Index;
            int endPos = FindMatchingBrace(bibTeXText, startPos + entryMatch.Length - 1);

            if (endPos == -1) return null; // 如果没有找到匹配的括号

            // 提取条目内容
            string entryContent = bibTeXText[(startPos + entryMatch.Length)..endPos].Trim();

            // 创建条目对象
            BibtexEntry entry = new(entryType, citationKey);

            // 解析字段
            if (ParseFieldsStrict(entryContent, entry))
            { return entry; }
            else
            { return null; }
        }

        // 移除BibTeX注释
        private static string RemoveComments(string text)
        {
            // 移除行内注释
            return CommentRegex().Replace(text, "");
        }

        // 找到匹配的右大括号
        private static int FindMatchingBrace(string text, int startPos)
        {
            int braceCount = 1;
            bool inQuotes = false;

            for (int i = int.Max(startPos, 0); i < text.Length; i++)
            {
                char c = text[i];

                if (c == '"' && (i == 0 || text[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }

                if (!inQuotes)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;

                    if (braceCount == 0) return i;
                }
            }

            return -1; // 没有找到匹配的括号
        }

        // 解析字段
        private static void ParseFields(string content, BibtexEntry entry)
        {
            // 匹配字段: 字段名 = {值} 或 字段名 = "值" 或 字段名 = 值
            var fieldMatches = FieldRegex().Matches(content);

            foreach (Match match in fieldMatches)
            {
                string fieldName = match.Groups[1].Value.Trim().ToLower();
                string fieldValue = StringCleaner.CleanStringWithRegex(match.Groups[2].Value.Trim());

                // 清理字段值（移除大括号和引号）
                if (fieldValue.StartsWith('{') && fieldValue.EndsWith('}'))
                { fieldValue = fieldValue[1..^1].Trim(); }
                else if (fieldValue.StartsWith('\"') && fieldValue.EndsWith('\"'))
                { fieldValue = fieldValue[1..^1].Trim(); }

                entry.Fields[fieldName] = fieldValue;
            }
        }


        // 解析字段
        private static bool ParseFieldsStrict(string content, BibtexEntry entry)
        {
            // 匹配字段: 字段名 = {值} 或 字段名 = "值" 或 字段名 = 值
            var fieldMatches = FieldRegex().Matches(content);

            foreach (Match match in fieldMatches)
            {
                string fieldName = match.Groups[1].Value.Trim().ToLower();
                string fieldValue = StringCleaner.CleanStringWithRegex(match.Groups[2].Value.Trim());

                // 清理字段值（移除大括号和引号）
                if (fieldValue.StartsWith('{'))
                {
                    if (fieldValue.EndsWith('}'))
                    { fieldValue = fieldValue[1..^1].Trim(); }
                    else
                    { return false; }
                }
                else if (fieldValue.StartsWith('\"') && fieldValue.EndsWith('\"'))
                {
                    if (fieldValue.EndsWith('\"'))
                    { fieldValue = fieldValue[1..^1].Trim(); }
                    else
                    { return false; }
                }

                entry.Fields[fieldName] = fieldValue;
            }
            return true;
        }

        [GeneratedRegex(@"@(\w*)\s*{\s*([^,]*)\s*,", RegexOptions.Compiled)]
        private static partial Regex BibtexEntryRegex();

        [GeneratedRegex(@"%.*?$", RegexOptions.Multiline)]
        private static partial Regex CommentRegex();

        [GeneratedRegex(@"(\w+)\s*=\s*({(?:[^{}]|(?<c>{)|(?<-c>}))*(?(c)(?!))}|""[^""]*""|[^,}]+)", RegexOptions.Compiled)]
        private static partial Regex FieldRegex();
    }
}
