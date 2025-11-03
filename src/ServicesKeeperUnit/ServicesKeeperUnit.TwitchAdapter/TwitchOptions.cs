using System;
using System.Collections.Generic;
using System.Text;

namespace ServicesKeeperUnit.Twitch.Adapter
{
    public class TwitchOptions
    {
        public string BotUsername { get; set; }
        public string ChannelName { get; set; }
        public string OAuthToken { get; set; }
        public string ApitUrl { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
    }
}
