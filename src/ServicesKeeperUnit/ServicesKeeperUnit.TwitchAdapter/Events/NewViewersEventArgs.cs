using System;

namespace ServicesKeeperUnit.Twitch.Adapter.Events
{
    public class NewViewersEventArgs : EventArgs
    {
        private int foundViewerCount;

        public NewViewersEventArgs(int foundViewerCount)
        {
            this.foundViewerCount = foundViewerCount;
        }

        public int ViewerCount => foundViewerCount;
    }
}