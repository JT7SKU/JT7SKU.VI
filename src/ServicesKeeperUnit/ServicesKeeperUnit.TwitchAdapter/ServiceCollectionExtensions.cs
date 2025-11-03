using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServicesKeeperUnit.Twitch.Adapter.ChatBot;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServicesKeeperUnit.Twitch.Adapter
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTwitchAdapter(this IServiceCollection collection, Action<TwitchOptions> contentDelegate)
        {
            collection.AddSingleton<IHostedService>();

            collection.AddSingleton<TwitchAdapter>();
            collection.AddHttpClient<Proxy>();
            collection.AddSingleton<ChatClient>();
            collection.AddOptions();
            collection.Configure(contentDelegate);
        }
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Convert a Unix timestamp to a .NET DateTime
        /// </summary>
        /// <param name="unixTime"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this long unixTime)
        {
            return epoch.AddSeconds(unixTime);
        }
    }
}
