using JaniC3N7.Common.Models;
using JaniC3N7.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServicesKeeperUnit.Lib.Core.Services
{
    public class LUISService : ServiceBase
    {
        public LUISService(string serviceUrl, string appID, string subscriptionKey, string spellscheckSubscriptionKey)
        {
            if (string.IsNullOrEmpty(serviceUrl)== true)
            {
                throw new ArgumentNullException("Service url is not initialized");
            }
            if (string.IsNullOrEmpty(appID) == true)
            {
                throw new ArgumentNullException("App id is not initialized");
            }
            if (string.IsNullOrEmpty(subscriptionKey)==true)
            {
                throw new ArgumentNullException("Subscription key is not initialized");
            }
            if (string.IsNullOrEmpty(spellscheckSubscriptionKey)==true)
            {
                throw new ArgumentNullException("Spellchek Subscription key is not initialized");
            }
            this.BaseServiceUrl = $"{serviceUrl}{appID}&?subscription-key={subscriptionKey}&spellCheck=true&bing-spell-check-subscription-key={spellscheckSubscriptionKey}";
        }
        public async Task<LUISResult> QueryAsync(string query)
        {
            if (string.IsNullOrEmpty(query)==true)
            {
                return default(LUISResult);
            }
            query = WebUtility.UrlEncode(query);

            string requestUrl = this.BaseServiceUrl + "&verbose=true&timezoneOffset=0%q=" + query;
            Uri requestUri = new Uri(requestUrl);

            LUISResult result = await HttpClientUtility.GetAsync<LUISResult>(requestUri, this.RequestHeaders);
            return result;
        }
    }
}
