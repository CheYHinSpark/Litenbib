using System;
using System.Collections.Generic;

namespace Litenbib.Models
{
    public enum BibtexAmpersandIssueKind
    {
        None,
        Unescaped,
        HtmlEntity,
    }

    public static class BibtexAmpersand
    {
        private static readonly HashSet<string> CleanupTextFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "abstract",
            "address",
            "annote",
            "booktitle",
            "comment",
            "edition",
            "howpublished",
            "institution",
            "journal",
            "keywords",
            "note",
            "organization",
            "publisher",
            "school",
            "series",
            "title",
            "type",
        };

        private static readonly HashSet<string> IgnoredWarningFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "doi",
            "file",
            "url",
        };

        public static bool ShouldCleanupField(string fieldName)
        {
            return CleanupTextFields.Contains(fieldName);
        }

        public static bool ShouldWarnField(string fieldName)
        {
            return !IgnoredWarningFields.Contains(fieldName);
        }

        public static BibtexAmpersandIssueKind GetIssueKind(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '&' || IsEscaped(value, i))
                {
                    continue;
                }

                return IsAmpersandHtmlEntityAt(value, i)
                    ? BibtexAmpersandIssueKind.HtmlEntity
                    : BibtexAmpersandIssueKind.Unescaped;
            }

            return BibtexAmpersandIssueKind.None;
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string result = value;
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != '&' || IsEscaped(result, i))
                {
                    continue;
                }

                if (TryGetAmpersandHtmlEntityLength(result, i, out int entityLength))
                {
                    result = result.Remove(i, entityLength).Insert(i, "\\&");
                }
                else
                {
                    result = result.Insert(i, "\\");
                }

                i++;
            }

            return result;
        }

        private static bool IsAmpersandHtmlEntityAt(string value, int index)
        {
            return TryGetAmpersandHtmlEntityLength(value, index, out _);
        }

        private static bool TryGetAmpersandHtmlEntityLength(string value, int index, out int length)
        {
            string rest = value[index..];
            if (rest.StartsWith("&amp;", StringComparison.OrdinalIgnoreCase))
            {
                length = 5;
                return true;
            }

            if (rest.StartsWith("&#38;", StringComparison.OrdinalIgnoreCase))
            {
                length = 5;
                return true;
            }

            if (rest.StartsWith("&#x26;", StringComparison.OrdinalIgnoreCase))
            {
                length = 6;
                return true;
            }

            length = 0;
            return false;
        }

        private static bool IsEscaped(string value, int index)
        {
            int slashCount = 0;
            for (int i = index - 1; i >= 0 && value[i] == '\\'; i--)
            {
                slashCount++;
            }

            return slashCount % 2 == 1;
        }
    }
}
