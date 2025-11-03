using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using ServicesKeeperUnit.Twitch.Adapter.ChatBot;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace ServicesKeeperUnit.Twitch.Adapter
{
    public static class ActivityExtensions
    {
        
        #region Connection
        public static OnChannelStateChangedArgs AsChannelStateChanged(this Activity activity)
        {
            var ChannelStateChangedEvent = new OnChannelStateChangedArgs();
            return ChannelStateChangedEvent;
        }
        public static OnConnectedArgs AsConnected(this Activity activity)
        {
            var ConnectedEvent = new OnConnectedArgs();
            return ConnectedEvent;
        }
        public static OnConnectionErrorArgs AsConnectionError(this Activity activity)
        {
            var ConnectionErrorEvent = new OnConnectionErrorArgs();
            return ConnectionErrorEvent;
        }
        public static OnDisconnectedArgs AsDisconnected(this Activity activity)
        {
            var DisconnectedEvent = new OnDisconnectedArgs();
            return DisconnectedEvent;
        }
        public static OnExistingUsersDetectedArgs AsExistingUsersDetected(this Activity activity)
        {
            var ExistingUsersDetectedEvent = new OnExistingUsersDetectedArgs();
            return ExistingUsersDetectedEvent;
        }
        public static OnFailureToReceiveJoinConfirmationArgs AsFailureToReceiveJoinConfirmation(this Activity activity)
        {
            var FailureToReceiveJoinConfirmationEvent = new OnFailureToReceiveJoinConfirmationArgs();
            return FailureToReceiveJoinConfirmationEvent;
        }
        public static OnIncorrectLoginArgs AsIncorrectLogin(this Activity activity)
        {
            var IncorrectLoginEvent = new OnIncorrectLoginArgs();
            return IncorrectLoginEvent;
        }
        public static OnJoinedChannelArgs AsJoinedChannel(this Activity activity)
        {
            var JoinedChannelEvent = new OnJoinedChannelArgs();
            return JoinedChannelEvent;
        }
        public static OnLeftChannelArgs AsLeftChannel(this Activity activity)
        {
            var LeftChannel = new OnLeftChannelArgs();
            return LeftChannel;
        }
        public static OnLogArgs OnLog(this Activity activity)
        {
            var LogEvent = new OnLogArgs();
            return LogEvent;
        }
        public static OnUnaccountedForArgs AsUnaccountedFor(this Activity activity)
        {
            var UnaccountedForEvent = new OnUnaccountedForArgs();
            return UnaccountedForEvent;
        }
        public static OnUserJoinedArgs AsUserJoined(this Activity activity)
        {
            var UserJoinedEvent = new OnUserJoinedArgs();
            return UserJoinedEvent;
        }
        public static OnUserLeftArgs AsUserLeft(this Activity activity)
        {
            var UserLeftEvent = new OnUserLeftArgs();
            return UserLeftEvent;
        }
        public static OnUserStateChangedArgs AsUserStateChanged(this Activity activity)
        {
            var UserStateChangedEvent = new OnUserStateChangedArgs();
            return UserStateChangedEvent;
        }
        #endregion
        #region Messages
        //TODO Add Whispermessage Object 
        public static OnWhisperReceivedArgs AsWhisperRecieved(this Activity activity)
        {


            var newWishperEvent = new OnWhisperReceivedArgs ();
            return newWishperEvent;
        }
        public static OnMessageReceivedArgs AsMessageReceived(this Activity activity)
        {
            var MessageReceivedEvent = new OnMessageReceivedArgs();
            return MessageReceivedEvent;
        }
        public static OnMessageSentArgs AsMessageSent(this Activity activity, TwitchLib.Client.Models.UserState state )
        {

            var MessageSentEvent = new OnMessageSentArgs
            {
                 
            SentMessage = new SentMessage(state, activity.Text)
            };
            return MessageSentEvent;
        }
        public static OnRitualNewChatterArgs AsRitualNewChatter(this Activity activity)
        {
            var RitualNewChatterEvent = new OnRitualNewChatterArgs();
            return RitualNewChatterEvent;
        }
        public static OnSendReceiveDataArgs AsSendReceiveData(this Activity activity)
        {
            var SendReceiveDataEvent = new OnSendReceiveDataArgs();
            return SendReceiveDataEvent;
        }
        public static OnWhisperSentArgs AsWhisperSent(this Activity activity)
        {
            var WhisperSendEvent = new OnWhisperSentArgs {
                Username = activity.From.Name,
                Receiver=activity.Recipient.Name,
                Message = activity.Text
            };
            return WhisperSendEvent;
        }
        #endregion
        #region Subscriptions
        public static OnCommunitySubscriptionArgs OnCommunitySubscription(this Activity activity)
        {
            var CommunitySubscriptionEvent = new OnCommunitySubscriptionArgs();
            return CommunitySubscriptionEvent;
        }
        public static OnGiftedSubscriptionArgs OnGiftedSubscription(this Activity activity)
        {
            var GiftedSubscriptionEvent = new OnGiftedSubscriptionArgs();
            return GiftedSubscriptionEvent;
        }
        public static OnNewSubscriberArgs OnNewSubscriber(this Activity activity)
        {
            var NewSubscriberEvent = new OnNewSubscriberArgs();
            return NewSubscriberEvent;
        }
        public static OnReSubscriberArgs OnReSubscriber(this Activity activity)
        {
            var ReSubscriberEvent = new OnReSubscriberArgs();
            return ReSubscriberEvent;
        }
        #endregion
        #region Hosting
        public static OnBeingHostedArgs AsBeinHosted(this Activity activity)
        {
            var BeinHostedevent = new OnBeingHostedArgs();
            return BeinHostedevent;
        }
        public static OnHostingStartedArgs OnHostingStarted(this Activity activity)
        {
            var HostingStartedEvent = new OnHostingStartedArgs();
            return HostingStartedEvent;
        }
        public static OnHostingStoppedArgs OnHostingStopped(this Activity activity)
        {
            var HostingStoppedEvent = new OnHostingStoppedArgs();
            return HostingStoppedEvent;
        }
        public static OnNowHostingArgs OnNowHosting(this Activity activity)
        {
            var NowHostingEvent = new OnNowHostingArgs();
            return NowHostingEvent;
        }
        public static OnRaidNotificationArgs OnRaidNotification(this Activity activity)
        {
            var RaidNotificationEvent = new OnRaidNotificationArgs();
            return RaidNotificationEvent;
        }
        #endregion
        #region Commands
        public static OnChatClearedArgs OnChatCleared(this Activity activity)
        {
            var ChatClearedEvent = new OnChatClearedArgs();
            return ChatClearedEvent;
        }
        public static OnChatColorChangedArgs OnChatColorChanged(this Activity activity)
        {
            var ChatColorChangedEvent = new OnChatColorChangedArgs();
            return ChatColorChangedEvent;
        }
        public static OnChatCommandReceivedArgs OnChatCommandReceived(this Activity activity)
        {
            var ChatCommandReceivedEvent = new OnChatCommandReceivedArgs();
            return ChatCommandReceivedEvent;
        }
        public static OnWhisperCommandReceivedArgs OnWhisperCommandReceived(this Activity activity)
        {
            var WhisperCommandReceivedEvent = new OnWhisperCommandReceivedArgs();
            return WhisperCommandReceivedEvent;
        }
        
        #endregion
        #region Moderation
        public static OnModeratorJoinedArgs OnModeratorJoined(this Activity activity)
        {
            var ModeratorJoinedEvent = new OnModeratorJoinedArgs();
            return ModeratorJoinedEvent;
        }
        public static OnModeratorLeftArgs OnModeratorLeft(this Activity activity)
        {
            var ModeratorLeftEvent = new OnModeratorLeftArgs();
            return ModeratorLeftEvent;
        }
        public static OnModeratorsReceivedArgs OnModeratorsReceived(this Activity activity)
        {
            var ModeratorsReceivedEvent = new OnModeratorsReceivedArgs();
            return ModeratorsReceivedEvent;
        }
        public static OnUserBannedArgs OnUserBanned(this Activity activity)
        {
            var UserBannedEvent = new OnUserBannedArgs();
            return UserBannedEvent;
        }
        public static OnUserTimedoutArgs OnUserTimedout(this Activity activity)
        {
            var UserTimedoutEvent = new OnUserTimedoutArgs();
            return UserTimedoutEvent;
        }
        #endregion

    }
}
