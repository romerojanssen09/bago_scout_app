using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BagoScoutApp.Pages.AuthUser.Employer
{
    public partial class EDashboardPage : BasePage
    {
        private readonly ApiClient _api = new();

        public EDashboardPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Set welcome name
            var employerName = Preferences.Get("UserName", "Employer");
            WelcomeLabel.Text = $"Welcome back, {employerName}! 👋";
            
            await LoadDashboardData();
        }

        private async Task LoadDashboardData()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                var jobsTask = _api.GetEmployerJobsAsync();
                var appsTask = _api.GetAllEmployerApplicationsAsync();

                await Task.WhenAll(jobsTask, appsTask);

                var jobs = jobsTask.Result ?? new List<JobDto>();
                var apps = appsTask.Result ?? new List<ApplicationDto>();

                // Calculate stats
                int activeJobs = jobs.Count(j => j.isActive);
                int totalApps = apps.Count;
                int pendingApps = apps.Count(a => a.status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
                int hiredApps = apps.Count(a => a.status.Equals("Accepted", StringComparison.OrdinalIgnoreCase));

                ActiveJobsCountLabel.Text = activeJobs.ToString();
                TotalApplicantsLabel.Text = totalApps.ToString();
                PendingCountLabel.Text = pendingApps.ToString();
                AcceptedCountLabel.Text = hiredApps.ToString();

                // Format photo paths for recent applicants before binding
                foreach (var app in apps)
                {
                    if (!string.IsNullOrEmpty(app.seekerPhoto))
                    {
                        var path = app.seekerPhoto.Replace('\\', '/');
                        app.seekerPhoto = path.StartsWith("http")
                            ? path
                            : $"{_api.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
                    }
                }

                // Show top 5 recent applicants
                var recentApplicants = apps.OrderByDescending(a => a.appliedAt).Take(5).ToList();
                ApplicantsCollectionView.ItemsSource = recentApplicants;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading employer dashboard data: {ex.Message}");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnViewAllApplicantsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"//{nameof(ECandidatesPage)}", false);
        }

        private async void OnApplicantTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ApplicationDto applicant)
            {
                // Go to candidates page and we can filter by job if needed, or simply switch to candidates tab
                await Shell.Current.GoToAsync($"//{nameof(ECandidatesPage)}", false);
            }
        }
    }
}
