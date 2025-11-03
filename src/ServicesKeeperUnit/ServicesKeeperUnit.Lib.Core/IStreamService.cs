using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServicesKeeperUnit.Lib.Core
{
    public interface IStreamService
    {
        string Name { get; }
        int CurrentFollowerCount { get; }
        int CurrentViewerCount { get; }
        ValueTask<TimeSpan?> Uptime();
        /// <summary>
        /// Event raised when the number of Followers or Viewers changes
        /// </summary>
        event EventHandler<ServiceUpdatedEventArgs> Updated;

    }
}
