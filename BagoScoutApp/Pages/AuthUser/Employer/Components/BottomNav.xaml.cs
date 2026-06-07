namespace BagoScoutApp.Pages.AuthUser.Employer.Components;

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
        PostingsIcon.TextColor = Color.FromArgb("#8D94A8");
        PostingsLabel.TextColor = Color.FromArgb("#8D94A8");
        CandidatesIcon.TextColor = Color.FromArgb("#8D94A8");
        CandidatesLabel.TextColor = Color.FromArgb("#8D94A8");
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
            case "postings":
                PostingsIcon.TextColor = activeColor;
                PostingsLabel.TextColor = activeColor;
                break;
            case "candidates":
                CandidatesIcon.TextColor = activeColor;
                CandidatesLabel.TextColor = activeColor;
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
            await Shell.Current.Navigation.PushAsync(new EDashboardPage(), false);
        }
    }

    private async void OnPostingsTapped(object sender, EventArgs e)
    {
        if (ActivePage != "postings")
        {
            await Shell.Current.Navigation.PushAsync(new EPostingsPage(), false);
        }
    }

    private async void OnCandidatesTapped(object sender, EventArgs e)
    {
        if (ActivePage != "candidates")
        {
            await Shell.Current.Navigation.PushAsync(new ECandidatesPage(), false);
        }
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        if (ActivePage != "messages")
        {
            await Shell.Current.Navigation.PushAsync(new EMessagesPage(), false);
        }
    }

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        if (ActivePage != "profile")
        {
            await Shell.Current.Navigation.PushAsync(new EProfilePage(), false);
        }
    }
}
