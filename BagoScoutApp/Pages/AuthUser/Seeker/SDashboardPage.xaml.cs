using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BagoScoutApp.Pages.AuthUser.Seeker
{
    public partial class SDashboardPage : BasePage
    {
        private readonly ApiClient _api = new();

        public SDashboardPage()
        {
            InitializeComponent();
            WelcomeLabel.Text = $"Welcome back, {Preferences.Get("UserName", "User")}! 👋";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDashboardData();
        }

        private async Task LoadDashboardData()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                // 1. Fetch dashboard data (applications count)
                var dashboardData = await _api.GetSeekerDashboardAsync();
                if (dashboardData != null)
                {
                    AppliedCountLabel.Text = dashboardData.applicationsCount.ToString();
                }

                // 2. Fetch all jobs
                var jobsList = await _api.GetAllJobsAsync();
                JobsCollectionView.ItemsSource = jobsList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading seeker dashboard data: {ex.Message}");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnJobTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is JobDto selectedJob)
            {
                // Navigate to search jobs tab or show alert
                // To keep it simple, we navigate to the search jobs tab and pass selected job ID
                await Shell.Current.GoToAsync($"//SJobsPage?jobId={selectedJob.jobId}", false);
            }
        }
    }
}
