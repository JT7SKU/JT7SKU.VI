using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServicesKeeperUnit.Lib.Core
{
    [DataContract]
    public class TwitchChannelData
    {
        // Wrapper for twitch where Bot Framework Channels include Twitch and this implements the wrapper of twitch data what is channels and things 
        [DataMember(Name ="channel")]
        public long Channel { get; set; }
    }
}
