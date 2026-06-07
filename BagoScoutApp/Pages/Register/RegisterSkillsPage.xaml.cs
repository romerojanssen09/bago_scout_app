using BagoScoutApp.Models;
using BagoScoutApp.Services;
using BagoScoutApp.Pages.Components;
using Microsoft.Maui.Controls.Shapes;
using System.Net.Http.Json;

namespace BagoScoutApp.Pages.Register
{
    public partial class RegisterSkillsPage : BasePage
    {
        readonly ApiClient _api = new();
        public RegisterSkillsPage()
        {
            InitializeComponent();
            RefreshSkills();
        }
        
        async void OnCloseTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("../../../../..", false);
        }
        
        async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }
        
        async void OnLoginTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("../../../../..", false);
        }
        
        void OnSkillTapped(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                var skill = btn.Text;
                if (!RegistrationState.Skills.Contains(skill, StringComparer.OrdinalIgnoreCase))
                {
                    RegistrationState.Skills.Add(skill);
                    RefreshSkills();
                }
            }
        }
        
        void RefreshSkills()
        {
            SkillsWrap.Children.Clear();
            NoSkillsLabel.IsVisible = RegistrationState.Skills.Count == 0;
            
            var chipColor = App.Current.Resources.TryGetValue("Secondary", out var secColor) ? (Color)secColor : Color.FromArgb("#F0F4FF");
            var strokeColor = App.Current.Resources.TryGetValue("Primary", out var priColor) ? (Color)priColor : Color.FromArgb("#6C63FF");
            var darkTextColor = App.Current.Resources.TryGetValue("PrimaryDark", out var pdColor) ? (Color)pdColor : Color.FromArgb("#1C2B53");

            foreach (var s in RegistrationState.Skills)
            {
                var chip = new Border
                {
                    BackgroundColor = chipColor,
                    Stroke = strokeColor,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(12, 6),
                    Margin = new Thickness(4, 4, 4, 4)
                };
                
                var stack = new HorizontalStackLayout { Spacing = 8 };
                stack.Children.Add(new Label 
                { 
                    Text = s, 
                    TextColor = darkTextColor,
                    FontSize = 14,
                    VerticalOptions = LayoutOptions.Center
                });
                stack.Children.Add(new Label 
                { 
                    Text = "×", 
                    TextColor = strokeColor,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                });
                
                chip.Content = stack;
                
                var tap = new TapGestureRecognizer();
                tap.Tapped += (s2, e2) =>
                {
                    RegistrationState.Skills.Remove(s);
                    RefreshSkills();
                };
                chip.GestureRecognizers.Add(tap);
                SkillsWrap.Children.Add(chip);
            }
        }
        void OnAddSkill(object sender, EventArgs e)
        {
            var text = SkillEntry.Text?.Trim();
            if (!string.IsNullOrEmpty(text) && !RegistrationState.Skills.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                RegistrationState.Skills.Add(text);
                SkillEntry.Text = "";
                RefreshSkills();
            }
        }
        async void OnFinish(object sender, EventArgs e)
        {
            if (RegistrationState.Skills.Count == 0)
            {
                await ShowAlertAsync("Skills Required", "Please add at least one skill.", "OK");
                return;
            }
            
            try
            {
                LoadingOverlay.IsVisible = true;
                
                // Step 1: Register the user
                var registerResp = await _api.RegisterAsync(new RegisterReq(
                    RegistrationState.FirstName,
                    RegistrationState.LastName,
                    RegistrationState.Email,
                    RegistrationState.Password,
                    RegistrationState.UserType));
                    
                if (!registerResp.IsSuccessStatusCode)
                {
                    var errorContent = await registerResp.Content.ReadAsStringAsync();
                    LoadingOverlay.IsVisible = false;
                    await ShowAlertAsync("Registration Failed", $"Error: {errorContent}", "OK");
                    return;
                }
                
                var regResult = await registerResp.Content.ReadFromJsonAsync<RegResult>();
                if (regResult == null || regResult.userId <= 0)
                {
                    LoadingOverlay.IsVisible = false;
                    await ShowAlertAsync("Error", "Failed to get user ID from registration.", "OK");
                    return;
                }
                
                var userId = regResult.userId;
                
                // Step 2: Upload photos
                var uploadResp = await _api.UploadPhotosAsync(userId, RegistrationState.SelfiePath, RegistrationState.IdPath);
                if (!uploadResp.IsSuccessStatusCode)
                {
                    LoadingOverlay.IsVisible = false;
                    await ShowAlertAsync("Warning", "Photos upload failed, but account was created.", "OK");
                }
                
                // Step 3: Save skills
                var skillsResp = await _api.SaveSkillsAsync(userId, RegistrationState.Skills);
                
                LoadingOverlay.IsVisible = false;
                
                if (skillsResp.IsSuccessStatusCode)
                {
                    await ShowAlertAsync("Success", "Registration complete. Welcome!", "Continue");
                    RegistrationState.Reset();
                    await Shell.Current.GoToAsync("//MainPage", false);
                }
                else
                {
                    await ShowAlertAsync("Warning", "Skills saving failed, but account was created. You can add skills later.", "OK");
                    RegistrationState.Reset();
                    await Shell.Current.GoToAsync("//MainPage", false);
                }
            }
            catch (Exception ex)
            {
                LoadingOverlay.IsVisible = false;
                await ShowAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
        
        class RegResult { public int userId { get; set; } }
    }
}
