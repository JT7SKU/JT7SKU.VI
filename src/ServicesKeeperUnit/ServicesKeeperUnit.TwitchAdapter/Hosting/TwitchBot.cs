using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace ServicesKeeperUnit.Twitch.Adapter.Hosting
{
    public class TwitchBot : IHostedService
    {
        private TwitchMiddleware Middleware;
        private TwitchClient _client;
        public TwitchBot(IOptions<TwitchOptions> options)
        {

        }
        public ILogger<TwitchBot> Logger { get; }
        public static TwitchOptions TwitchOptions { get; set; }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var creds = new ConnectionCredentials(TwitchOptions.BotUsername, TwitchOptions.OAuthToken);
            _client = new TwitchClient();
            _client.Initialize(creds);

            _client.OnConnected += (sender, args) => _client.JoinChannel(TwitchOptions.ChannelName);
            _client.OnNewSubscriber += Middleware.OnNewSubscriber;
            _client.OnReSubscriber += Middleware.OnReSubscriber;
            _client.OnGiftedSubscription += Middleware.OnGiftedSubscription;
            _client.OnRaidNotification += Middleware.OnRaidNotification;
            _client.OnChatCommandReceived += Middleware.OnChatCommandReveived;
            _client.OnMessageReceived += Middleware.OnMessageReviewved;
            _client.OnWhisperReceived += Middleware.OnWishperMessageReceived;

            _client.Connect();
            await StartTwitchMonitoring();
        }

        private Task StartTwitchMonitoring()
        {
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_client != null) _client.Disconnect();
            await StopTwitchMonitoring();
        }

        private Task StopTwitchMonitoring()
        {
            return Task.CompletedTask;
        }
    }
}
