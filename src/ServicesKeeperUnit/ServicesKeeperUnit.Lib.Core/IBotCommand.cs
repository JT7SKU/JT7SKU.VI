using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client.Models;

namespace ServicesKeeperUnit.Lib.Core
{
    public interface IBotCommand
    {
        bool Enabled { get; }
        string CommandText { get; }
        void Execute(ChatCommand chatCommand, IChatService chatService);
    }
}
