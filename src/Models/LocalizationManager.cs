using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Litenbib.Models
{
    public sealed class LocalizedOption(string value, string resourceKey)
    {
        public string Value { get; } = value;

        public string ResourceKey { get; } = resourceKey;

        public string DisplayName => I18n.Get(ResourceKey);

        public override string ToString()
        { return DisplayName; }
    }

    public static class LocalizationManager
    {
        private const string English = "en-US";
        private const string ChineseSimplified = "zh-CN";

        private static readonly Dictionary<string, string> SupportedLanguageCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            [English] = "Language.English",
            [ChineseSimplified] = "Language.ChineseSimplified",
        };

        private static ResourceInclude? _currentLanguageResources;

        public static string CurrentLanguageCode { get; private set; } = GetDefaultLanguageCode();

        public static IReadOnlyList<LocalizedOption> LanguageOptions { get; } =
        [
            new(English, "Language.English"),
            new(ChineseSimplified, "Language.ChineseSimplified"),
        ];

        public static IReadOnlyList<LocalizedOption> FilterFieldOptions { get; } =
        [
            new("Whole", "Main.Filter.Whole"),
            new("Author", "Main.Filter.Author"),
            new("Title", "Main.Filter.Title"),
            new("Citation Key", "Main.Filter.CitationKey"),
        ];

        public static string GetDefaultLanguageCode()
        {
            string cultureName = CultureInfo.CurrentUICulture.Name;
            return cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? ChineseSimplified
                : English;
        }

        public static string NormalizeLanguageCode(string? languageCode)
        {
            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                string exactMatch = SupportedLanguageCodes.Keys.FirstOrDefault(
                    code => string.Equals(code, languageCode, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
                if (!string.IsNullOrEmpty(exactMatch))
                {
                    return exactMatch;
                }

                if (languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                {
                    return ChineseSimplified;
                }
            }

            return GetDefaultLanguageCode();
        }

        public static LocalizedOption GetLanguageOption(string? languageCode)
        {
            string normalizedCode = NormalizeLanguageCode(languageCode);
            return LanguageOptions.FirstOrDefault(option => option.Value == normalizedCode)
                ?? LanguageOptions[0];
        }

        public static LocalizedOption GetFilterFieldOption(string? value)
        {
            return FilterFieldOptions.FirstOrDefault(
                    option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
                ?? FilterFieldOptions[0];
        }

        public static void ApplyLanguage(string? languageCode)
        {
            string normalizedCode = NormalizeLanguageCode(languageCode);
            CurrentLanguageCode = normalizedCode;

            CultureInfo culture = CultureInfo.GetCultureInfo(normalizedCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            if (Application.Current?.Resources == null)
            {
                return;
            }

            if (_currentLanguageResources != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(_currentLanguageResources);
            }

            _currentLanguageResources = new ResourceInclude(new Uri("avares://Litenbib/Localization/"))
            {
                Source = new Uri($"avares://Litenbib/Localization/Strings.{normalizedCode}.axaml"),
            };
            Application.Current.Resources.MergedDictionaries.Add(_currentLanguageResources);
        }
    }

    public static class I18n
    {
        public static string Get(string resourceKey)
        {
            if (Application.Current?.TryFindResource(resourceKey, out object? value) == true)
            {
                return value?.ToString() ?? resourceKey;
            }

            return resourceKey;
        }

        public static string Format(string resourceKey, params object?[] args)
        { return string.Format(CultureInfo.CurrentCulture, Get(resourceKey), args); }
    }
}
