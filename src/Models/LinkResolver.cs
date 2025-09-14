using Avalonia;
using MsBox.Avalonia.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    internal class LinkResolver
    {
        private static readonly HttpClient client = new();

        ///// <summary> TODO：使用CrossRef查询 https://www.crossref.org/documentation/retrieve-metadata/rest-api/ </summary>
        //public static async Task<string?> ResolveDoiCrossRefAsync(string doi)
        //{
        //    try
        //    {
        //        // CrossRef API 端点
        //        var url = $"https://api.crossref.org/works/{doi}";
        //        return await client.GetStringAsync(url);
        //    }
        //    catch (HttpRequestException)
        //    {
        //        return null;
        //    }
        //}

        /// <summary> 使用DOI官方路径 https://citation.doi.org/docs.html </summary>
        public static async Task<string> GetFromDoiAsync(string doi)
        {
            var url = $"https://doi.org/{doi}";
            client.DefaultRequestHeaders.Add("Accept", "application/x-bibtex");
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return string.Empty;
        }

        ///// <summary> TODO：使用DataCite路径 https://support.datacite.org/docs/api </summary>
        //public static async Task<string> ResolveDoiDataCiteAsync(string doi)
        //{
        //    var url = $"https://api.datacite.org/dois/{doi}";
        //    return await client.GetStringAsync(url);
        //}

        /// <summary> 将输入的DOI或arXiv链接转换成bibtex </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public static async Task<string> GetBibTeXAsync(string link)
        {
            string result;

            // 首先，尝试使用DOI直接解析
            result = await GetFromDoiAsync(link);
            if (!string.IsNullOrWhiteSpace(result)) { return result; }

            // 接着，假设这是一个arxiv链接，直接截取后面部分
            string pattern = @"(?<=\/| |^)(\w+\/\d{7}|\d{4}\.\d{4,5})(v\d+)?(?=\.|\s|$)";
            Match match = Regex.Match(link, pattern);
            if (match.Success)
            {
                string arxivId = match.Groups[1].Value;

                // arxiv链接但是通过DOI解析
                result = await GetFromDoiAsync($"10.48550/ARXIV.{arxivId}");
                if (!string.IsNullOrWhiteSpace(result)) { return result; }

                // 通过arxiv解析
                var response = await client.GetAsync($"http://arxiv.org/bibtex/{arxivId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
           

            //// 尝试使用Crossref或得更新的DOI

            //// Construct the API URL. We need to specify the fields we want.
            //string requestUrl = $"https://api.semanticscholar.org/graph/v1/paper/ARXIV:{arxivId}" +
            //    $"?fields=paperId,externalIds";
            //client.DefaultRequestHeaders.Clear();
            //response = await client.GetAsync(requestUrl);
            ////response.EnsureSuccessStatusCode();

            //string jsonString = await response.Content.ReadAsStringAsync();
            //return jsonString;
            ////var paperData = JsonSerializer.Deserialize<PaperResponse>(jsonString);

            //// Now, build the BibTeX string from the parsed data
            ////return GenerateBibtex(paperData);
            //// 如果Crossref没有，可以返回null或尝试其他数据源（如Semantic Scholar, NASA ADS）
            ////return null;





            // TODO更多的链接，从链接提取
            // 尝试从常见学术URL中提取DOI
            //var patterns = new[]
            //{
            //    @"doi\.org/(?<doi>10\.\d+/[^\s/]+)",
            //    @"dx\.doi\.org/(?<doi>10\.\d+/[^\s/]+)",
            //    @"onlinelibrary\.wiley\.com/doi/(?<doi>10\.\d+/[^\s/]+)",
            //    @"link\.springer\.com/article/(?<doi>10\.\d+/[^\s/]+)",
            //    @"tandfonline\.com/doi/(?<doi>10\.\d+/[^\s/]+)",
            //    @"sciencedirect\.com/science/article/pii/[^\?]+\?.*doi=(?<doi>10\.\d+/[^\s/&]+)"
            //};

            //foreach (var pattern in patterns)
            //{
            //    var match = Regex.Match(url, pattern, RegexOptions.IgnoreCase);
            //    if (match.Success)
            //    {
            //        return match.Groups["doi"].Value;
            //    }
            //}

            return string.Empty;
        }

        
    }
}
