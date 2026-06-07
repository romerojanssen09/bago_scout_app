using BagoScoutApp.Pages.Register;
using BagoScoutApp.Pages.Components;

namespace BagoScoutApp.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            
            // Remove safe area insets to prevent white space at top
            this.Padding = new Thickness(0);

            // Subscribe to scroll events
            ComponentStatic.ScrollToTop += OnScrollToTop;
            ComponentStatic.ScrollToAbout += OnScrollToAbout;
            ComponentStatic.ScrollToContact += OnScrollToContact;
        }

        private async void OnScrollToTop(object sender, EventArgs e)
        {
            await MainScrollView.ScrollToAsync(0, 0, true);
        }

        private async void OnScrollToAbout(object sender, EventArgs e)
        {
            await MainScrollView.ScrollToAsync(AboutSection, ScrollToPosition.Start, true);
        }

        private async void OnScrollToContact(object sender, EventArgs e)
        {
            await MainScrollView.ScrollToAsync(ContactSection, ScrollToPosition.Start, true);
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegisterTypePage), false);
        }

        async void OnSendMessageClicked(object sender, EventArgs e)
        {
            await ShowCustomAlert("Message sent", "We will contact you soon", "Close");
        }
        
        Task ShowCustomAlert(string title, string message, string button)
        {
            var tcs = new TaskCompletionSource();
            var overlay = new Grid { BackgroundColor = Color.FromArgb("#80000000") };
            var panel = new Frame
            {
                BackgroundColor = Colors.White,
                Padding = 20,
                CornerRadius = 12,
                WidthRequest = 300,
                HasShadow = true,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            var stack = new VerticalStackLayout { Spacing = 12 };
            stack.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 20, TextColor = Color.FromArgb("#1C2B53") });
            stack.Add(new Label { Text = message, TextColor = Color.FromArgb("#8D94A8") });
            var action = new Button { Text = button, BackgroundColor = Color.FromArgb("#6C63FF"), TextColor = Colors.White, CornerRadius = 8 };
            action.Clicked += (s, e) =>
            {
                if (this.Content is Layout layout)
                    layout.Children.Remove(overlay);
                tcs.SetResult();
            };
            stack.Add(action);
            panel.Content = stack;
            overlay.Children.Add(panel);
            if (this.Content is Layout root)
                root.Children.Add(overlay);
            else
                this.Content = new Grid { Children = { this.Content, overlay } };
            return tcs.Task;
        }

        private async void OnEmailContactTapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.Default.OpenAsync("mailto:BagoScout@gmail.com");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Could not open mail client: " + ex.Message, "OK");
            }
        }

        private void OnPhoneContactTapped(object sender, EventArgs e)
        {
            try
            {
                if (PhoneDialer.Default.IsSupported)
                    PhoneDialer.Default.Open("09564186361");
                else
                    DisplayAlert("Not Supported", "Phone dialing is not supported on this device.", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", "Could not open phone dialer: " + ex.Message, "OK");
            }
        }

        private async void OnLinkedInContactTapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.Default.OpenAsync("https://linkedin.com");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Could not open link: " + ex.Message, "OK");
            }
        }
    }
}
