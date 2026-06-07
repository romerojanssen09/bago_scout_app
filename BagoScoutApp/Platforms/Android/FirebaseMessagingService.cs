using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;

namespace BagoScoutApp.Platforms.Android
{
    [global::Android.App.Service(Exported = false)]
    [global::Android.App.IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : global::Firebase.Messaging.FirebaseMessagingService
    {
        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);
            System.Diagnostics.Debug.WriteLine($"FCM Token: {token}");
            
            // Store token for later registration with backend
            Microsoft.Maui.Storage.Preferences.Set("FcmToken", token);

            // Register with the backend immediately if already authenticated
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var authToken = await Microsoft.Maui.Storage.SecureStorage.GetAsync("AuthToken");
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        var api = new BagoScoutApp.Services.ApiClient();
                        await api.RegisterFcmAsync(authToken, token);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to register new FCM token: {ex.Message}");
                }
            });
        }

        public override void OnMessageReceived(global::Firebase.Messaging.RemoteMessage message)
        {
            base.OnMessageReceived(message);

            var notification = message.GetNotification();
            var title = notification?.Title ?? (message.Data.ContainsKey("title") ? message.Data["title"] : "Bago Scout");
            var body = notification?.Body ?? (message.Data.ContainsKey("body") ? message.Data["body"] : "New notification");

            int notificationId = 0;
            bool isChat = false;
            int chatSenderId = 0;

            if (message.Data.TryGetValue("type", out string typeVal) && typeVal == "chat")
            {
                isChat = true;
            }

            if (message.Data.TryGetValue("senderId", out string senderIdStr) && int.TryParse(senderIdStr, out int senderId))
            {
                notificationId = senderId;
                chatSenderId = senderId;
            }
            else if (message.Data.TryGetValue("applicationId", out string appIdStr) && int.TryParse(appIdStr, out int appId))
            {
                notificationId = appId;
            }
            else if (message.Data.TryGetValue("jobId", out string jobIdStr) && int.TryParse(jobIdStr, out int jobId))
            {
                notificationId = jobId;
            }

            if (isChat && chatSenderId > 0)
            {
                // Trigger real-time UI refresh in the chat page or conversation list
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    BagoScoutApp.Services.MessagingService.NotifyChatMessageReceived(chatSenderId);
                    BagoScoutApp.Services.MessagingService.NotifyConversationsUpdated();
                });

                // Suppress local notification if user has this chat open
                if (chatSenderId == BagoScoutApp.Services.MessagingService.ActiveChatUserId)
                {
                    return;
                }
            }

            SendLocalNotification(title, body, notificationId, message.Data);
        }

        private void SendLocalNotification(string title, string body, int notificationId, System.Collections.Generic.IDictionary<string, string> data)
        {
            var context = global::Android.App.Application.Context;
            var intent = new Intent(context, Java.Lang.Class.FromType(typeof(MainActivity)));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

            if (data != null && data.TryGetValue("type", out string type))
            {
                string userType = Microsoft.Maui.Storage.Preferences.Get("UserType", "");
                string? targetPage = null;
                int userIdVal = 0;
                string userNameVal = title;

                if (type == "chat")
                {
                    if (userType == "seeker")
                    {
                        targetPage = "SMessagesPage";
                    }
                    else if (userType == "employer")
                    {
                        targetPage = "EMessagesPage";
                    }

                    if (data.TryGetValue("senderId", out string senderIdStr) && int.TryParse(senderIdStr, out int sId))
                    {
                        userIdVal = sId;
                    }
                }
                else if (type == "application")
                {
                    if (userType == "employer")
                    {
                        targetPage = "ECandidatesPage";
                    }
                }
                else if (type == "application_status")
                {
                    if (userType == "seeker")
                    {
                        targetPage = "SApplicationsPage";
                    }
                }

                if (!string.IsNullOrEmpty(targetPage))
                {
                    intent.PutExtra("navigationTarget", targetPage);
                    if (userIdVal > 0)
                    {
                        intent.PutExtra("userId", userIdVal);
                        intent.PutExtra("userName", userNameVal);
                    }
                }
            }
            
            var pendingIntent = PendingIntent.GetActivity(context, notificationId, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var channelId = "bagoscout_notifications";
            var notificationBuilder = new NotificationCompat.Builder(context, channelId)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetSmallIcon(global::Android.Resource.Mipmap.SymDefAppIcon); // Use a standard icon if the custom one is not found

            var notificationManager = NotificationManagerCompat.From(context);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Bago Scout Notifications", NotificationImportance.Default);
                notificationManager.CreateNotificationChannel(channel);
            }

            notificationManager.Notify(notificationId, notificationBuilder.Build());
        }
    }
}
