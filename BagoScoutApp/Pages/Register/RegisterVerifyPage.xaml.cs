using BagoScoutApp.Services;
using System.Net.Http.Json;
using BagoScoutApp.Models;
using BagoScoutApp.Pages.Components;

namespace BagoScoutApp.Pages.Register
{
    public partial class RegisterVerifyPage : BasePage
    {
        readonly ApiClient _api = new();
        public RegisterVerifyPage()
        {
            InitializeComponent();
        }
        
        async void OnCloseTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("../../..", false);
        }
        
        async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }
        
        async void OnLoginTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("../../..", false);
        }
        
        async void OnVerify(object sender, EventArgs e)
        {
            var code = CodeEntry.Text?.Trim() ?? "";
            if (code.Length != 6)
            {
                await ShowAlertAsync("Invalid", "Please enter the 6-digit code.", "OK");
                return;
            }
            
            try
            {
                var resp = await _api.VerifyEmailAsync(RegistrationState.Email, code);
                if (!resp.IsSuccessStatusCode)
                {
                    await ShowAlertAsync("Error", "Invalid or expired code.", "OK");
                    return;
                }
                
                // Email verified successfully, proceed to next step
                // Registration will happen after all steps are completed
                await Shell.Current.GoToAsync(nameof(RegisterIdPage), false);
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
        async void OnResend(object sender, EventArgs e)
        {
            await _api.SendVerificationCodeAsync(RegistrationState.Email, RegistrationState.FirstName);
            await ShowAlertAsync("Sent", "A new code has been sent.", "OK");
        }
    }
}
