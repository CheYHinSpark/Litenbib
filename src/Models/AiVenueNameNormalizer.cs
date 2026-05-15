using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    public enum VenueNameNormalizationMode
    {
        Expand,
        Abbreviate
    }

    public sealed record VenueNameNormalizationResult(
        bool Success,
        List<string> Values,
        string ErrorMessage)
    {
        public static VenueNameNormalizationResult Failed(string message)
        {
            return new VenueNameNormalizationResult(false, [], message);
        }
    }

    internal static partial class AiVenueNameNormalizer
    {
        private static readonly HttpClient client = new()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        public static async Task<VenueNameNormalizationResult> NormalizeAsync(
            VenueNameNormalizationMode mode,
            IReadOnlyList<VenueAbbreviationMapping> mappings,
            IReadOnlyList<string> venueNames)
        {
            AppSettings settings = AppSettingsState.Current;
            if (!AiBibtexExtractor.IsConfigured(settings))
            {
                return VenueNameNormalizationResult.Failed(I18n.Get("Message.AiSettingsIncomplete"));
            }

            if (mappings.Count == 0)
            {
                return VenueNameNormalizationResult.Failed(I18n.Get("VenueNormalize.Status.NoMappings"));
            }

            if (venueNames.Count == 0)
            {
                return VenueNameNormalizationResult.Failed(I18n.Get("VenueNormalize.Ai.NoVenueValues"));
            }

            if (!TryCreateChatCompletionsUri(settings.AiBaseUrl, out Uri? endpoint))
            {
                return VenueNameNormalizationResult.Failed(I18n.Get("Message.AiBaseUrlInvalid"));
            }

            string referenceTable = VenueAbbreviationMappings.ToPromptReferenceTable(mappings);
            string modeText = mode == VenueNameNormalizationMode.Expand ? "EXPAND" : "ABBREVIATE";
            string userPrompt = AiPrompts.BuildVenueNameNormalizationUserPrompt(
                modeText,
                referenceTable,
                venueNames);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AiApiKey);
                request.Content = new StringContent(
                    CreateRequestJson(settings.AiModelName, AiPrompts.VenueNameNormalizationSystem, userPrompt),
                    Encoding.UTF8,
                    "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(settings.OnlineLookupTimeoutSeconds));
                using var response = await client.SendAsync(request, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    return VenueNameNormalizationResult.Failed(I18n.Format("VenueNormalize.Ai.Failed", $"{(int)response.StatusCode} {response.ReasonPhrase}"));
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                string content = ExtractMessageContent(responseJson);
                if (!TryParseNumberedList(content, venueNames.Count, out List<string> values, out string errorMessage))
                {
                    return VenueNameNormalizationResult.Failed(errorMessage);
                }

                return new VenueNameNormalizationResult(true, values, string.Empty);
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine(ex);
                return VenueNameNormalizationResult.Failed(I18n.Get("VenueNormalize.Ai.TimedOut"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return VenueNameNormalizationResult.Failed(I18n.Format("VenueNormalize.Ai.Failed", ex.Message));
            }
        }

        private static string CreateRequestJson(string modelName, string systemPrompt, string userPrompt)
        {
            var payload = new
            {
                model = modelName,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = userPrompt
                    }
                },
                temperature = 0
            };

            return JsonSerializer.Serialize(payload);
        }

        private static bool TryCreateChatCompletionsUri(string baseUrl, out Uri? endpoint)
        {
            endpoint = null;
            string url = (baseUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            url = url.TrimEnd('/');
            if (!url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                url += "/chat/completions";
            }

            return Uri.TryCreate(url, UriKind.Absolute, out endpoint)
                && (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps);
        }

        private static string ExtractMessageContent(string responseJson)
        {
            using JsonDocument document = JsonDocument.Parse(responseJson);
            if (!document.RootElement.TryGetProperty("choices", out JsonElement choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            JsonElement choice = choices[0];
            if (choice.TryGetProperty("message", out JsonElement message)
                && message.TryGetProperty("content", out JsonElement content))
            {
                return JsonContentToString(content);
            }

            if (choice.TryGetProperty("text", out JsonElement text)
                && text.ValueKind == JsonValueKind.String)
            {
                return text.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string JsonContentToString(JsonElement content)
        {
            if (content.ValueKind == JsonValueKind.String)
            {
                return content.GetString() ?? string.Empty;
            }

            if (content.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            foreach (JsonElement item in content.EnumerateArray())
            {
                if (item.TryGetProperty("text", out JsonElement text)
                    && text.ValueKind == JsonValueKind.String)
                {
                    builder.AppendLine(text.GetString());
                }
            }

            return builder.ToString();
        }

        private static bool TryParseNumberedList(
            string content,
            int expectedCount,
            out List<string> values,
            out string errorMessage)
        {
            values = [];
            errorMessage = string.Empty;
            string[] lines = (content ?? string.Empty)
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string line in lines)
            {
                Match match = NumberedLineRegex().Match(line);
                if (!match.Success)
                {
                    errorMessage = I18n.Get("VenueNormalize.Ai.ExtraText");
                    return false;
                }

                int number = int.Parse(match.Groups[1].Value);
                if (number != values.Count + 1)
                {
                    errorMessage = I18n.Get("VenueNormalize.Ai.OutOfOrder");
                    return false;
                }

                values.Add(match.Groups[2].Value.Trim());
            }

            if (values.Count != expectedCount)
            {
                errorMessage = I18n.Format("VenueNormalize.Status.AiReturnedWrongCount", values.Count, expectedCount);
                return false;
            }

            if (values.Any(string.IsNullOrWhiteSpace))
            {
                errorMessage = I18n.Get("VenueNormalize.Ai.EmptyVenue");
                return false;
            }

            return true;
        }

        [GeneratedRegex(@"^\s*(\d+)[\.\)]\s*(.+?)\s*$", RegexOptions.Compiled)]
        private static partial Regex NumberedLineRegex();
    }
}
