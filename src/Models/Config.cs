using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Litenbib.Models
{
    public class LocalConfig
    {
        public bool ThemeIndex { get; set; }

        public double? WindowWidth { get; set; }

        public double? WindowHeight { get; set; }

        public double? WindowPositionX { get; set; }

        public double? WindowPositionY { get; set; }

        public WindowState? WindowState { get; set; }

        public int SelectedTabIndex { get; set; } = -1;

        public List<RecentFileState> RecentFiles { get; set; } = [];

        public AppSettings Settings { get; set; } = new();
    }

    public class RecentFileState
    {
        public string FilePath { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public int FilterMode { get; set; }

        public string FilterField { get; set; } = "Whole";

        public string FilterText { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        public const string DefaultCitationKeyTemplate = "{family}{year}{title}";

        public const string DefaultCitationKeyDuplicateSuffix = "a";

        private static readonly Regex CitationKeyTemplateTokenRegex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

        private static readonly Regex CitationKeyDuplicateSuffixRegex = new(@"^[A-Za-z0-9:_-]*(?:1|a)$", RegexOptions.Compiled);

        private static readonly HashSet<string> SupportedCitationKeyTokens = new(StringComparer.Ordinal)
        {
            "family",
            "Family",
            "year",
            "title",
        };

        public int OnlineLookupTimeoutSeconds { get; set; } = 15;

        public int FieldIndentSpaces { get; set; } = 4;

        public string EntryTypeCaseStyle { get; set; } = EntryTypeCaseStyles.Lowercase;

        public string PdfFilePathStyle { get; set; } = PdfFilePathStyles.AbsolutePath;

        public string CitationKeyTemplate { get; set; } = DefaultCitationKeyTemplate;

        public string CitationKeyDuplicateSuffix { get; set; } = DefaultCitationKeyDuplicateSuffix;

        public string AiBaseUrl { get; set; } = string.Empty;

        public string AiApiKey { get; set; } = string.Empty;

        public string AiModelName { get; set; } = string.Empty;

        public bool UseAiPdfImportFallback { get; set; }

        public AppSettings Copy()
        {
            return new AppSettings
            {
                OnlineLookupTimeoutSeconds = OnlineLookupTimeoutSeconds,
                FieldIndentSpaces = FieldIndentSpaces,
                EntryTypeCaseStyle = EntryTypeCaseStyle,
                PdfFilePathStyle = PdfFilePathStyle,
                CitationKeyTemplate = CitationKeyTemplate,
                CitationKeyDuplicateSuffix = CitationKeyDuplicateSuffix,
                AiBaseUrl = AiBaseUrl,
                AiApiKey = AiApiKey,
                AiModelName = AiModelName,
                UseAiPdfImportFallback = UseAiPdfImportFallback,
            };
        }

        public static AppSettings Normalize(AppSettings? settings)
        {
            settings ??= new AppSettings();
            string caseStyle = EntryTypeCaseStyles.IsSupported(settings.EntryTypeCaseStyle)
                ? settings.EntryTypeCaseStyle
                : EntryTypeCaseStyles.Lowercase;

            return new AppSettings
            {
                OnlineLookupTimeoutSeconds = Math.Clamp(settings.OnlineLookupTimeoutSeconds, 3, 120),
                FieldIndentSpaces = Math.Clamp(settings.FieldIndentSpaces, 0, 12),
                EntryTypeCaseStyle = caseStyle,
                PdfFilePathStyle = PdfFilePathStyles.IsSupported(settings.PdfFilePathStyle)
                    ? settings.PdfFilePathStyle
                    : PdfFilePathStyles.AbsolutePath,
                CitationKeyTemplate = NormalizeCitationKeyTemplate(settings.CitationKeyTemplate),
                CitationKeyDuplicateSuffix = NormalizeCitationKeyDuplicateSuffix(settings.CitationKeyDuplicateSuffix),
                AiBaseUrl = settings.AiBaseUrl?.Trim() ?? string.Empty,
                AiApiKey = settings.AiApiKey?.Trim() ?? string.Empty,
                AiModelName = settings.AiModelName?.Trim() ?? string.Empty,
                UseAiPdfImportFallback = settings.UseAiPdfImportFallback,
            };
        }

        private static string NormalizeCitationKeyTemplate(string? template)
        {
            template = template?.Trim();
            if (string.IsNullOrWhiteSpace(template))
            {
                return DefaultCitationKeyTemplate;
            }

            foreach (Match match in CitationKeyTemplateTokenRegex.Matches(template))
            {
                if (!SupportedCitationKeyTokens.Contains(match.Groups[1].Value))
                {
                    return DefaultCitationKeyTemplate;
                }
            }

            string literalText = CitationKeyTemplateTokenRegex.Replace(template, string.Empty);
            return literalText.Contains('{') || literalText.Contains('}')
                ? DefaultCitationKeyTemplate
                : template;
        }

        private static string NormalizeCitationKeyDuplicateSuffix(string? suffix)
        {
            suffix = suffix?.Trim();
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return DefaultCitationKeyDuplicateSuffix;
            }

            return CitationKeyDuplicateSuffixRegex.IsMatch(suffix)
                ? suffix
                : DefaultCitationKeyDuplicateSuffix;
        }
    }

    public static class EntryTypeCaseStyles
    {
        public const string Lowercase = "lowercase";
        public const string TitleCase = "TitleCase";
        public const string Uppercase = "UPPERCASE";

        public static bool IsSupported(string? value)
        {
            return value == Lowercase || value == TitleCase || value == Uppercase;
        }
    }

    public static class PdfFilePathStyles
    {
        public const string None = "Deprecate";
        public const string AbsolutePath = "Absolute path";
        public const string JabRef = "JabRef style";

        public static bool IsSupported(string? value)
        {
            return value == None || value == AbsolutePath || value == JabRef;
        }
    }

    public static class AppSettingsState
    {
        public static AppSettings Current { get; private set; } = new();

        public static void Apply(AppSettings? settings)
        {
            Current = AppSettings.Normalize(settings);
        }
    }
}
