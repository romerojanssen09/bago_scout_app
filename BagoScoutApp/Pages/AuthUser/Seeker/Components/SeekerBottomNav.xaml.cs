namespace BagoScoutApp.Pages.AuthUser.Seeker.Components;

public partial class BottomNav : ContentView
{
    public static readonly BindableProperty ActivePageProperty =
        BindableProperty.Create(nameof(ActivePage), typeof(string), typeof(BottomNav), "dashboard", propertyChanged: OnActivePageChanged);

    public string ActivePage
    {
        get => (string)GetValue(ActivePageProperty);
        set => SetValue(ActivePageProperty, value);
    }

    public BottomNav()
    {
        InitializeComponent();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        UpdateHighlight(ActivePage);
    }

    private static void OnActivePageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (BottomNav)bindable;
        control.UpdateHighlight(newValue as string ?? "dashboard");
    }

    private void UpdateHighlight(string activePage)
    {
        if (DashboardIcon == null) return;

        // Reset all to inactive color
        DashboardIcon.TextColor = Color.FromArgb("#8D94A8");
        DashboardLabel.TextColor = Color.FromArgb("#8D94A8");
        JobsIcon.TextColor = Color.FromArgb("#8D94A8");
        JobsLabel.TextColor = Color.FromArgb("#8D94A8");
        ApplicationsIcon.TextColor = Color.FromArgb("#8D94A8");
        ApplicationsLabel.TextColor = Color.FromArgb("#8D94A8");
        MessagesIcon.TextColor = Color.FromArgb("#8D94A8");
        MessagesLabel.TextColor = Color.FromArgb("#8D94A8");
        ProfileIcon.TextColor = Color.FromArgb("#8D94A8");
        ProfileLabel.TextColor = Color.FromArgb("#8D94A8");

        // Set active color
        var activeColor = Color.FromArgb("#6C63FF");
        switch (activePage.ToLower())
        {
            case "dashboard":
                DashboardIcon.TextColor = activeColor;
                DashboardLabel.TextColor = activeColor;
                break;
            case "jobs":
                JobsIcon.TextColor = activeColor;
                JobsLabel.TextColor = activeColor;
                break;
            case "applications":
                ApplicationsIcon.TextColor = activeColor;
                ApplicationsLabel.TextColor = activeColor;
                break;
            case "messages":
                MessagesIcon.TextColor = activeColor;
                MessagesLabel.TextColor = activeColor;
                break;
            case "profile":
                ProfileIcon.TextColor = activeColor;
                ProfileLabel.TextColor = activeColor;
                break;
        }
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        if (ActivePage != "dashboard")
        {
            await Shell.Current.Navigation.PushAsync(new SDashboardPage(), false);
        }
    }

    private async void OnJobsTapped(object sender, EventArgs e)
    {
        if (ActivePage != "jobs")
        {
            await Shell.Current.Navigation.PushAsync(new SJobsPage(), false);
        }
    }

    private async void OnApplicationsTapped(object sender, EventArgs e)
    {
        if (ActivePage != "applications")
        {
            await Shell.Current.Navigation.PushAsync(new SApplicationsPage(), false);
        }
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        if (ActivePage != "messages")
        {
            await Shell.Current.Navigation.PushAsync(new SMessagesPage(), false);
        }
    }

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        if (ActivePage != "profile")
        {
            await Shell.Current.Navigation.PushAsync(new SProfilePage(), false);
        }
    }
}
