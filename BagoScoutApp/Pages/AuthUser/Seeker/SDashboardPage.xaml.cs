using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

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
            // Show loading, hide list
            LoadingContainer.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            JobsCollectionView.IsVisible = false;

            try
            {
                // 1. Fetch dashboard data (applications count)
                var dashboardData = await _api.GetSeekerDashboardAsync();
                if (dashboardData != null)
                {
                    AppliedCountLabel.Text = dashboardData.applicationsCount.ToString();
                }

                // 2. Load preferences
                await LoadPreferences();

                // 3. Fetch recommended jobs based on preferences
                var recommendedJobs = await _api.GetRecommendedJobsAsync();
                
                if (recommendedJobs != null && recommendedJobs.Any())
                {
                    // Convert RecommendedJobDto to JobDto for display
                    var jobsList = recommendedJobs.Select(rj => new JobDto
                    {
                        jobId = rj.jobId,
                        title = rj.title,
                        description = rj.description,
                        company = rj.company,
                        address = rj.address,
                        salaryRange = rj.salaryRange,
                        jobType = rj.jobType,
                        experienceLevel = rj.experienceLevel,
                        createdAt = rj.createdAt
                    }).ToList();
                    
                    JobsCollectionView.ItemsSource = jobsList;
                    RecommendedLabel.Text = "Recommended Jobs for You";
                }
                else
                {
                    // No preferences set or no matches, show recent jobs
                    var allJobs = await _api.GetAllJobsAsync();
                    JobsCollectionView.ItemsSource = allJobs?.Take(10).ToList();
                    RecommendedLabel.Text = "Recent Job Opportunities";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading seeker dashboard data: {ex.Message}");
                // Fallback to all jobs on error
                try
                {
                    var fallbackJobs = await _api.GetAllJobsAsync();
                    JobsCollectionView.ItemsSource = fallbackJobs?.Take(10).ToList();
                    RecommendedLabel.Text = "Recent Job Opportunities";
                }
                catch { }
            }
            finally
            {
                // Hide loading, show list
                LoadingContainer.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                JobsCollectionView.IsVisible = true;
            }
        }

        private async Task LoadPreferences()
        {
            try
            {
                var preferences = await _api.GetJobPreferencesAsync();
                
                if (preferences == null || 
                    (string.IsNullOrEmpty(preferences.preferredJobType) && 
                     string.IsNullOrEmpty(preferences.preferredJobTitles) &&
                     string.IsNullOrEmpty(preferences.preferredExperienceLevel) &&
                     preferences.minSalary == null &&
                     preferences.maxSalary == null &&
                     string.IsNullOrEmpty(preferences.preferredLocation) &&
                     preferences.maxDistance == null))
                {
                    // No preferences set
                    PreferencesContainer.Children.Clear();
                    PreferencesContainer.Children.Add(new Label
                    {
                        Text = "No preferences set yet",
                        FontFamily = "Inter",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#64748B"),
                        HorizontalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 10)
                    });
                    PreferencesContainer.Children.Add(new Label
                    {
                        Text = "Tap Edit to set your job preferences",
                        FontFamily = "Inter",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#94A3B8"),
                        HorizontalTextAlignment = TextAlignment.Center
                    });
                }
                else
                {
                    // 1. Clear the view container
                    PreferencesContainer.Children.Clear();

                    var grid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        },
                        RowSpacing = 10,
                        ColumnSpacing = 10
                    };

                    // 2. Collect regular 1-column items separate from the full-width location item
                    var regularItems = new List<IView>();
                    IView locationItem = null;

                    if (!string.IsNullOrEmpty(preferences.preferredJobType))
                        regularItems.Add(CreatePreferenceItem("Job Type", preferences.preferredJobType));

                    if (!string.IsNullOrEmpty(preferences.preferredJobTitles))
                        regularItems.Add(CreatePreferenceItem("Job Titles", preferences.preferredJobTitles));

                    if (!string.IsNullOrEmpty(preferences.preferredExperienceLevel))
                        regularItems.Add(CreatePreferenceItem("Experience", preferences.preferredExperienceLevel));

                    if (preferences.minSalary.HasValue || preferences.maxSalary.HasValue)
                    {
                        var salaryText = $"₱{preferences.minSalary?.ToString("N0") ?? "Any"} - ₱{preferences.maxSalary?.ToString("N0") ?? "Any"}";
                        regularItems.Add(CreatePreferenceItem("Salary", salaryText));
                    }

                    if (preferences.maxDistance.HasValue)
                        regularItems.Add(CreatePreferenceItem("Max Distance", $"{preferences.maxDistance} km"));

                    if (!string.IsNullOrEmpty(preferences.preferredLocation))
                        locationItem = CreatePreferenceItem("Location", preferences.preferredLocation);

                    // 3. Populate all regular 1-column items sequentially
                    int currentRow = 0;
                    int currentCol = 0;

                    foreach (var item in regularItems)
                    {
                        // Explicitly ensure the grid row exists
                        if (grid.RowDefinitions.Count <= currentRow)
                        {
                            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        }

                        grid.SetRow(item, currentRow);
                        grid.SetColumn(item, currentCol);
                        grid.Children.Add(item);

                        currentCol++;
                        if (currentCol > 1)
                        {
                            currentCol = 0;
                            currentRow++;
                        }
                    }

                    // 4. Place the Location field safely at the very bottom spanning both columns
                    if (locationItem != null)
                    {
                        // If the last regular row was partially filled, advance to a clean row
                        if (currentCol == 1)
                        {
                            currentRow++;
                        }

                        // Add a dedicated final row for the location item
                        if (grid.RowDefinitions.Count <= currentRow)
                        {
                            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        }

                        grid.SetRow(locationItem, currentRow);
                        grid.SetColumn(locationItem, 0);
                        grid.SetColumnSpan(locationItem, 2);
                        grid.Children.Add(locationItem);
                    }

                    PreferencesContainer.Children.Add(grid);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading preferences: {ex.Message}");
                PreferencesContainer.Children.Clear();
                PreferencesContainer.Children.Add(new Label
                {
                    Text = "Error loading preferences",
                    FontFamily = "Inter",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#EF4444"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 10)
                });
            }
        }

        private Border CreatePreferenceItem(string label, string value)
        {
            return new Border
            {
                BackgroundColor = Color.FromArgb("#F8FAFC"),
                StrokeThickness = 0,
                Padding = new Thickness(12, 10),
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text = label,
                            FontFamily = "Inter",
                            FontSize = 11,
                            TextColor = Color.FromArgb("#64748B")
                        },
                        new Label
                        {
                            Text = value,
                            FontFamily = "Inter",
                            FontAttributes = FontAttributes.Bold,
                            FontSize = 13,
                            TextColor = Color.FromArgb("#0F172A"),
                            LineBreakMode = LineBreakMode.TailTruncation,
                            MaxLines = 2
                        }
                    }
                }
            };
        }

        private async void OnEditPreferencesClicked(object sender, EventArgs e)
        {
            // Navigate to preferences page
            await Navigation.PushAsync(new SPreferencesPage());
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

        private async void OnViewAllJobsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///SJobsPage");
        }

        private async void OnBuildingIconTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///SProfilePage");
        }
    }
}
