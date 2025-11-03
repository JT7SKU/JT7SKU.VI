using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace ServicesKommunityUnit.AskWho.Utilities
{
    public static class SettingsUtility
    {
        private static readonly IConfiguration ConfigurationManager;
        public static string AzureSearchServiceName => ConfigurationManager["AzureSearchServiceName"]?.ToString();
        public static string AzureSearchServiceKey => ConfigurationManager["AzureSearchServiceKey"]?.ToString();
        public static string AzureSearchIndexName => ConfigurationManager["AzureSearchIndexName"]?.ToString();

        
        public static string BingSearchUrl => ConfigurationManager["AzureSearchServiceKey"].ToString();
        public static string BingSearchSubscriptionKey => ConfigurationManager["AzureSearchIndexName"]?.ToString();

        public static int MaxResultsCount => Int32.Parse(ConfigurationManager["MaxResultsCount"]?.ToString());

        public static string QnAMakerUrl => ConfigurationManager["QnAMakerUrl"]?.ToString();
        public static string QnAMakerSubscriptionKey => ConfigurationManager["QnAMakerSubscriptionKey"]?.ToString();
        public static string QnAMakerWhoTaskKnowledgeBaseId => ConfigurationManager["QnAMakerWhoTaskKnoledgeBaseId"].ToString();

        public static string TextAnalyticsUrl => ConfigurationManager["TextAnalycsUrl"]?.ToString();
        public static string TextAnalyticsSubscriptionKey => ConfigurationManager["TextAnalyticsSubscriptionKey"]?.ToString();

        public static string TranslatorUrl => ConfigurationManager["TranslatorUrl"]?.ToString();
        public static string TranslatorSubscriptionKey => ConfigurationManager["TranslatorSubscriptionKey"]?.ToString();

        public static string VideoIndexerUrl => ConfigurationManager["VideoIndexerUrl"]?.ToString();
        public static string VideoIndexerSubscriptionKey => ConfigurationManager["VideoIndexerSubscriptionKey"]?.ToString();
    }
}
