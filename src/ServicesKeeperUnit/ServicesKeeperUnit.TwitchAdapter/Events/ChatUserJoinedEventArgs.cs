using System;

namespace ServicesKeeperUnit.Twitch.Adapter.Events
{
    public class ChatUserJoinedEventArgs : EventArgs
    {
        public string UserName { get; set; }
    }
}