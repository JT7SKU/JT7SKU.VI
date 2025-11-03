using JaniC3N7.Common.Models;
using JaniC3N7.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ServicesKeeperUnit.Lib.Core.Services
{
    public class QnAMakerService : ServiceBase
    {
        public QnAMakerService(string serviceUrl, string subscriptionKey)
        {
            if (string.IsNullOrEmpty(serviceUrl)== true)
            {
                throw new ArgumentNullException("Service url is not initialized");
            }
            if (string.IsNullOrEmpty(subscriptionKey)==true)
            {
                throw new ArgumentNullException("Subscription Key is not initialized");
            }
            this.BaseServiceUrl = serviceUrl;
            this.RequestHeaders = new Dictionary<string, string>()
            {
                {this.HEADER_SUB_KEY, subscriptionKey }
            };
        }
        public async Task<QnAMakerSearchResult> GenerateAnswersAsync(string query, string knowledgebaseId)
        {
            if (string.IsNullOrEmpty(query)==true)
            {
                return default(QnAMakerSearchResult);
            }
            string requestUrl = Path.Combine(this.BaseServiceUrl, "knowledgebases", knowledgebaseId, "generateAnswer");
            Uri requestUri = new Uri(requestUrl);

            QnAMakerSearchRequest content = new QnAMakerSearchRequest()
            {
                Question = query
            };
            QnAMakerSearchResult result = await HttpClientUtility.PostJsonAsync<QnAMakerSearchResult>(requestUri, this.RequestHeaders, content);
            return result;
        }
    }
}
