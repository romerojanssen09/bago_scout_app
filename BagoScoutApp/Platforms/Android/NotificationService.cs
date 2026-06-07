using Android.App;
using Android.Content;
using BagoScoutApp.Services;
using Microsoft.Maui.Controls;

[assembly: Dependency(typeof(BagoScoutApp.Platforms.Android.NotificationService))]
namespace BagoScoutApp.Platforms.Android
{
    public class NotificationService : INotificationService
    {
        public void DismissNotification(int notificationId)
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
                notificationManager?.Cancel(notificationId);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error canceling notification: {ex.Message}");
            }
        }
    }
}
