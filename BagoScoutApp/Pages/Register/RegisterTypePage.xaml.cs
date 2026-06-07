using BagoScoutApp.Services;
using BagoScoutApp.Models;
using BagoScoutApp.Pages.Components;

namespace BagoScoutApp.Pages.Register
{
    public partial class RegisterTypePage : BasePage
    {
        private Border? _seekerBorder;
        private Border? _employerBorder;

        public RegisterTypePage()
        {
            InitializeComponent();
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Find the borders after the page is loaded
            _seekerBorder = this.FindByName<Border>("SeekerBorder");
            _employerBorder = this.FindByName<Border>("EmployerBorder");
        }
        
        async void OnCloseTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }
        
        async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }
        
        async void OnNextClicked(object sender, EventArgs e)
        {
            // Navigate to next step if user type is selected
            if (!string.IsNullOrEmpty(RegistrationState.UserType))
            {
                await Shell.Current.GoToAsync(nameof(RegisterAccountPage), false);
            }
            else
            {
                await ShowAlertAsync("Selection Required", "Please select Job Seeker or Employer", "OK");
            }
        }
        
        async void OnLoginTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }
        
        void OnSeeker(object sender, EventArgs e)
        {
            RegistrationState.UserType = "seeker";
            
            // Update border styles
            if (_seekerBorder != null)
            {
                _seekerBorder.Stroke = Color.FromArgb("#6C63FF");
                _seekerBorder.StrokeThickness = 2;
            }
            if (_employerBorder != null)
            {
                _employerBorder.Stroke = Color.FromArgb("#E5E7EB");
                _employerBorder.StrokeThickness = 2;
            }
        }
        
        void OnEmployer(object sender, EventArgs e)
        {
            RegistrationState.UserType = "employer";
            
            // Update border styles
            if (_employerBorder != null)
            {
                _employerBorder.Stroke = Color.FromArgb("#6C63FF");
                _employerBorder.StrokeThickness = 2;
            }
            if (_seekerBorder != null)
            {
                _seekerBorder.Stroke = Color.FromArgb("#E5E7EB");
                _seekerBorder.StrokeThickness = 2;
            }
        }
    }
}
