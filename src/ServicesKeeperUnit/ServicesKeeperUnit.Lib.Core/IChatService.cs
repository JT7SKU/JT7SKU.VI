using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ServicesKeeperUnit.Lib.Core
{
    public interface IChatService
    {
        string Name { get; }
        bool IsAuthenticated { get; }

        event EventHandler<ChatMessageEventArgs> ChatMessage;
        event EventHandler<ChatUserInfoEventArgs> UserJoined;
        event EventHandler<ChatUserInfoEventArgs> UserLeft;

        Task<ChatMessage> SendMessageAsync(string message);
        Task<WhisperMessage> SendWhisperAsync(string userName, string message);
        
    }
}
