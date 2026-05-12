using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Litenbib.Models;
using System.Collections.ObjectModel;
using System.IO;

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

        [ObservableProperty]
        private string _onlineLookupTimeoutSeconds;

        [ObservableProperty]
        private string _fieldIndentSpaces;

        [ObservableProperty]
        private string _entryTypeCaseStyle;

        [ObservableProperty]
        private string _pdfFilePathStyle;

        [ObservableProperty]
        private string _citationKeyTemplate;

        [ObservableProperty]
        private string _citationKeyDuplicateSuffix;

        [ObservableProperty]
        private bool _requireBatchOperationConfirmation;

        [ObservableProperty]
        private string _aiBaseUrl;

        [ObservableProperty]
        private string _aiApiKey;

        [ObservableProperty]
        private string _aiModelName;

        [ObservableProperty]
        private bool _useAiPdfImportFallback;

        public SettingsViewModel() : this(AppSettingsState.Current) { }

        public SettingsViewModel(AppSettings settings)
        {
            AppSettings normalized = AppSettings.Normalize(settings);
            OnlineLookupTimeoutSeconds = normalized.OnlineLookupTimeoutSeconds.ToString();
            FieldIndentSpaces = normalized.FieldIndentSpaces.ToString();
            EntryTypeCaseStyle = normalized.EntryTypeCaseStyle;
            PdfFilePathStyle = normalized.PdfFilePathStyle;
            CitationKeyTemplate = normalized.CitationKeyTemplate;
            CitationKeyDuplicateSuffix = normalized.CitationKeyDuplicateSuffix;
            RequireBatchOperationConfirmation = normalized.RequireBatchOperationConfirmation;
            AiBaseUrl = normalized.AiBaseUrl;
            AiApiKey = normalized.AiApiKey;
            AiModelName = normalized.AiModelName;
            UseAiPdfImportFallback = normalized.UseAiPdfImportFallback;
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
                EntryTypeCaseStyle = EntryTypeCaseStyle,
                PdfFilePathStyle = PdfFilePathStyle,
                CitationKeyTemplate = CitationKeyTemplate,
                CitationKeyDuplicateSuffix = CitationKeyDuplicateSuffix,
                RequireBatchOperationConfirmation = RequireBatchOperationConfirmation,
                AiBaseUrl = AiBaseUrl,
                AiApiKey = AiApiKey,
                AiModelName = AiModelName,
                UseAiPdfImportFallback = UseAiPdfImportFallback,
            });
        }

        [RelayCommand]
        private void OpenAbbreviationMappings()
        {
            try
            {
                Directory.CreateDirectory(AppPaths.ConfigDirectory);
                if (!File.Exists(AppPaths.AbbreviationMappingsPath))
                {
                    File.WriteAllText(
                        AppPaths.AbbreviationMappingsPath,
                        "NeurIPS=Advances in Neural Information Processing Systems\n");
                }

                UriProcessor.StartProcess(AppPaths.AbbreviationMappingsPath);
            }
            catch (System.Exception ex)
            {
                NotificationCenter.Error($"Could not open abbreviation mappings: {ex.Message}");
            }
        }
    }
}
