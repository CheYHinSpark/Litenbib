using CommunityToolkit.Mvvm.ComponentModel;
using Litenbib.Models;
using System.Collections.ObjectModel;

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

        [ObservableProperty]
        private string _onlineLookupTimeoutSeconds;

        [ObservableProperty]
        private string _fieldIndentSpaces;

        [ObservableProperty]
        private string _entryTypeCaseStyle;

        [ObservableProperty]
        private string _citationKeyTemplate;

        [ObservableProperty]
        private string _aiBaseUrl;

        [ObservableProperty]
        private string _aiApiKey;

        [ObservableProperty]
        private string _aiModelName;

        [ObservableProperty]
        private bool _useAiPdfImportFallback;

        public SettingsViewModel() : this(AppSettingsState.Current)
        {
        }

        public SettingsViewModel(AppSettings settings)
        {
            AppSettings normalized = AppSettings.Normalize(settings);
            OnlineLookupTimeoutSeconds = normalized.OnlineLookupTimeoutSeconds.ToString();
            FieldIndentSpaces = normalized.FieldIndentSpaces.ToString();
            EntryTypeCaseStyle = normalized.EntryTypeCaseStyle;
            CitationKeyTemplate = normalized.CitationKeyTemplate;
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
            {
                timeoutSeconds = parsedTimeoutSeconds;
            }
            if (int.TryParse(FieldIndentSpaces, out int parsedIndentSpaces))
            {
                indentSpaces = parsedIndentSpaces;
            }

            return AppSettings.Normalize(new AppSettings
            {
                OnlineLookupTimeoutSeconds = timeoutSeconds,
                FieldIndentSpaces = indentSpaces,
                EntryTypeCaseStyle = EntryTypeCaseStyle,
                CitationKeyTemplate = CitationKeyTemplate,
                AiBaseUrl = AiBaseUrl,
                AiApiKey = AiApiKey,
                AiModelName = AiModelName,
                UseAiPdfImportFallback = UseAiPdfImportFallback,
            });
        }
    }
}
