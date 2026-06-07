using BagoScoutApp.Pages;
using BagoScoutApp.Pages.Register;
using BagoScoutApp.Pages.AuthUser.Employer;
using BagoScoutApp.Pages.AuthUser.Seeker;

namespace BagoScoutApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes for navigation (Detail flow pages only, root pages registered in AppShell.xaml)
            Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
            Routing.RegisterRoute(nameof(RegisterTypePage), typeof(RegisterTypePage));
            Routing.RegisterRoute(nameof(RegisterAccountPage), typeof(RegisterAccountPage));
            Routing.RegisterRoute(nameof(RegisterVerifyPage), typeof(RegisterVerifyPage));
            Routing.RegisterRoute(nameof(RegisterIdPage), typeof(RegisterIdPage));
            Routing.RegisterRoute(nameof(RegisterSkillsPage), typeof(RegisterSkillsPage));
        }
    }
}
