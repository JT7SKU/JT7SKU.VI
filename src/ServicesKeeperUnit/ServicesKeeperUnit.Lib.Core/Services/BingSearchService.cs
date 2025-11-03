using JaniC3N7.Common.Models;
using JaniC3N7.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServicesKeeperUnit.Lib.Core.Services
{
    public class BingSearchService : ServiceBase
    {
        public enum SafeSearch
        {
            Off, Moderate, Strict
        }

        public BingSearchService(string serviceUrl, string subscriptionKey)
        {
            if (string.IsNullOrEmpty(serviceUrl)==true)
            {
                throw new ArgumentNullException("Service url is not initialized.");
            }
            if (string.IsNullOrEmpty(subscriptionKey)==true)
            {
                throw new ArgumentNullException("Subscribtion key isnt initialized.");
            }
            this.BaseServiceUrl = serviceUrl;
            this.RequestHeaders = new Dictionary<string, string>()
            {
                {this.HEADER_SUB_KEY, subscriptionKey }
            };
        }
        public async Task<BingWebSearchResult> SearchWebAsync(string query, int count= 10, int offset = 0, string market ="en-us", SafeSearch safeSearch = SafeSearch.Strict)
        {
            if (string.IsNullOrEmpty(query)==true)
            {
                return default(BingWebSearchResult);
            }
            else if (query.Length > MAX_QUERY_LENGHT)
            {
                query = query.Substring(0, MAX_QUERY_LENGHT);
            }

            query = WebUtility.UrlEncode(query);

            string searchUrl = $"{this.BaseServiceUrl}search?q={query}&offset={count}&mkt={market}&safesearch={safeSearch}";
            Uri requestUri = new Uri(searchUrl);

            BingWebSearchResult result = await HttpClientUtility.GetAsync<BingWebSearchResult>(requestUri, this.RequestHeaders);
            return result;
        }
    }
}
