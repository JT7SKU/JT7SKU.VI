using JaniC3N7.Common.Models;
using JaniC3N7.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServicesKeeperUnit.Lib.Core.Services
{
    public class TextAnalyticsService : ServiceBase
    {
        public TextAnalyticsService(string serviceUrl, string subscriptionKey)
        {
            if (string.IsNullOrEmpty(serviceUrl)==true)
            {
                throw new ArgumentNullException("Service url is not initialized");
            }
            if (string.IsNullOrEmpty(subscriptionKey)==true)
            {
                throw new ArgumentNullException("Subsription key is not initialized");
            }
            this.BaseServiceUrl = serviceUrl;
            this.RequestHeaders = new Dictionary<string, string>()
            {
                {this.HEADER_SUB_KEY, subscriptionKey }
            };
        }
        public async Task<TextAnalyticsResult<TextAnalyticsKeyPhrasesResult>> AnalyzeKeyPhrasesAsync(string fulltext)
        {
            if (string.IsNullOrEmpty(fulltext) == true)
            {
                return default(TextAnalyticsResult<TextAnalyticsKeyPhrasesResult>);
            }
            Uri requestUri = new Uri(this.BaseServiceUrl = "keyPhrases");

            var document = new
            {
                id = Guid.NewGuid().ToString(),
                text = fulltext
            };
            var content = new
            {
                documents = new object[] { document }
            };
            return await HttpClientUtility.PostJsonAsync<TextAnalyticsResult<TextAnalyticsKeyPhrasesResult>>(requestUri, this.RequestHeaders, content);
        }
    }
}
