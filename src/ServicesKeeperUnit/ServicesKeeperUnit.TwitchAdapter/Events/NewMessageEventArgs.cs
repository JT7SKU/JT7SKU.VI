using System;

namespace ServicesKeeperUnit.Twitch.Adapter.Events
{
    public class NewMessageEventArgs : EventArgs
    {
        public string UserName { get; set; }
        public string Message { get; set; }
        public object Badges { get; set; }
        public bool IsWhisper { get; set; } = false;
    }
}