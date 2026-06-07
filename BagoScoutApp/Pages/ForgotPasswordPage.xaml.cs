using BagoScoutApp.Services;
using BagoScoutApp.Pages.Components;
using System.Text.RegularExpressions;

namespace BagoScoutApp.Pages
{
    public partial class ForgotPasswordPage : BasePage
    {
        private int _currentStep = 1;
        private string _email = "";
        private string _otpCode = "";
        private readonly ApiClient _api = new();

        private bool _showPassword = false;
        private bool _showConfirmPassword = false;

        public ForgotPasswordPage()
        {
            InitializeComponent();
            PasswordEntry.TextChanged += OnPasswordTextChanged;
            ConfirmPasswordEntry.TextChanged += OnConfirmPasswordTextChanged;
        }

        private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
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

            CheckPasswordMatch();
        }

        private void OnConfirmPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            CheckPasswordMatch();
        }

        private void CheckPasswordMatch()
        {
            var pwd = PasswordEntry.Text ?? "";
            var confirm = ConfirmPasswordEntry.Text ?? "";

            if (string.IsNullOrEmpty(confirm))
            {
                MatchLabel.IsVisible = false;
            }
            else if (pwd == confirm)
            {
                MatchLabel.Text = "✓ Passwords match";
                MatchLabel.TextColor = Color.FromArgb("#10B981"); // green
                MatchLabel.IsVisible = true;
            }
            else
            {
                MatchLabel.Text = "✗ Passwords do not match";
                MatchLabel.TextColor = Color.FromArgb("#EF4444"); // red
                MatchLabel.IsVisible = true;
            }
        }

        private void UpdateStepViews()
        {
            // Toggle step visibility
            Step1View.IsVisible = _currentStep == 1;
            Step2View.IsVisible = _currentStep == 2;
            Step3View.IsVisible = _currentStep == 3;

            // Toggle back button visibility
            BackBtn.IsVisible = _currentStep > 1;

            // Update Next/Submit button text
            NextBtn.Text = _currentStep == 3 ? "Reset Password" : "Next";

            // Update Progress Steps UI
            UpdateProgressStepsUI();
        }

        private void UpdateProgressStepsUI()
        {
            // Step 1 Styling
            if (_currentStep == 1)
            {
                Step1Indicator.BackgroundColor = Color.FromArgb("#6C63FF");
                Step1IndicatorLabel.Text = "1";
                Step1IndicatorLabel.TextColor = Colors.White;
                Step1Label.TextColor = Color.FromArgb("#1C2B53");
                Step1Label.FontAttributes = FontAttributes.Bold;

                Step1Line.Color = Color.FromArgb("#E5E7EB");

                Step2Indicator.BackgroundColor = Color.FromArgb("#E5E7EB");
                Step2IndicatorLabel.Text = "2";
                Step2IndicatorLabel.TextColor = Color.FromArgb("#8D94A8");
                Step2Label.TextColor = Color.FromArgb("#8D94A8");
                Step2Label.FontAttributes = FontAttributes.None;

                Step2Line.Color = Color.FromArgb("#E5E7EB");

                Step3Indicator.BackgroundColor = Color.FromArgb("#E5E7EB");
                Step3IndicatorLabel.Text = "3";
                Step3IndicatorLabel.TextColor = Color.FromArgb("#8D94A8");
                Step3Label.TextColor = Color.FromArgb("#8D94A8");
                Step3Label.FontAttributes = FontAttributes.None;
            }
            // Step 2 Styling
            else if (_currentStep == 2)
            {
                Step1Indicator.BackgroundColor = Color.FromArgb("#6C63FF");
                Step1IndicatorLabel.Text = "✓";
                Step1IndicatorLabel.TextColor = Colors.White;
                Step1Label.TextColor = Color.FromArgb("#8D94A8");
                Step1Label.FontAttributes = FontAttributes.None;

                Step1Line.Color = Color.FromArgb("#6C63FF");

                Step2Indicator.BackgroundColor = Color.FromArgb("#6C63FF");
                Step2IndicatorLabel.Text = "2";
                Step2IndicatorLabel.TextColor = Colors.White;
                Step2Label.TextColor = Color.FromArgb("#1C2B53");
                Step2Label.FontAttributes = FontAttributes.Bold;

                Step2Line.Color = Color.FromArgb("#E5E7EB");

                Step3Indicator.BackgroundColor = Color.FromArgb("#E5E7EB");
                Step3IndicatorLabel.Text = "3";
                Step3IndicatorLabel.TextColor = Color.FromArgb("#8D94A8");
                Step3Label.TextColor = Color.FromArgb("#8D94A8");
                Step3Label.FontAttributes = FontAttributes.None;
            }
            // Step 3 Styling
            else if (_currentStep == 3)
            {
                Step1Indicator.BackgroundColor = Color.FromArgb("#6C63FF");
                Step1IndicatorLabel.Text = "✓";
                Step1IndicatorLabel.TextColor = Colors.White;
                Step1Label.TextColor = Color.FromArgb("#8D94A8");
                Step1Label.FontAttributes = FontAttributes.None;

                Step1Line.Color = Color.FromArgb("#6C63FF");

                Step2Indicator.BackgroundColor = Color.FromArgb("#6C63FF");
                Step2IndicatorLabel.Text = "✓";
                Step2IndicatorLabel.TextColor = Colors.White;
                Step2Label.TextColor = Color.FromArgb("#8D94A8");
                Step2Label.FontAttributes = FontAttributes.None;

                Step2Line.Color = Color.FromArgb("#6C63FF");

                Step3Indicator.BackgroundColor = Color.FromArgb("#6C63FF");
                Step3IndicatorLabel.Text = "3";
                Step3IndicatorLabel.TextColor = Colors.White;
                Step3Label.TextColor = Color.FromArgb("#1C2B53");
                Step3Label.FontAttributes = FontAttributes.Bold;
            }
        }

        private async void OnNextTapped(object sender, EventArgs e)
        {
            if (_currentStep == 1)
            {
                var email = EmailEntry.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(email))
                {
                    await ShowAlertAsync("Validation Error", "Please enter your email address.", "OK");
                    return;
                }

                if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                {
                    await ShowAlertAsync("Validation Error", "Please enter a valid email address.", "OK");
                    return;
                }

                _email = email;
                await SendForgotPasswordOtp();
            }
            else if (_currentStep == 2)
            {
                var code = CodeEntry.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(code) || code.Length != 6)
                {
                    await ShowAlertAsync("Validation Error", "Please enter the 6-digit OTP code.", "OK");
                    return;
                }

                _otpCode = code;
                await VerifyForgotPasswordOtp();
            }
            else if (_currentStep == 3)
            {
                var newPwd = PasswordEntry.Text ?? "";
                var confirmPwd = ConfirmPasswordEntry.Text ?? "";

                if (string.IsNullOrEmpty(newPwd) || string.IsNullOrEmpty(confirmPwd))
                {
                    await ShowAlertAsync("Validation Error", "Please fill in both password fields.", "OK");
                    return;
                }

                // Password strength validation checks
                if (newPwd.Length < 8 || 
                    !Regex.IsMatch(newPwd, @"[A-Z]") ||
                    !Regex.IsMatch(newPwd, @"[0-9]") ||
                    !Regex.IsMatch(newPwd, @"[!@#$%^&*(),.?""':{}|<>_\-+=]"))
                {
                    await ShowAlertAsync("Invalid Password", "Password must meet all complexity requirements.", "OK");
                    return;
                }

                if (newPwd != confirmPwd)
                {
                    await ShowAlertAsync("Validation Error", "Passwords do not match.", "OK");
                    return;
                }

                await ResetPassword(newPwd);
            }
        }

        private void OnBackTapped(object sender, EventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateStepViews();
            }
        }

        private async void OnResendTapped(object sender, EventArgs e)
        {
            await SendForgotPasswordOtp();
        }

        private async void OnCloseTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }

        private async void OnLoginTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }

        private void OnTogglePasswordTapped(object sender, EventArgs e)
        {
            _showPassword = !_showPassword;
            PasswordEntry.IsPassword = !_showPassword;
            TogglePwdIcon.Text = _showPassword ? "\uf070" : "\uf06e"; // fa-eye-slash vs fa-eye
        }

        private void OnToggleConfirmPasswordTapped(object sender, EventArgs e)
        {
            _showConfirmPassword = !_showConfirmPassword;
            ConfirmPasswordEntry.IsPassword = !_showConfirmPassword;
            ToggleConfirmPwdIcon.Text = _showConfirmPassword ? "\uf070" : "\uf06e";
        }

        private async Task SendForgotPasswordOtp()
        {
            LoadingText.Text = "Sending OTP...";
            LoadingOverlay.IsVisible = true;

            try
            {
                var response = await _api.SendForgotPasswordOtpAsync(_email);
                LoadingOverlay.IsVisible = false;

                if (response.IsSuccessStatusCode)
                {
                    // In development/test mode, the server might return the generated code in JSON
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content.Contains("\"code\":"))
                    {
                        // Extract code from json payload (e.g. {"success":true,"code":"123456"})
                        var match = Regex.Match(content, @"""code"":\s*""(\d{6})""");
                        if (match.Success)
                        {
                            var localCode = match.Groups[1].Value;
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] OTP Code: {localCode}");
                        }
                    }

                    await ShowAlertAsync("Success", "If the email is registered, we have sent a 6-digit OTP code to it.", "OK");
                    
                    _currentStep = 2;
                    UpdateStepViews();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to send OTP code. Please try again later.", "OK");
                }
            }
            catch (Exception ex)
            {
                LoadingOverlay.IsVisible = false;
                await ShowAlertAsync("Error", $"Connection failed: {ex.Message}", "OK");
            }
        }

        private async Task VerifyForgotPasswordOtp()
        {
            LoadingText.Text = "Verifying code...";
            LoadingOverlay.IsVisible = true;

            try
            {
                var response = await _api.VerifyForgotPasswordOtpAsync(_email, _otpCode);
                LoadingOverlay.IsVisible = false;

                if (response.IsSuccessStatusCode)
                {
                    _currentStep = 3;
                    UpdateStepViews();
                }
                else
                {
                    await ShowAlertAsync("Error", "The OTP code is invalid or has expired.", "OK");
                }
            }
            catch (Exception ex)
            {
                LoadingOverlay.IsVisible = false;
                await ShowAlertAsync("Error", $"Connection failed: {ex.Message}", "OK");
            }
        }

        private async Task ResetPassword(string newPassword)
        {
            LoadingText.Text = "Resetting password...";
            LoadingOverlay.IsVisible = true;

            try
            {
                var response = await _api.ResetPasswordAsync(_email, _otpCode, newPassword);
                LoadingOverlay.IsVisible = false;

                if (response.IsSuccessStatusCode)
                {
                    await ShowAlertAsync("Success", "Your password has been reset successfully. You can now log in.", "OK");
                    await Shell.Current.GoToAsync("..", false);
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    await ShowAlertAsync("Error", "Failed to reset password. Please request a new OTP and try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                LoadingOverlay.IsVisible = false;
                await ShowAlertAsync("Error", $"Connection failed: {ex.Message}", "OK");
            }
        }
    }
}
