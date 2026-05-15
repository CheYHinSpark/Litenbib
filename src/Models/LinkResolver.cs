using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    public class BibliographyCandidate
    {
        public string Source { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BibTeX { get; set; } = string.Empty;
        public BibtexEntry? Entry { get; set; }
    }

    public class BibliographyResolveResult
    {
        public List<BibliographyCandidate> Candidates { get; set; } = [];
        public string Hint { get; set; } = string.Empty;
        public bool Success => Candidates.Count > 0;
    }

    internal enum MergeSearchSource
    {
        Super,
        Doi,
        Dblp,
        Crossref,
        Title,
    }

    internal static class LinkResolver
    {
        private static readonly HttpClient client = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            HttpClient httpClient = new()
            {
                Timeout = System.Threading.Timeout.InfiniteTimeSpan
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Litenbib/0.0.1 (+https://github.com/CheYHinSpark/Litenbib)");
            return httpClient;
        }

        private static CancellationTokenSource CreateRequestTimeout()
        {
            return new CancellationTokenSource(TimeSpan.FromSeconds(AppSettingsState.Current.OnlineLookupTimeoutSeconds));
        }

        private static async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            using var cts = CreateRequestTimeout();
            return await client.SendAsync(request, cts.Token);
        }

        private static async Task<HttpResponseMessage> GetRequestAsync(string url)
        {
            using var cts = CreateRequestTimeout();
            return await client.GetAsync(url, cts.Token);
        }

        public static async Task<string> GetBibTeXAsync(string input)
        {
            var result = await ResolveAsync(input, maxCandidates: 1);
            return result.Candidates.FirstOrDefault()?.BibTeX ?? string.Empty;
        }

        public static async Task<BibliographyResolveResult> ResolveAsync(string input, int maxCandidates = 5)
        {
            BibliographyResolveResult result = new();
            string query = input?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
            {
                result.Hint = "Input is empty.";
                return result;
            }

            try
            {
                if (TryExtractDoi(query, out string doi))
                {
                    await AddIfResolved(result.Candidates, await ResolveDoiCandidatesAsync(doi), maxCandidates);
                }

                if (result.Candidates.Count < maxCandidates && TryExtractArxivId(query, out string arxivId))
                {
                    await AddIfResolved(result.Candidates, await ResolveArxivCandidatesAsync(arxivId), maxCandidates);
                }

                if (result.Candidates.Count < maxCandidates && TryExtractOpenReviewId(query, out string openReviewId))
                {
                    await AddIfResolved(result.Candidates, await ResolveOpenReviewCandidatesAsync(openReviewId), maxCandidates);
                }

                if (result.Candidates.Count < maxCandidates)
                {
                    await AddIfResolved(result.Candidates, await SearchDblpCandidatesAsync(query), maxCandidates);
                }

                if (result.Candidates.Count < maxCandidates)
                {
                    await AddIfResolved(result.Candidates, await SearchCrossrefCandidatesAsync(query), maxCandidates);
                }
            }
            catch (Exception ex)
            {
                NotifyLookupException("Bibliography search", ex);
                Debug.WriteLine(ex);
            }

            result.Hint = result.Candidates.Count == 0
                ? "No bibliographic result found from DOI / arXiv / OpenReview / DBLP / Crossref."
                : $"Resolved {result.Candidates.Count} candidate(s).";
            return result;
        }

        public static async Task<List<BibtexEntry>> ResolveEntriesAsync(string input, int maxCandidates = 5)
        {
            var result = await ResolveAsync(input, maxCandidates);
            return result.Candidates
                .Where(c => c.Entry != null)
                .Select(c => BibtexEntry.CopyFrom(c.Entry!))
                .ToList();
        }

        public static async Task<List<BibtexEntry>> SearchMergeCandidatesAsync(BibtexEntry entry, int maxCandidates = 8)
        {
            return await SearchMergeCandidatesAsync(entry, MergeSearchSource.Super, maxCandidates);
        }

        public static async Task<List<BibtexEntry>> SearchMergeCandidatesAsync(BibtexEntry entry, MergeSearchSource source, int maxCandidates = 8)
        {
            List<BibtexEntry> merged = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            async Task AddCandidatesAsync(IEnumerable<BibtexEntry> candidates)
            {
                foreach (var candidate in candidates)
                {
                    string key = candidate.CitationKey + "|" + candidate.Title;
                    if (seen.Add(key))
                    {
                        merged.Add(candidate);
                        if (merged.Count >= maxCandidates)
                        {
                            return;
                        }
                    }
                }

                await Task.CompletedTask;
            }

            async Task AddFromQueryAsync(string? query)
            {
                if (merged.Count >= maxCandidates || string.IsNullOrWhiteSpace(query))
                {
                    return;
                }

                var result = await ResolveEntriesAsync(query, maxCandidates);
                await AddCandidatesAsync(result);
            }

            async Task AddFromResolverAsync(Func<string, Task<List<BibliographyCandidate>>> resolver, string? query)
            {
                if (merged.Count >= maxCandidates || string.IsNullOrWhiteSpace(query))
                {
                    return;
                }

                var result = await resolver(query);
                await AddCandidatesAsync(result
                    .Where(c => c.Entry != null)
                    .Select(c => BibtexEntry.CopyFrom(c.Entry!)));
            }

            switch (source)
            {
                case MergeSearchSource.Doi:
                    await AddFromResolverAsync(ResolveDoiCandidatesAsync, entry.DOI);
                    break;
                case MergeSearchSource.Dblp:
                    await AddFromResolverAsync(SearchDblpCandidatesAsync, !string.IsNullOrWhiteSpace(entry.Title) ? entry.Title : entry.DOI);
                    break;
                case MergeSearchSource.Crossref:
                    await AddFromResolverAsync(SearchCrossrefCandidatesAsync, !string.IsNullOrWhiteSpace(entry.Title) ? entry.Title : entry.DOI);
                    break;
                case MergeSearchSource.Title:
                    await AddFromQueryAsync(entry.Title);
                    break;
                case MergeSearchSource.Super:
                default:
                    string[] parts = [entry.Title, entry.DOI, entry.Url, entry.CitationKey];
                    foreach (string part in parts)
                    {
                        if (merged.Count >= maxCandidates)
                        {
                            break;
                        }

                        await AddFromQueryAsync(part);
                    }
                    break;
            }

            return merged;
        }

        private static async Task AddIfResolved(List<BibliographyCandidate> target, List<BibliographyCandidate> source, int maxCandidates)
        {
            foreach (var candidate in source)
            {
                if (target.Count >= maxCandidates)
                {
                    return;
                }

                string key = (candidate.Entry?.CitationKey ?? string.Empty) + "|" + candidate.Title;
                bool exists = target.Any(t =>
                    string.Equals((t.Entry?.CitationKey ?? string.Empty) + "|" + t.Title, key, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(candidate.BibTeX) && string.Equals(t.BibTeX, candidate.BibTeX, StringComparison.Ordinal)));
                if (!exists)
                {
                    target.Add(candidate);
                }
            }

            await Task.CompletedTask;
        }

        private static async Task<List<BibliographyCandidate>> ResolveDoiCandidatesAsync(string doi)
        {
            List<BibliographyCandidate> result = [];

            string bibtex = await GetFromDoiAsync(doi);
            AddCandidateIfPossible(result, "DOI", doi, bibtex);

            if (result.Count == 0)
            {
                bibtex = await GetCrossrefBibtexByDoiAsync(doi);
                AddCandidateIfPossible(result, "Crossref DOI", doi, bibtex);
            }

            if (result.Count == 0)
            {
                var dblp = await SearchDblpCandidatesAsync(doi);
                result.AddRange(dblp);
            }

            return result;
        }

        private static async Task<List<BibliographyCandidate>> ResolveArxivCandidatesAsync(string arxivId)
        {
            List<BibliographyCandidate> result = [];

            string bibtex = await GetFromDoiAsync($"10.48550/ARXIV.{arxivId}");
            AddCandidateIfPossible(result, "arXiv DOI", arxivId, bibtex);

            if (result.Count == 0)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, $"https://arxiv.org/bibtex/{arxivId}");
                    var response = await SendRequestAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        bibtex = await response.Content.ReadAsStringAsync();
                        AddCandidateIfPossible(result, "arXiv", arxivId, bibtex);
                    }
                }
                catch (Exception ex)
                {
                    NotifyLookupException("arXiv", ex);
                    Debug.WriteLine(ex);
                }
            }

            return result;
        }

        private static async Task<List<BibliographyCandidate>> ResolveOpenReviewCandidatesAsync(string openReviewId)
        {
            List<BibliographyCandidate> result = [];
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.openreview.net/notes?id={Uri.EscapeDataString(openReviewId)}");
                var response = await SendRequestAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return result;
                }

                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("notes", out JsonElement notes)
                    || notes.GetArrayLength() == 0)
                {
                    return result;
                }

                JsonElement note = notes[0];
                string title = TryGetNestedString(note, "content", "title", "value")
                    ?? TryGetNestedString(note, "content", "title")
                    ?? string.Empty;
                string[] authors = TryGetNestedStringArray(note, "content", "authors", "value")
                    ?? TryGetNestedStringArray(note, "content", "authors")
                    ?? [];
                string year = TryGetNestedString(note, "tcdate") is string tcdate && long.TryParse(tcdate, out long ms)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ms).Year.ToString()
                    : DateTime.UtcNow.Year.ToString();

                if (string.IsNullOrWhiteSpace(title))
                {
                    return result;
                }

                BibtexEntry entry = new("misc", openReviewId)
                {
                    Title = title,
                    Year = year,
                    Author = string.Join(" and ", authors),
                    Url = $"https://openreview.net/forum?id={openReviewId}",
                    Note = "OpenReview"
                };
                entry.UpdateBibtex();
                result.Add(new BibliographyCandidate
                {
                    Source = "OpenReview",
                    Query = openReviewId,
                    Title = entry.Title,
                    BibTeX = entry.BibTeX,
                    Entry = entry,
                });
            }
            catch (Exception ex)
            {
                NotifyLookupException("OpenReview", ex);
                Debug.WriteLine(ex);
            }
            return result;
        }

        private static async Task<List<BibliographyCandidate>> SearchCrossrefCandidatesAsync(string query)
        {
            List<BibliographyCandidate> result = [];
            try
            {
                string url = $"https://api.crossref.org/works?rows=5&query.bibliographic={Uri.EscapeDataString(query)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await SendRequestAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return result;
                }

                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("message", out JsonElement message)
                    || !message.TryGetProperty("items", out JsonElement items))
                {
                    return result;
                }

                foreach (JsonElement item in items.EnumerateArray())
                {
                    string doi = TryGetString(item, "DOI") ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(doi))
                    {
                        continue;
                    }

                    string bibtex = await GetCrossrefBibtexByDoiAsync(doi);
                    if (string.IsNullOrWhiteSpace(bibtex))
                    {
                        bibtex = await GetFromDoiAsync(doi);
                    }
                    AddCandidateIfPossible(result, "Crossref", query, bibtex);
                    if (result.Count >= 5)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyLookupException("Crossref", ex);
                Debug.WriteLine(ex);
            }
            return result;
        }

        private static async Task<List<BibliographyCandidate>> SearchDblpCandidatesAsync(string query)
        {
            List<BibliographyCandidate> result = [];
            try
            {
                string normalized = query.Replace("{", "").Replace("}", "").Replace(":", " ");
                var response = await GetRequestAsync($"https://dblp.org/search/publ/api?q={Uri.EscapeDataString(normalized)}&format=json");
                if (!response.IsSuccessStatusCode)
                {
                    return result;
                }

                string json = await response.Content.ReadAsStringAsync();
                MatchCollection matches = Regex.Matches(json, @"""key""\s*:\s*""(.*?)""");
                foreach (Match match in matches)
                {
                    if (!match.Success)
                    {
                        continue;
                    }

                    var bibResponse = await GetRequestAsync($"https://dblp.org/rec/{match.Groups[1].Value}.bib");
                    if (!bibResponse.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    string bibtex = await bibResponse.Content.ReadAsStringAsync();
                    AddCandidateIfPossible(result, "DBLP", query, bibtex);
                    if (result.Count >= 5)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyLookupException("DBLP", ex);
                Debug.WriteLine(ex);
            }
            return result;
        }

        public static async Task<string> GetFromDoiAsync(string doi)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"https://doi.org/{doi}");
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-bibtex"));
                var response = await SendRequestAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                NotifyLookupException("DOI", ex);
                Debug.WriteLine(ex);
            }
            return string.Empty;
        }

        private static async Task<string> GetCrossrefBibtexByDoiAsync(string doi)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.crossref.org/works/{Uri.EscapeDataString(doi)}/transform/application/x-bibtex");
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-bibtex"));
                var response = await SendRequestAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                NotifyLookupException("Crossref DOI", ex);
                Debug.WriteLine(ex);
            }
            return string.Empty;
        }

        private static void NotifyLookupException(string source, Exception ex)
        {
            string message = ex is TaskCanceledException or TimeoutException
                ? I18n.Format("Message.LookupTimedOut", source)
                : I18n.Format("Message.LookupFailed", source, ex.Message);
            NotificationCenter.Error(message);
        }

        private static void AddCandidateIfPossible(List<BibliographyCandidate> list, string source, string query, string bibtex)
        {
            if (string.IsNullOrWhiteSpace(bibtex))
            {
                return;
            }

            BibtexEntry? entry = BibtexParser.ParseBibTeX(bibtex);
            if (entry == null)
            {
                var parsed = BibtexParser.Parse(bibtex);
                entry = parsed.FirstOrDefault();
            }
            if (entry == null)
            {
                return;
            }

            list.Add(new BibliographyCandidate
            {
                Source = source,
                Query = query,
                Title = entry.Title,
                BibTeX = entry.BibTeX,
                Entry = entry,
            });
        }

        private static bool TryExtractDoi(string input, out string doi)
        {
            Match match = Regex.Match(input, @"10\.\d{4,9}/[-._;()/:A-Z0-9]+", RegexOptions.IgnoreCase);
            doi = match.Success ? match.Value.Trim().TrimEnd('.', ',', ';') : string.Empty;
            return match.Success;
        }

        private static bool TryExtractArxivId(string input, out string arxivId)
        {
            Match match = Regex.Match(input, @"(?<=/|\b)(\d{4}\.\d{4,5}|[a-z\-]+(?:\.[A-Z]{2})?/\d{7})(v\d+)?(?=\b|$)", RegexOptions.IgnoreCase);
            arxivId = match.Success ? match.Groups[1].Value : string.Empty;
            return match.Success;
        }

        private static bool TryExtractOpenReviewId(string input, out string openReviewId)
        {
            Match match = Regex.Match(input, @"(?:openreview\.net/(?:forum|pdf)\?id=|\bid=)([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
            openReviewId = match.Success ? match.Groups[1].Value : string.Empty;
            return match.Success;
        }

        private static string? TryGetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) ? value.GetString() : null;
        }

        private static string? TryGetNestedString(JsonElement element, params string[] path)
        {
            JsonElement current = element;
            foreach (string part in path)
            {
                if (!current.TryGetProperty(part, out current))
                {
                    return null;
                }
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }

        private static string[]? TryGetNestedStringArray(JsonElement element, params string[] path)
        {
            JsonElement current = element;
            foreach (string part in path)
            {
                if (!current.TryGetProperty(part, out current))
                {
                    return null;
                }
            }

            if (current.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return current.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }
    }
}
