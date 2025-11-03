using Microsoft.Extensions.Logging;
using ServicesKeeperUnit.Lib.Core;
using ServicesKeeperUnit.Twitch.Adapter.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Models.Undocumented.Comments;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;

namespace ServicesKeeperUnit.Twitch.Adapter.ChatBot
{
    public class Chat 
    {
        public TwitchOptions Options { get; set; }



        public bool IsAuthenticated { get; } = false;

        private DateTime _NextReset;
        private int _RemainingThrottledCommands;

        public event EventHandler<ChatMessageEventArgs> ChatMessage;
        public event EventHandler<ChatUserInfoEventArgs> UserJoined;
        public event EventHandler<ChatUserInfoEventArgs> UserLeft;

        public Chat(TwitchOptions options)
        {
            Options = options;
        }
        public string Name { get { return Options.BotUsername; } }

        
        public async Task<SentMessage> SentMessageAsync(OnMessageSentArgs sentArgs)
        {
            throw new NotImplementedException();
        }
        public Task<ChatMessage> MessageReceivedAsync(OnMessageReceivedArgs message)
        {
            if (IsAuthenticated)
            {
                
            }
            throw new NotImplementedException();
        }
        public Task<WhisperMessage> WhisperReceivedAsync(OnWhisperReceivedArgs onWhisperReceived)
        {
            throw new NotImplementedException();
        }
        public Task<WhisperMessage> SendWhisperAsync(OnWhisperSentArgs whisperSentArgs)
        {
            throw new NotImplementedException();
        }
        #region Chat Moderation
        public bool IsModerator { get; } = false;
        public Task<bool> TimeoutUserAsync(string userName, TimeSpan time)
        {
            throw new NotImplementedException();
        }

        public Task<bool> BanUserAsync(string userName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnbanUserAsync(string userName)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
