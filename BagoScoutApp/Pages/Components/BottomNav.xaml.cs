namespace BagoScoutApp.Pages.Components;

public partial class BottomNav : ContentView
{
    public static readonly BindableProperty UserTypeProperty =
        BindableProperty.Create(nameof(UserType), typeof(string), typeof(BottomNav), string.Empty, propertyChanged: OnUserTypeChanged);

    public static readonly BindableProperty ActivePageProperty =
        BindableProperty.Create(nameof(ActivePage), typeof(string), typeof(BottomNav), "dashboard", propertyChanged: OnActivePageChanged);

    public string UserType
    {
        get => (string)GetValue(UserTypeProperty);
        set => SetValue(UserTypeProperty, value);
    }

    public string ActivePage
    {
        get => (string)GetValue(ActivePageProperty);
        set => SetValue(ActivePageProperty, value);
    }

    public BottomNav()
    {
        InitializeComponent();
    }

    private static void OnUserTypeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (BottomNav)bindable;
        var userType = newValue as string;
        
        control.SeekerNav.IsVisible = userType == "seeker";
        control.EmployerNav.IsVisible = userType == "employer";
    }

    private static void OnActivePageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (BottomNav)bindable;
        var activePage = newValue as string ?? "dashboard";
        control.UpdateActiveState(activePage);
    }

    private void UpdateActiveState(string activePage)
    {
        if (UserType == "seeker")
        {
            // Reset all seeker items
            ResetSeekerItem(SeekerDashboardBorder, SeekerDashboardLabel);
            ResetSeekerItem(SeekerJobsBorder, SeekerJobsLabel);
            ResetSeekerItem(SeekerApplicationsBorder, SeekerApplicationsLabel);
            ResetSeekerItem(SeekerMessagesBorder, SeekerMessagesLabel);
            ResetSeekerItem(SeekerProfileBorder, SeekerProfileLabel);

            // Set active item
            switch (activePage.ToLower())
            {
                case "dashboard":
                    SetActiveSeekerItem(SeekerDashboardBorder, SeekerDashboardLabel);
                    break;
                case "jobs":
                    SetActiveSeekerItem(SeekerJobsBorder, SeekerJobsLabel);
                    break;
                case "applications":
                    SetActiveSeekerItem(SeekerApplicationsBorder, SeekerApplicationsLabel);
                    break;
                case "messages":
                    SetActiveSeekerItem(SeekerMessagesBorder, SeekerMessagesLabel);
                    break;
                case "profile":
                    SetActiveSeekerItem(SeekerProfileBorder, SeekerProfileLabel);
                    break;
            }
        }
        else if (UserType == "employer")
        {
            // Reset all employer items
            ResetEmployerItem(EmployerDashboardBorder, EmployerDashboardLabel);
            ResetEmployerItem(EmployerPostingsBorder, EmployerPostingsLabel);
            ResetEmployerItem(EmployerCandidatesBorder, EmployerCandidatesLabel);
            ResetEmployerItem(EmployerMessagesBorder, EmployerMessagesLabel);
            ResetEmployerItem(EmployerProfileBorder, EmployerProfileLabel);

            // Set active item
            switch (activePage.ToLower())
            {
                case "dashboard":
                    SetActiveEmployerItem(EmployerDashboardBorder, EmployerDashboardLabel);
                    break;
                case "postings":
                    SetActiveEmployerItem(EmployerPostingsBorder, EmployerPostingsLabel);
                    break;
                case "candidates":
                    SetActiveEmployerItem(EmployerCandidatesBorder, EmployerCandidatesLabel);
                    break;
                case "messages":
                    SetActiveEmployerItem(EmployerMessagesBorder, EmployerMessagesLabel);
                    break;
                case "profile":
                    SetActiveEmployerItem(EmployerProfileBorder, EmployerProfileLabel);
                    break;
            }
        }
    }

    private void ResetSeekerItem(Border border, Label label)
    {
        border.BackgroundColor = Colors.Transparent;
        var iconLabel = (Label)border.Content;
        iconLabel.TextColor = Color.FromArgb("#8D94A8");
        label.TextColor = Color.FromArgb("#8D94A8");
    }

    private void SetActiveSeekerItem(Border border, Label label)
    {
        border.BackgroundColor = Color.FromArgb("#6C63FF");
        var iconLabel = (Label)border.Content;
        iconLabel.TextColor = Colors.White;
        label.TextColor = Colors.White;
    }

    private void ResetEmployerItem(Border border, Label label)
    {
        border.BackgroundColor = Colors.Transparent;
        var iconLabel = (Label)border.Content;
        iconLabel.TextColor = Color.FromArgb("#8D94A8");
        label.TextColor = Color.FromArgb("#8D94A8");
    }

    private void SetActiveEmployerItem(Border border, Label label)
    {
        border.BackgroundColor = Color.FromArgb("#6C63FF");
        var iconLabel = (Label)border.Content;
        iconLabel.TextColor = Colors.White;
        label.TextColor = Colors.White;
    }

    // Seeker Navigation Handlers
    private void OnSeekerDashboardTapped(object sender, EventArgs e)
    {
        ActivePage = "dashboard";
        // Navigation will be handled by the page
    }

    private void OnSeekerJobsTapped(object sender, EventArgs e)
    {
        ActivePage = "jobs";
        // TODO: Navigate to jobs page
    }

    private void OnSeekerApplicationsTapped(object sender, EventArgs e)
    {
        ActivePage = "applications";
        // TODO: Navigate to applications page
    }

    private void OnSeekerMessagesTapped(object sender, EventArgs e)
    {
        ActivePage = "messages";
        // TODO: Navigate to messages page
    }

    private void OnSeekerProfileTapped(object sender, EventArgs e)
    {
        ActivePage = "profile";
        // TODO: Navigate to profile page
    }

    // Employer Navigation Handlers
    private void OnEmployerDashboardTapped(object sender, EventArgs e)
    {
        ActivePage = "dashboard";
        // Navigation will be handled by the page
    }

    private void OnEmployerPostingsTapped(object sender, EventArgs e)
    {
        ActivePage = "postings";
        // TODO: Navigate to postings page
    }

    private void OnEmployerCandidatesTapped(object sender, EventArgs e)
    {
        ActivePage = "candidates";
        // TODO: Navigate to candidates page
    }

    private void OnEmployerMessagesTapped(object sender, EventArgs e)
    {
        ActivePage = "messages";
        // TODO: Navigate to messages page
    }

    private void OnEmployerProfileTapped(object sender, EventArgs e)
    {
        ActivePage = "profile";
        // TODO: Navigate to profile page
    }
}
