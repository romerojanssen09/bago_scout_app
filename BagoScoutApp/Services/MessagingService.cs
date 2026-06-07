using System;

namespace BagoScoutApp.Services
{
    public static class MessagingService
    {
        // Tracks who the user is currently chatting with on screen
        public static int ActiveChatUserId { get; set; } = 0;

        // Fired when a new chat message arrives from FCM
        public static event Action<int>? ChatMessageReceived;

        public static void NotifyChatMessageReceived(int senderId)
        {
            ChatMessageReceived?.Invoke(senderId);
        }

        // Fired when conversations list needs refreshing
        public static event Action? ConversationsUpdated;

        public static void NotifyConversationsUpdated()
        {
            ConversationsUpdated?.Invoke();
        }
    }
}
