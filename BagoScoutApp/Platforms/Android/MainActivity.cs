using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace BagoScoutApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Notification permission check (Android 13+)
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != global::Android.Content.PM.Permission.Granted)
                {
                    RequestPermissions(new string[] { global::Android.Manifest.Permission.PostNotifications }, 0);
                }
            }

            // Fetch current FCM token on startup and register it with the backend database
            try
            {
                Firebase.Messaging.FirebaseMessaging.Instance.GetToken().AddOnCompleteListener(new OnCompleteListener(fcmToken =>
                {
                    if (!string.IsNullOrEmpty(fcmToken))
                    {
                        Microsoft.Maui.Storage.Preferences.Set("FcmToken", fcmToken);
                        System.Diagnostics.Debug.WriteLine($"[FCM] Token fetched successfully on launch: {fcmToken}");

                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            try
                            {
                                var authToken = await Microsoft.Maui.Storage.SecureStorage.GetAsync("AuthToken");
                                if (!string.IsNullOrEmpty(authToken))
                                {
                                    var api = new BagoScoutApp.Services.ApiClient();
                                    await api.RegisterFcmAsync(authToken, fcmToken);
                                    System.Diagnostics.Debug.WriteLine("[FCM] Registered token with backend on launch.");
                                }
                            }
                            catch (System.Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[FCM] Error registering token: {ex.Message}");
                            }
                        });
                    }
                }));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FCM] Error starting token fetch: {ex.Message}");
            }

            if (Intent != null)
            {
                ProcessNotificationIntent(Intent);
            }
        }

        private class OnCompleteListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
        {
            private readonly Action<string> _callback;
            public OnCompleteListener(Action<string> callback)
            {
                _callback = callback;
            }
            public void OnComplete(Android.Gms.Tasks.Task task)
            {
                if (task.IsSuccessful)
                {
                    var result = task.Result;
                    _callback?.Invoke(result.ToString());
                }
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent != null)
            {
                ProcessNotificationIntent(intent);
            }
        }

        private void ProcessNotificationIntent(Intent intent)
        {
            if (intent.HasExtra("navigationTarget"))
            {
                var target = intent.GetStringExtra("navigationTarget");
                var userId = intent.GetIntExtra("userId", 0);
                var userName = intent.GetStringExtra("userName") ?? "User";

                if (string.IsNullOrEmpty(target)) return;

                string route = target;
                if (userId > 0)
                {
                    route = $"{target}?userId={userId}&userName={System.Uri.EscapeDataString(userName)}";
                }

                if (Microsoft.Maui.Controls.Shell.Current != null)
                {
                    Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            await Microsoft.Maui.Controls.Shell.Current.GoToAsync(route, false);
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to navigate to route: {ex.Message}");
                        }
                    });
                }
                else
                {
                    BagoScoutApp.App.PendingNavigationRoute = route;
                }
            }
        }
    }
}
