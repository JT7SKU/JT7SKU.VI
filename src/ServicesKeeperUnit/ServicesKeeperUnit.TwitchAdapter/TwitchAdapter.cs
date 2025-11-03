using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using ServicesKeeperUnit.Twitch.Adapter.ChatBot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Client.Models;

namespace ServicesKeeperUnit.Twitch.Adapter
{
    public class TwitchAdapter : BotAdapter
    {
        private TwitchLib.Client.Models.UserState UserState;
        private readonly Chat chat;
        public TwitchAdapter(IOptions<TwitchOptions> options)
        {
            chat = new Chat(options.Value);
        }
        public Task ConnectChatActivity(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();
            foreach (var activity in activities)
            {
                
                switch (activity.Type)
                {
                    case "whispersent":
                        await chat.SendWhisperAsync(activity.AsWhisperSent());
                        break;
                    case "messagesent":
                        await chat.SentMessageAsync(activity.AsMessageSent(UserState));
                        break;
                    
                }
                                
                responses.Add(new ResourceResponse(activity.Id));
            }
            return responses.ToArray();
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public async Task<ResourceResponse[]> ReceiveActivityAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();
            foreach (var activity in activities)
            {
                switch (activity.Type)
                {
                    case "whisperreceived":
                        await chat.WhisperReceivedAsync(activity.AsWhisperRecieved());
                        break;
                    case "messagereceived":
                        await chat.MessageReceivedAsync(activity.AsMessageReceived());
                        break;
                    default:
                        break;
                }
                
                responses.Add(new ResourceResponse(activity.Id));
            }
            return responses.ToArray();
        }
        public async Task<ResourceResponse[]> CommandActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();
            foreach (var activity in activities)
            {
                switch (activity.Type)
                {
                    case "help":

                        break;
                    case "ping":

                        break;
                    case "mods":

                        break;
                    case "whisper":

                        break;
                    case "color":
                        break;
                    case "me":
                        break;
                    case "vips":
                        break;
                    case "disconnect":
                        break;
                    case "raid":
                        break;
                    case "host":
                        break;
                    case "clear":
                        break;
                    case "chatcommand":
                        break;
                    default:
                        break;
                }
                responses.Add(new ResourceResponse(activity.Id));
            }
            return responses.ToArray();
        }
    }
}
