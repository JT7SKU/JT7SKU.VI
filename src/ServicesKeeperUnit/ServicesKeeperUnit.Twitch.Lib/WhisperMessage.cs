using System;
using System.Runtime.Serialization;

namespace ServicesKeeperUnit.Twitch.Lib
{
    [DataContract]
    public class WhisperMessage
    {
        [DataMember(Name ="userid")]
        public int UserID { get; set; }
        [DataMember(Name ="message")]
        public string Message { get; set; }
        [DataMember(Name ="timestamp")]
        public DateTime SendTime { get; set; }
    }
}
