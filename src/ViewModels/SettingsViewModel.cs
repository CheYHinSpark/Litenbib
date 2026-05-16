using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Litenbib.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public static ObservableCollection<string> EntryTypeCaseStyleList { get; } =
        [
            EntryTypeCaseStyles.Lowercase,
            EntryTypeCaseStyles.TitleCase,
            EntryTypeCaseStyles.Uppercase
        ];

        public static ObservableCollection<string> PdfFilePathStyleList { get; } =
        [
            PdfFilePathStyles.None,
            PdfFilePathStyles.AbsolutePath,
            PdfFilePathStyles.JabRef
        ];

        public static ObservableCollection<LocalizedOption> LanguageList { get; } =
            [.. LocalizationManager.LanguageOptions];

        public static ObservableCollection<LocalizedOption> ThemeModeList { get; } =
        [
            new(ThemeModes.Dark, "Settings.Theme.Dark"),
            new(ThemeModes.Light, "Settings.Theme.Light"),
        ];

        [ObservableProperty]
        private LocalizedOption _selectedLanguage = LocalizationManager.GetLanguageOption(null);

        [ObservableProperty]
        private LocalizedOption _selectedThemeMode = GetThemeModeOption(null);

        [ObservableProperty]
        private string _onlineLookupTimeoutSeconds = string.Empty;

        [ObservableProperty]
        private string _fieldIndentSpaces = string.Empty;

        [ObservableProperty]
        private string _entryTypeCaseStyle = string.Empty;

        [ObservableProperty]
        private string _pdfFilePathStyle = string.Empty;

        [ObservableProperty]
        private string _citationKeyTemplate = string.Empty;

        [ObservableProperty]
        private string _citationKeyDuplicateSuffix = string.Empty;

        [ObservableProperty]
        private bool _requireBatchOperationConfirmation;

        [ObservableProperty]
        private int _exportAuthorFormat;

        [ObservableProperty]
        private int _exportAuthorClip;

        [ObservableProperty]
        private int _exportMaxAuthors;

        [ObservableProperty]
        private string _exportEnding = string.Empty;

        [ObservableProperty]
        private string _aiBaseUrl = string.Empty;

        [ObservableProperty]
        private string _aiApiKey = string.Empty;

        [ObservableProperty]
        private string _aiModelName = string.Empty;

        [ObservableProperty]
        private bool _useAiPdfImportFallback;

        public SettingsViewModel() : this(AppSettingsState.Current) { }

        public SettingsViewModel(AppSettings settings)
        {
            LoadFromSettings(settings);
        }

        private void LoadFromSettings(AppSettings settings)
        {
            AppSettings normalized = AppSettings.Normalize(settings);
            SelectedLanguage = LocalizationManager.GetLanguageOption(normalized.LanguageCode);
            SelectedThemeMode = GetThemeModeOption(normalized.ThemeMode);
            OnlineLookupTimeoutSeconds = normalized.OnlineLookupTimeoutSeconds.ToString();
            FieldIndentSpaces = normalized.FieldIndentSpaces.ToString();
            EntryTypeCaseStyle = normalized.EntryTypeCaseStyle;
            PdfFilePathStyle = normalized.PdfFilePathStyle;
            CitationKeyTemplate = normalized.CitationKeyTemplate;
            CitationKeyDuplicateSuffix = normalized.CitationKeyDuplicateSuffix;
            RequireBatchOperationConfirmation = normalized.RequireBatchOperationConfirmation;
            ExportAuthorFormat = normalized.ExportAuthorFormat;
            ExportAuthorClip = normalized.ExportAuthorClip;
            ExportMaxAuthors = normalized.ExportMaxAuthors;
            ExportEnding = normalized.ExportEnding;
            AiBaseUrl = normalized.AiBaseUrl;
            AiApiKey = normalized.AiApiKey;
            AiModelName = normalized.AiModelName;
            UseAiPdfImportFallback = normalized.UseAiPdfImportFallback;
        }

        private static LocalizedOption GetThemeModeOption(string? value)
        {
            return ThemeModeList.FirstOrDefault(option => option.Value == value)
                ?? ThemeModeList[0];
        }

        public AppSettings ToSettings()
        {
            int timeoutSeconds = AppSettingsState.Current.OnlineLookupTimeoutSeconds;
            int indentSpaces = AppSettingsState.Current.FieldIndentSpaces;
            if (int.TryParse(OnlineLookupTimeoutSeconds, out int parsedTimeoutSeconds))
            { timeoutSeconds = parsedTimeoutSeconds; }
            if (int.TryParse(FieldIndentSpaces, out int parsedIndentSpaces))
            { indentSpaces = parsedIndentSpaces; }

            return AppSettings.Normalize(new AppSettings
            {
                OnlineLookupTimeoutSeconds = timeoutSeconds,
                FieldIndentSpaces = indentSpaces,
                LanguageCode = SelectedLanguage.Value,
                ThemeMode = SelectedThemeMode.Value,
                EntryTypeCaseStyle = EntryTypeCaseStyle,
                PdfFilePathStyle = PdfFilePathStyle,
                CitationKeyTemplate = CitationKeyTemplate,
                CitationKeyDuplicateSuffix = CitationKeyDuplicateSuffix,
                RequireBatchOperationConfirmation = RequireBatchOperationConfirmation,
                ExportAuthorFormat = ExportAuthorFormat,
                ExportAuthorClip = ExportAuthorClip,
                ExportMaxAuthors = ExportMaxAuthors,
                ExportEnding = ExportEnding,
                AiBaseUrl = AiBaseUrl,
                AiApiKey = AiApiKey,
                AiModelName = AiModelName,
                UseAiPdfImportFallback = UseAiPdfImportFallback,
            });
        }

        [RelayCommand]
        private void OpenConfigFolder()
        {
            try
            {
                Directory.CreateDirectory(AppPaths.ConfigDirectory);
                UriProcessor.StartProcess(AppPaths.ConfigDirectory);
            }
            catch (System.Exception ex)
            {
                NotificationCenter.Error(I18n.Format("Message.CouldNotOpenConfigFolder", ex.Message));
            }
        }

        [RelayCommand]
        private void OpenAbbreviationMappings()
        {
            try
            {
                VenueAbbreviationMappings.EnsureFileExists();
                UriProcessor.StartProcess(AppPaths.AbbreviationMappingsPath);
            }
            catch (System.Exception ex)
            {
                NotificationCenter.Error(I18n.Format("Message.CouldNotOpenAbbreviationMappings", ex.Message));
            }
        }

        [RelayCommand]
        private void ResetSettings()
        {
            LoadFromSettings(new AppSettings());
        }
    }
}
