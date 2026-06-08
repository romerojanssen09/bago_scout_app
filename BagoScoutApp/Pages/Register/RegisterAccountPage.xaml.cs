using BagoScoutApp.Models;
using BagoScoutApp.Services;
using BagoScoutApp.Pages.Components;
using System.Text.RegularExpressions;

namespace BagoScoutApp.Pages.Register
{
    public partial class RegisterAccountPage : BasePage
    {
        bool _showPassword;
        bool _showConfirmPassword;
        readonly ApiClient _api = new();

        public RegisterAccountPage()
        {
            InitializeComponent();
            PasswordEntry.TextChanged += OnPasswordTextChanged;
            EmailEntry.Unfocused += OnEmailUnfocused;
        }
        
        async void OnEmailUnfocused(object sender, FocusEventArgs e)
        {
            var email = EmailEntry.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(email))
                return;
                
            try
            {
                var result = await _api.CheckEmailAsync(email);
                if (result?.exists == true)
                {
                    await ShowAlertAsync("Email Already Registered", "This email is already registered. Please use a different email or login.", "OK");
                    EmailEntry.Text = "";
                }
            }
            catch (Exception ex)
            {
                // Silently fail - don't block user if check fails
                System.Diagnostics.Debug.WriteLine($"Email check failed: {ex.Message}");
            }
        }
        
        void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            var password = e.NewTextValue ?? "";
            
            // Check length (at least 8 characters)
            bool hasLength = password.Length >= 8;
            LengthDot.TextColor = hasLength ? Color.FromArgb("#6C63FF") : Color.FromArgb("#E5E7EB");
            
            // Check uppercase letter
            bool hasUppercase = Regex.IsMatch(password, @"[A-Z]");
            UppercaseDot.TextColor = hasUppercase ? Color.FromArgb("#6C63FF") : Color.FromArgb("#E5E7EB");
            
            // Check number
            bool hasNumber = Regex.IsMatch(password, @"[0-9]");
            NumberDot.TextColor = hasNumber ? Color.FromArgb("#6C63FF") : Color.FromArgb("#E5E7EB");

            // Check special character
            bool hasSpecial = Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>_\-+=]");
            SpecialDot.TextColor = hasSpecial ? Color.FromArgb("#6C63FF") : Color.FromArgb("#E5E7EB");
        }
        
        async void OnCloseTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("../..", false);
        }
        
        async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }
        
        async void OnLoginTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(LoginPage), false);
        }
        
        void OnTogglePassword(object sender, EventArgs e)
        {
            _showPassword = !_showPassword;
            PasswordEntry.IsPassword = !_showPassword;
            TogglePwdIcon.Text = _showPassword ? "\uf070" : "\uf06e";
        }
        
        void OnToggleConfirmPassword(object sender, EventArgs e)
        {
            _showConfirmPassword = !_showConfirmPassword;
            ConfirmPasswordEntry.IsPassword = !_showConfirmPassword;
            ToggleConfirmPwdIcon.Text = _showConfirmPassword ? "\uf070" : "\uf06e";
        }
        
        async void OnSendCode(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
                string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                await ShowAlertAsync("Invalid", "Please fill all fields and confirm password.", "OK");
                return;
            }
            
            // Validate password requirements
            var password = PasswordEntry.Text!;
            if (password.Length < 8 || 
                !Regex.IsMatch(password, @"[A-Z]") ||
                !Regex.IsMatch(password, @"[0-9]") ||
                !Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>_\-+=]"))
            {
                await ShowAlertAsync("Invalid Password", "Password must meet all requirements.", "OK");
                return;
            }
            
            LoadingOverlay.IsVisible = true;
            
            var email = EmailEntry.Text!.Trim();
            
            // Check if email already exists
            try
            {
                var checkResult = await _api.CheckEmailAsync(email);
                if (checkResult?.exists == true)
                {
                    LoadingOverlay.IsVisible = false;
                    await ShowAlertAsync("Email Already Registered", "This email is already registered. Please use a different email or login.", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                LoadingOverlay.IsVisible = false;
                await ShowAlertAsync("Error", $"Unable to verify email: {ex.Message}", "OK");
                return;
            }
            
            RegistrationState.FirstName = FirstNameEntry.Text!.Trim();
            RegistrationState.LastName = LastNameEntry.Text!.Trim();
            RegistrationState.Email = email;
            RegistrationState.Password = PasswordEntry.Text!;

            var resp = await _api.SendVerificationCodeAsync(RegistrationState.Email, RegistrationState.FirstName);
            
            LoadingOverlay.IsVisible = false;
            
            if (resp.IsSuccessStatusCode)
            {
                await Shell.Current.GoToAsync(nameof(RegisterVerifyPage), false);
            }
            else
            {
                await ShowAlertAsync("Error", "Failed to send verification code.", "OK");
            }
        }
    }
}
