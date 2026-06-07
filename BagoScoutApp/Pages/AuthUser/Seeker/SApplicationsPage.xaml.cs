using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace BagoScoutApp.Pages.AuthUser.Seeker
{
    public partial class SApplicationsPage : BasePage
    {
        private readonly ApiClient _api = new();

        public SApplicationsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadApplications();
        }

        private async Task LoadApplications()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                var apps = await _api.GetSeekerApplicationsAsync();
                AppsCollectionView.ItemsSource = apps;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading seeker applications: {ex.Message}");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnViewJobClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ApplicationDto app)
            {
                await ShowJobDetailsOverlay(app.jobId);
            }
        }

        private void OnCloseJobDetailsClicked(object sender, EventArgs e)
        {
            JobDetailsOverlay.IsVisible = false;
        }

        private async Task ShowJobDetailsOverlay(int jobId)
        {
            try
            {
                ShowLoading("Loading job details...");
                var job = await _api.GetJobByIdAsync(jobId);
                HideLoading();

                if (job == null)
                {
                    await ShowAlertAsync("Error", "Could not fetch job details.", "OK");
                    return;
                }

                JobDetailTitleLabel.Text = job.title;
                JobDetailCompanyLabel.Text = job.company;
                JobDetailSalaryLabel.Text = string.IsNullOrEmpty(job.salaryRange) ? "Negotiable" : job.salaryRange;
                JobDetailTypeLabel.Text = job.jobType;
                JobDetailAddressLabel.FormattedText = new FormattedString
                {
                    Spans = 
                    {
                        new Span { Text = "\uf3c5 ", FontFamily = "FASolid" },
                        new Span { Text = job.address }
                    }
                };
                JobDetailDescLabel.Text = job.description;
                JobDetailReqLabel.Text = string.IsNullOrEmpty(job.requirements) ? "No specific requirements listed." : job.requirements;

                JobDetailsOverlay.IsVisible = true;
            }
            catch (Exception ex)
            {
                HideLoading();
                await ShowAlertAsync("Error", $"Failed to load job details: {ex.Message}", "OK");
            }
        }

        private async void OnViewCompanyClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ApplicationDto app)
            {
                await ShowCompanyProfileOverlay(app.employerId);
            }
        }

        private async void OnWithdrawClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ApplicationDto app)
            {
                var confirm = await ShowConfirmAsync("Withdraw Application?", 
                    "Are you sure you want to withdraw this application? This action cannot be undone.", 
                    "Yes, withdraw it", "Cancel");
                if (confirm)
                {
                    await ShowAlertAsync("Feature Coming Soon", "Application withdrawal feature will be available soon!", "OK");
                }
            }
        }

        private async void OnViewEmailsTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ApplicationDto app)
            {
                await ShowEmailsOverlay(app.applicationId);
            }
        }

        private void OnCloseEmailOverlayClicked(object sender, EventArgs e)
        {
            EmailOverlay.IsVisible = false;
        }

        private void OnCloseProfileOverlayClicked(object sender, EventArgs e)
        {
            ProfileDetailsOverlay.IsVisible = false;
        }

        private async Task ShowCompanyProfileOverlay(int employerId)
        {
            try
            {
                ShowLoading("Loading company profile...");
                var profile = await _api.GetCompanyProfileByIdAsync(employerId);
                if (profile == null)
                {
                    HideLoading();
                    await ShowAlertAsync("Error", "Could not fetch company profile details.", "OK");
                    return;
                }

                if (!string.IsNullOrEmpty(profile.companyLogoPath))
                {
                    var path = profile.companyLogoPath.Replace('\\', '/');
                    OverlayLogoImage.Source = path.StartsWith("http")
                        ? ImageSource.FromUri(new Uri(path))
                        : ImageSource.FromUri(new Uri($"{_api.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}"));
                }
                else
                {
                    OverlayLogoImage.Source = null;
                }

                OverlayCompanyNameLabel.Text = profile.companyName;
                OverlayIndustryLabel.Text = !string.IsNullOrEmpty(profile.companyIndustry) ? profile.companyIndustry : "Employer";
                OverlayDescriptionLabel.Text = !string.IsNullOrEmpty(profile.companyDescription) ? profile.companyDescription : "No description available.";
                OverlayAddressLabel.Text = $"Address: {profile.companyAddress}";
                OverlaySizeLabel.Text = $"Size: {profile.companySize}";
                OverlayWebsiteLabel.Text = $"Website: {(!string.IsNullOrEmpty(profile.companyWebsite) ? profile.companyWebsite : "N/A")}";

                OverlayRepNameLabel.Text = $"Representative: {profile.firstName} {profile.lastName}";
                OverlayRepEmailLabel.Text = $"✉ {profile.email}";
                OverlayRepPhoneLabel.Text = $"☏ {profile.phoneNumber ?? "N/A"}";

                HideLoading();
                ProfileDetailsOverlay.IsVisible = true;
            }
            catch (Exception ex)
            {
                HideLoading();
                await ShowAlertAsync("Error", $"Failed to show company profile: {ex.Message}", "OK");
            }
        }

        private async Task ShowEmailsOverlay(int applicationId)
        {
            try
            {
                ShowLoading("Loading email history...");
                var emails = await _api.GetEmailHistoryAsync(applicationId);
                HideLoading();

                if (emails == null || emails.Count == 0)
                {
                    await ShowAlertAsync("No Emails", "No emails found for this application.", "OK");
                    return;
                }

                EmailHistoryLayout.Children.Clear();
                foreach (var email in emails)
                {
                    var stack = new VerticalStackLayout { Spacing = 4, Margin = new Thickness(0, 0, 0, 12) };
                    
                    var headerGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
                    
                    var subjectLabel = new Label 
                    { 
                        Text = email.subject, 
                        FontAttributes = FontAttributes.Bold, 
                        FontSize = 13, 
                        TextColor = Color.FromArgb("#1C2B53") 
                    };
                    Grid.SetColumn(subjectLabel, 0);
                    
                    var dateLabel = new Label 
                    { 
                        Text = email.sentAt.ToLocalTime().ToString("MMM dd, yyyy h:mm tt"), 
                        FontSize = 10, 
                        TextColor = Color.FromArgb("#8D94A8"), 
                        VerticalOptions = LayoutOptions.Center 
                    };
                    Grid.SetColumn(dateLabel, 1);
                    
                    headerGrid.Children.Add(subjectLabel);
                    headerGrid.Children.Add(dateLabel);
                    
                    var bodyLabel = new Label 
                    { 
                        Text = email.message, 
                        FontSize = 12, 
                        TextColor = Color.FromArgb("#4B5563"), 
                        LineBreakMode = LineBreakMode.WordWrap 
                    };

                    var divider = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB"), Margin = new Thickness(0, 8, 0, 0) };

                    stack.Children.Add(headerGrid);
                    stack.Children.Add(bodyLabel);
                    stack.Children.Add(divider);
                    
                    EmailHistoryLayout.Children.Add(stack);
                }

                EmailOverlay.IsVisible = true;
            }
            catch (Exception ex)
            {
                HideLoading();
                await ShowAlertAsync("Error", $"Failed to show emails: {ex.Message}", "OK");
            }
        }
    }
}
