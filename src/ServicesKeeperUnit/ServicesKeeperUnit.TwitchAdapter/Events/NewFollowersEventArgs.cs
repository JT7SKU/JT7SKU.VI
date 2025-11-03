using System;

namespace ServicesKeeperUnit.Twitch.Adapter.Events
{
    public class NewFollowersEventArgs :EventArgs
    {
        private readonly int foundFollowerCount;
        public NewFollowersEventArgs(int foundFollowerCount)
        {

            this.foundFollowerCount = foundFollowerCount;
        }

        public int FollowerCount => foundFollowerCount;
    }
}