using BagoScoutApp.Pages.AuthUser.Employer;
using BagoScoutApp.Pages.AuthUser.Seeker;
using BagoScoutApp.Pages.Components;
using BagoScoutApp.Pages.Register;

namespace BagoScoutApp.Pages
{
    public partial class LoginPage : BasePage
    {
        bool _showPassword = false;
        public LoginPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 1. Quick check if token already exists (skip auto-login request)
            var token = await SecureStorage.GetAsync("AuthToken");
            if (!string.IsNullOrEmpty(token))
            {
                var storedUserType = Preferences.Get("UserType", "");
                if (storedUserType == "seeker")
                {
                    await Shell.Current.GoToAsync($"//{nameof(SDashboardPage)}", false);
                    return;
                }
                else if (storedUserType == "employer")
                {
                    await Shell.Current.GoToAsync($"//{nameof(EDashboardPage)}", false);
                    return;
                }
            }

            // 2. Perform auto-login if auth.txt is present
            try
            {
                var creds = await Services.AuthStorageService.LoadCredentialsAsync();
                if (creds != null && !string.IsNullOrEmpty(creds.Email) && !string.IsNullOrEmpty(creds.Password))
                {
                    Overlay.IsVisible = true;
                    var api = new Services.ApiClient();
                    var loginResp = await api.LoginAsync(creds.Email, creds.Password, true);
                    if (loginResp != null && loginResp.success)
                    {
                        // Save updated credentials/token
                        await Services.AuthStorageService.SaveCredentialsAsync(creds.Email, creds.Password, loginResp.token, loginResp.userId, loginResp.userType);

                        if (loginResp.userType == "seeker")
                        {
                            await Shell.Current.GoToAsync($"//{nameof(SDashboardPage)}", false);
                            return;
                        }
                        else if (loginResp.userType == "employer")
                        {
                            await Shell.Current.GoToAsync($"//{nameof(EDashboardPage)}", false);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-login error: {ex.Message}");
            }
            finally
            {
                Overlay.IsVisible = false;
            }
        }

        async void OnLoginClicked(object sender, EventArgs e)
        {
            Overlay.IsVisible = true;
            try
            {
                System.Diagnostics.Debug.WriteLine("=== LOGIN ATTEMPT STARTED ===");
                
                var api = new Services.ApiClient();
                var email = LoginEmail.Text?.Trim() ?? "";
                var pwd = LoginPassword.Text ?? "";
                
                System.Diagnostics.Debug.WriteLine($"Email: {email}");
                System.Diagnostics.Debug.WriteLine($"Password length: {pwd.Length}");
                System.Diagnostics.Debug.WriteLine($"Remember me: {RememberCheckBox.IsChecked}");
                System.Diagnostics.Debug.WriteLine($"API Base URL: {api.BaseUrl}");
                
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Empty email or password");
                    await ShowAlertAsync("Validation Error", "Please enter both username and password.", "OK");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("Sending login request...");
                var loginResp = await api.LoginAsync(email, pwd, RememberCheckBox.IsChecked);
                
                System.Diagnostics.Debug.WriteLine($"Response received: {loginResp != null}");
                
                if (loginResp != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Success: {loginResp.success}");
                    System.Diagnostics.Debug.WriteLine($"Message: {loginResp.message}");
                    System.Diagnostics.Debug.WriteLine($"UserType: {loginResp.userType}");
                    System.Diagnostics.Debug.WriteLine($"UserId: {loginResp.userId}");
                    System.Diagnostics.Debug.WriteLine($"Name: {loginResp.name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: loginResp is null");
                }
                
                if (loginResp != null && loginResp.success)
                {
                    System.Diagnostics.Debug.WriteLine($"Navigating to {loginResp.userType} dashboard...");

                    // Save encrypted credentials to auth.txt
                    await Services.AuthStorageService.SaveCredentialsAsync(email, pwd, loginResp.token, loginResp.userId, loginResp.userType);
                    
                    // Navigate based on user type (use relative routing, no animation)
                    if (loginResp.userType == "seeker")
                    {
                        await Shell.Current.GoToAsync($"//{nameof(SDashboardPage)}", false);
                    }
                    else if (loginResp.userType == "employer")
                    {
                        await Shell.Current.GoToAsync($"//{nameof(EDashboardPage)}", false);
                    }
                    else 
                    {
                        await ShowAlertAsync("Login failed", "Invalid User Type.", "Close");
                    }

                    System.Diagnostics.Debug.WriteLine("Navigation completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Login failed - showing error alert");
                    await ShowAlertAsync("Login failed", "Invalid email or password.", "Close");
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await ShowAlertAsync("Connection Error", $"Cannot connect to server. Please check your network connection.\n\nDetails: {ex.Message}", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UNEXPECTED ERROR: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await ShowAlertAsync("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("=== LOGIN ATTEMPT ENDED ===");
                Overlay.IsVisible = false;
            }
        }

        bool isEyeVisible = true;
        void OnEyeIconTapped(object sender, EventArgs e)
        {
            if (isEyeVisible)
            {
                eyeIcon.Text = "\uf070"; // fa-eye-slash
            }
            else
            {
                eyeIcon.Text = "\uf06e"; // fa-eye
            }

            isEyeVisible = !isEyeVisible;
            LoginPassword.IsPassword = !LoginPassword.IsPassword;
        }

        async void OnCreateAccountTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegisterTypePage), false);
        }
        async void OnForgotPasswordTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ForgotPasswordPage), false);
        }
    }
}
