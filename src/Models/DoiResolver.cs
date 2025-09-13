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
    public class DoiResolver
    {
        private static readonly HttpClient client = new();

        /// <summary> TODO：使用CrossRef查询 https://www.crossref.org/documentation/retrieve-metadata/rest-api/ </summary>
        public static async Task<string?> ResolveDoiCrossRefAsync(string doi)
        {
            try
            {
                // CrossRef API 端点
                var url = $"https://api.crossref.org/works/{doi}";
                return await client.GetStringAsync(url);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"错误: {e.Message}");
                return null;
            }
        }

        /// <summary> 使用DOI官方路径 https://citation.doi.org/docs.html </summary>
        public static async Task<string> ResolveDoiAsync(string doi)
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

        /// <summary> 将输入的DOI或arXiv链接转换成bibtex </summary>
        /// <param name="doi"></param>
        /// <returns></returns>
        public static async Task<string> GetBibTeXAsync(string doi)
        {
            // 首先，尝试使用DOI直接解析
            var url = $"https://doi.org/{doi}";
            client.DefaultRequestHeaders.Add("Accept", "application/x-bibtex");
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            // 接着，假设这是一个arxiv链接
            string arxivID = doi.Split('/')[^1];
            // arxiv链接但是通过DOI解析
            url = $"https://doi.org/10.48550/ARXIV.{arxivID}";
            client.DefaultRequestHeaders.Add("Accept", "application/x-bibtex");
            response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            // 通过arxiv解析
            url = $"http://arxiv.org/bibtex/{arxivID}";
            Debug.WriteLine(url);
            response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

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

        /// <summary> TODO：使用DataCite路径 https://support.datacite.org/docs/api </summary>
        public static async Task<string> ResolveDoiDataCiteAsync(string doi)
        {
            var url = $"https://api.datacite.org/dois/{doi}";
            return await client.GetStringAsync(url);
        }
    }
}
