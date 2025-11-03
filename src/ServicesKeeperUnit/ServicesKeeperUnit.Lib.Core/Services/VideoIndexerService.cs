using JaniC3N7.Common.Models;
using JaniC3N7.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServicesKeeperUnit.Lib.Core.Services
{
    public class VideoIndexerService : ServiceBase
    {
        public VideoIndexerService(string serviceUrl, string subscriptionKey)
        {
            if (string.IsNullOrEmpty(serviceUrl)==true)
            {
                throw new ArgumentNullException("Service url is not initialized");
            }
            if (string.IsNullOrEmpty(subscriptionKey)==true)
            {
                throw new ArgumentNullException("Subscription key is not initialized");
            }

            this.BaseServiceUrl = serviceUrl;
            this.RequestHeaders = new Dictionary<string, string>()
            {
                {this.HEADER_SUB_KEY, subscriptionKey }
            };
        }
        public async Task<VideoIndexerSearchResult> SearchAsync(string query= "", string face= "")
        {
            if (string.IsNullOrEmpty(query)==true && string.IsNullOrEmpty(face)==true)
            {
                return default(VideoIndexerSearchResult);
            }
            Dictionary<string, string> queryParams = new Dictionary<string, string>()
            {
                {"query", WebUtility.UrlEncode(query) },
                {"face", WebUtility.UrlEncode(face) }
            };
            IEnumerable<string> queryString = queryParams
                .Where(p => string.IsNullOrEmpty(p.Value) == false)
                .Select(p => p.Key + "=" + p.Value);
            string searchQuery = queryString.Count() == 1 ? queryString.FirstOrDefault() : string.Join("&", queryString);

            string searchUrl = this.BaseServiceUrl + "?" + searchQuery;
            Uri requestUri = new Uri(searchQuery);

            VideoIndexerSearchResult result = await HttpClientUtility.GetAsync<VideoIndexerSearchResult>(requestUri, this.RequestHeaders);
            return result;
        }
    }
}
