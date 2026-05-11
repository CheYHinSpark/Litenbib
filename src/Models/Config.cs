using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;

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
        public int OnlineLookupTimeoutSeconds { get; set; } = 15;

        public int FieldIndentSpaces { get; set; } = 4;

        public string EntryTypeCaseStyle { get; set; } = EntryTypeCaseStyles.Lowercase;

        public string CitationKeyTemplate { get; set; } = "{firstauthor}_{giveninitials}_{year}";

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
                CitationKeyTemplate = CitationKeyTemplate,
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
                CitationKeyTemplate = string.IsNullOrWhiteSpace(settings.CitationKeyTemplate)
                    ? "{firstauthor}_{giveninitials}_{year}"
                    : settings.CitationKeyTemplate.Trim(),
                AiBaseUrl = settings.AiBaseUrl?.Trim() ?? string.Empty,
                AiApiKey = settings.AiApiKey?.Trim() ?? string.Empty,
                AiModelName = settings.AiModelName?.Trim() ?? string.Empty,
                UseAiPdfImportFallback = settings.UseAiPdfImportFallback,
            };
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

    public static class AppSettingsState
    {
        public static AppSettings Current { get; private set; } = new();

        public static void Apply(AppSettings? settings)
        {
            Current = AppSettings.Normalize(settings);
        }
    }
}
