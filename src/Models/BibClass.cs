using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    internal class BibItem(string label)
    {
        public string Label = label;
        public Dictionary<string, string> Tags = [];

        public static BibItem FromDOI(string doi)
        {
            BibItem item = new(doi);
            item.Tags["doi"] = doi;
            // TODO: 解析DOI
            return item;
        }
    }

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
        public static async Task<string> ResolveDoiOfficialAsync(string doi)
        {
            var url = $"https://doi.org/{doi}";
            // also application/x-bibtex
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.citationstyles.csl+json");
            return await client.GetStringAsync(url);
        }

        /// <summary> TODO：使用DataCite路径 https://support.datacite.org/docs/api </summary>
        public static async Task<string> ResolveDoiDataCiteAsync(string doi)
        {
            var url = $"https://api.datacite.org/dois/{doi}";
            return await client.GetStringAsync(url);
        }
    }
}
