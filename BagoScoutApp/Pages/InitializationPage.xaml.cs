using BagoScoutApp.Services;

namespace BagoScoutApp.Pages
{
    public partial class InitializationPage : ContentPage
    {
        public InitializationPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CheckLoginStatus();
        }

        private async Task CheckLoginStatus()
        {
            try
            {
                StatusLabel.Text = "Checking login status...";
                var api = new ApiClient();
                
                // 1. Validate auth token
                if (await api.ValidateTokenAsync())
                {
                    StatusLabel.Text = "Registering push notifications...";
                    
                    // Register FCM token if available
                    var fcmToken = Preferences.Get("FcmToken", "");
                    var token = await SecureStorage.GetAsync("AuthToken");
                    if (!string.IsNullOrEmpty(fcmToken) && !string.IsNullOrEmpty(token))
                    {
                        await api.RegisterFcmAsync(token, fcmToken);
                    }

                    StatusLabel.Text = "Loading dashboard...";
                    await Task.Delay(500); // Small delay for visual transitions

                    // Check if there was a pending deep-link navigation (from push notification click)
                    if (!string.IsNullOrEmpty(App.PendingNavigationRoute))
                    {
                        var route = App.PendingNavigationRoute;
                        App.PendingNavigationRoute = null;
                        await Shell.Current.GoToAsync(route, false);
                    }
                    else
                    {
                        var userType = Preferences.Get("UserType", "");
                        if (userType == "seeker")
                        {
                            await Shell.Current.GoToAsync("//SDashboardPage", false);
                        }
                        else if (userType == "employer")
                        {
                            await Shell.Current.GoToAsync("//EDashboardPage", false);
                        }
                        else
                        {
                            await Shell.Current.GoToAsync("//MainPage", false);
                        }
                    }
                }
                else
                {
                    StatusLabel.Text = "Redirecting to MainPage...";
                    await Task.Delay(500);
                    
                    // Check if they need to see the MainPage (landing page) first or just go to Login
                    await Shell.Current.GoToAsync("//MainPage", false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialization error: {ex.Message}");
                StatusLabel.Text = "Redirecting...";
                await Task.Delay(500);
                await Shell.Current.GoToAsync("//LoginPage", false);
            }
        }
    }
}
