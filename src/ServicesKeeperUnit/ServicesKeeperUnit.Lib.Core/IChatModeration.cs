using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServicesKeeperUnit.Lib.Core
{
    public interface IChatModeration
    {
        bool IsModerator { get; }
        Task<bool> TimeoutUserAsync(string userName, TimeSpan time);
        Task<bool> BanUserAsync(string userName);
        Task<bool> UnbanUserAsync(string userName);
    }
}
