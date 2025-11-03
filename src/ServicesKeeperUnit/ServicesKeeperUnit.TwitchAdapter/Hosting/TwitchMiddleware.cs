using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicesKeeperUnit.Twitch.Adapter.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using IMiddleware = Microsoft.AspNetCore.Http.IMiddleware;
namespace ServicesKeeperUnit.Twitch.Adapter.Hosting
{
    public class TwitchMiddleware : IMiddleware
    {
        private readonly ILogger<TwitchMiddleware> logger;
        private readonly IBot bot;
        private readonly TwitchAdapter twitchAdapter;
        private readonly TwitchOptions twitchOptions;
        
        public TwitchMiddleware(IOptions<TwitchOptions> options, ILogger<TwitchMiddleware> _logger, IBot botti, TwitchAdapter twitchAdapteri)
        {
            logger = _logger;
            bot = botti;
            twitchAdapter = twitchAdapteri;
            twitchOptions = options.Value;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            await next(context);
        }
        public async void OnWishperMessageReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.Username == twitchOptions.BotUsername)
            {
                return;
            }
            else
            {
                await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity
                {
                    Text = e.WhisperMessage.Message,
                    Type = "message",
                    From = new ChannelAccount(e.WhisperMessage.UserId, e.WhisperMessage.Username),
                    Recipient = new ChannelAccount(e.WhisperMessage.UserId, e.WhisperMessage.Username),
                    Conversation = new ConversationAccount { Id = e.WhisperMessage.UserId },
                    ChannelData = new WhisperMessage(e.WhisperMessage.Badges,e.WhisperMessage.ColorHex,e.WhisperMessage.Color, e.WhisperMessage.Username,e.WhisperMessage.DisplayName,e.WhisperMessage.EmoteSet,e.WhisperMessage.ThreadId,e.WhisperMessage.MessageId,e.WhisperMessage.UserId,e.WhisperMessage.IsTurbo,e.WhisperMessage.BotUsername,e.WhisperMessage.Message,e.WhisperMessage.UserType)

                }));
            }
        }
        public async void OnMessageSent(object sender, OnMessageSentArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity
            {
                Text = e.SentMessage.Message,
                Type = "message",
                From = new ChannelAccount(null, e.SentMessage.DisplayName,e.SentMessage.UserType.ToString()),
                ChannelData = new SentMessage(e.SentMessage.Badges,e.SentMessage.Channel,e.SentMessage.ColorHex, e.SentMessage.DisplayName, e.SentMessage.EmoteSet,e.SentMessage.IsModerator,e.SentMessage.IsSubscriber, e.SentMessage.UserType, e.SentMessage.Message)
            })) ;
        }
        public async void OnMessageReviewved(object sender, OnMessageReceivedArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity
            {
                Text= e.ChatMessage.Message,
              Type="message",
              From = new ChannelAccount(),
              ChannelData = new ChatMessage(e.ChatMessage.BotUsername, e.ChatMessage.UserId, e.ChatMessage.Username, e.ChatMessage.DisplayName, e.ChatMessage.ColorHex, e.ChatMessage.Color, e.ChatMessage.EmoteSet, e.ChatMessage.Message, e.ChatMessage.UserType, e.ChatMessage.Channel, e.ChatMessage.Id, e.ChatMessage.IsSubscriber, e.ChatMessage.SubscribedMonthCount, e.ChatMessage.RoomId, e.ChatMessage.IsTurbo, e.ChatMessage.IsModerator,e.ChatMessage.IsMe,e.ChatMessage.IsBroadcaster,e.ChatMessage.Noisy,e.ChatMessage.RawIrcMessage,e.ChatMessage.EmoteReplacedMessage,e.ChatMessage.Badges,e.ChatMessage.CheerBadge,e.ChatMessage.Bits,e.ChatMessage.BitsInDollars)
            }));
        }
        public async void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity
            {
                Text = $"{e.BotUsername} bot joined Channel: {e.Channel}"
            }));
        }
        public async void OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity { Text = $"User: {e.Username} joined channel: {e.Channel}" }));
        }
        public async void OnNewFollower (object sender, NewFollowersEventArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity
            {
                Text = $"new follower number {e.FollowerCount}"
            }));
        }
        public async void OnNewSubscriber (object sender, OnNewSubscriberArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity { Text = $"New subscriber{e.Subscriber} on {e.Channel}" }));
        }
        public async void OnReSubscriber (object sender, OnReSubscriberArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity {Text = $"Channel {e.Channel} got ReSub {e.ReSubscriber}" }));
        }
        public async void OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity { Text = $"{e.Channel} get Gifted Sub {e.GiftedSubscription}" }));
        }
        public async void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity { Text = $"{e.Channel} got raided {e.RaidNotificaiton}" }));
        }
        public async void OnChatCommandReveived(object sender, OnChatCommandReceivedArgs e)
        {
            await bot.OnTurnAsync(new TurnContext(twitchAdapter, new Activity { Text = e.Command.CommandText }));
        }
    }
}
