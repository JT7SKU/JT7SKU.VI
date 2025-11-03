using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServicesKeeperUnit.Twitch.Lib
{
    [DataContract]
    public class ChatMessage
    {
        [DataMember(Name ="userid")]
        public int UserId { get; set; }
        [DataMember(Name ="message")]
        public string Message { get; set; }
    }
}
