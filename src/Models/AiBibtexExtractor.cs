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
    internal static partial class AiBibtexExtractor
    {
        private static readonly HttpClient client = new()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        public static bool IsConfigured(AppSettings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.AiBaseUrl)
                && !string.IsNullOrWhiteSpace(settings.AiApiKey)
                && !string.IsNullOrWhiteSpace(settings.AiModelName);
        }

        public static async Task<BibtexEntry?> ExtractFromPdfFirstPageAsync(string firstPageText)
        {
            var entries = await ExtractEntriesAsync(
                firstPageText,
                AiPrompts.PdfFirstPageToBibtexSystem,
                AiPrompts.BuildPdfFirstPageToBibtexUserPrompt(firstPageText));
            return entries.FirstOrDefault();
        }

        public static async Task<List<BibtexEntry>> ExtractEntriesFromReferenceTextAsync(string referenceText)
        {
            return await ExtractEntriesAsync(
                referenceText,
                AiPrompts.ReferenceTextToBibtexSystem,
                AiPrompts.BuildReferenceTextToBibtexUserPrompt(referenceText));
        }

        private static async Task<List<BibtexEntry>> ExtractEntriesAsync(string sourceText, string systemPrompt, string userPrompt)
        {
            List<BibtexEntry> empty = [];
            AppSettings settings = AppSettingsState.Current;
            if (!IsConfigured(settings))
            {
                NotificationCenter.Info("AI settings are incomplete");
                return empty;
            }

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return empty;
            }

            if (!TryCreateChatCompletionsUri(settings.AiBaseUrl, out Uri? endpoint))
            {
                NotificationCenter.Error("AI base URL is invalid");
                return empty;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AiApiKey);
                request.Content = new StringContent(CreateRequestJson(settings.AiModelName, systemPrompt, userPrompt), Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(settings.OnlineLookupTimeoutSeconds));
                using var response = await client.SendAsync(request, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    NotificationCenter.Error($"AI extraction failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                    return empty;
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                string content = ExtractMessageContent(responseJson);
                string bibtex = ExtractBibtexText(content);
                if (string.Equals(bibtex, "EMPTY", StringComparison.OrdinalIgnoreCase))
                {
                    return empty;
                }

                List<BibtexEntry> entries = BibtexParser.Parse(bibtex);
                if (entries.Count == 0 && BibtexParser.ParseBibTeX(bibtex) is BibtexEntry entry)
                {
                    entries.Add(entry);
                }

                if (entries.Count == 0)
                {
                    NotificationCenter.Error("AI did not return valid BibTeX");
                    return empty;
                }

                return entries;
            }
            catch (TaskCanceledException ex)
            {
                NotificationCenter.Error("AI extraction timed out");
                Debug.WriteLine(ex);
            }
            catch (Exception ex)
            {
                NotificationCenter.Error($"AI extraction failed: {ex.Message}");
                Debug.WriteLine(ex);
            }

            return empty;
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

        private static string ExtractBibtexText(string content)
        {
            string text = (content ?? string.Empty).Trim();
            Match fenced = FencedCodeRegex().Match(text);
            if (fenced.Success)
            {
                text = fenced.Groups[1].Value.Trim();
            }

            int bibtexStart = text.IndexOf('@');
            if (bibtexStart >= 0)
            {
                text = text[bibtexStart..].Trim();
            }

            return text;
        }

        [GeneratedRegex(@"```(?:bibtex|bib)?\s*(.*?)```", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex FencedCodeRegex();
    }
}
