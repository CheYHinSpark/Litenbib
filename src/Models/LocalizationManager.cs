using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Litenbib.Models
{
    public sealed class LocalizedOption(string value, string resourceKey) : INotifyPropertyChanged
    {
        public string Value { get; } = value;

        public string ResourceKey { get; } = resourceKey;

        public string DisplayName => I18n.Get(ResourceKey);

        public event PropertyChangedEventHandler? PropertyChanged;

        internal void RefreshDisplayName()
        {
            OnPropertyChanged(nameof(DisplayName));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public static IReadOnlyList<LocalizedOption> FilterModeOptions { get; } =
        [
            new("And", "Main.Filter.And"),
            new("Or", "Main.Filter.Or"),
            new("All", "Main.Filter.All"),
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
            NotifyLocalizedOptionsChanged();
        }

        private static void NotifyLocalizedOptionsChanged()
        {
            foreach (var option in LanguageOptions
                .Concat(FilterModeOptions)
                .Concat(FilterFieldOptions))
            {
                option.RefreshDisplayName();
            }
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
