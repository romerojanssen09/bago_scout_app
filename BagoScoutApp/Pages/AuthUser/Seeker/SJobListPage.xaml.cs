using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BagoScoutApp.Pages.AuthUser.Seeker
{
    public partial class SJobListPage : BasePage
    {
        private readonly ApiClient _api = new();
        private List<JobDto> _allJobs = new();
        private List<JobDto> _filteredJobs = new();
        private string _selectedCategory = "";
        private string _searchQuery = "";
        private Location? _lastUserLocation;

        public SJobListPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadJobsList();
        }

        private void LoadCachedJobs()
        {
            try
            {
                var cachedJson = Preferences.Get("CachedJobsList", string.Empty);
                if (!string.IsNullOrEmpty(cachedJson))
                {
                    var jobs = System.Text.Json.JsonSerializer.Deserialize<List<JobDto>>(cachedJson);
                    if (jobs != null && jobs.Any())
                    {
                        _allJobs = jobs;
                        if (_lastUserLocation != null)
                        {
                            UpdateJobsDistanceDisplay(_lastUserLocation);
                        }
                        ApplyFilters();
                        System.Diagnostics.Debug.WriteLine($"[SJobListPage] Loaded {_allJobs.Count} cached jobs on startup.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobListPage] Error loading cached jobs: {ex.Message}");
            }
        }

        private void UpdateJobsDistanceDisplay(Location userLoc)
        {
            foreach (var job in _allJobs)
            {
                if (job.latitude.HasValue && job.longitude.HasValue)
                {
                    var jobLoc = new Location(job.latitude.Value, job.longitude.Value);
                    double distance = Location.CalculateDistance(userLoc, jobLoc, DistanceUnits.Kilometers);
                    job.distanceDisplay = $"{distance:F1} km";
                }
            }
        }

        private async Task LoadJobsList()
        {
            System.Diagnostics.Debug.WriteLine("[SJobListPage] LoadJobsList called.");
            
            // Try loading from cache first for instant display
            if (!_allJobs.Any())
            {
                LoadCachedJobs();
            }

            bool showIndicator = !_allJobs.Any();
            if (showIndicator)
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[SJobListPage] Fetching fresh jobs list from API...");
                var jobs = await _api.GetAllJobsAsync();
                
                if (jobs != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SJobListPage] API returned {jobs.Count} jobs.");
                    _allJobs = jobs;
                    
                    if (_lastUserLocation != null)
                    {
                        UpdateJobsDistanceDisplay(_lastUserLocation);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SJobListPage] API returned null jobs.");
                }

                System.Diagnostics.Debug.WriteLine($"[SJobListPage] Calling ApplyFilters. _allJobs.Count={_allJobs.Count}");
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobListPage] Error loading jobs: {ex.Message}\n{ex.StackTrace}");
                ApplyFilters();
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }

            // Background location tracking
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status == PermissionStatus.Granted)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var userLoc = await Geolocation.Default.GetLastKnownLocationAsync();
                            if (userLoc == null)
                            {
                                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3));
                                userLoc = await Geolocation.Default.GetLocationAsync(request);
                            }

                            if (userLoc != null)
                            {
                                _lastUserLocation = userLoc;
                                System.Diagnostics.Debug.WriteLine($"[SJobListPage] Location acquired in background: {userLoc.Latitude}, {userLoc.Longitude}. Updating distances.");
                                UpdateJobsDistanceDisplay(userLoc);

                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    ApplyFilters();
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SJobListPage] Error in background location retrieval: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobListPage] Error in location permission setup: {ex.Message}");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = e.NewTextValue?.Trim().ToLower() ?? "";
            ApplyFilters();
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                _selectedCategory = button.CommandParameter?.ToString() ?? "";

                var primaryVal = App.Current.Resources.TryGetValue("Primary", out var priVal) ? (Color)priVal : Color.FromArgb("#6C63FF");
                var primaryDarkVal = App.Current.Resources.TryGetValue("PrimaryDark", out var pdVal) ? (Color)pdVal : Color.FromArgb("#1C2B53");
                var gray200Val = App.Current.Resources.TryGetValue("Gray200", out var g2Val) ? (Color)g2Val : Color.FromArgb("#E5E7EB");

                // Reset all filter buttons styling
                var buttons = new[] { AllFilterBtn, FtFilterBtn, PtFilterBtn, ContractFilterBtn, InternFilterBtn };
                foreach (var btn in buttons)
                {
                    btn.BackgroundColor = Colors.White;
                    btn.TextColor = primaryDarkVal;
                    btn.BorderColor = gray200Val;
                    btn.BorderWidth = 1;
                }

                // Highlight selected button
                button.BackgroundColor = primaryVal;
                button.TextColor = Colors.White;
                button.BorderWidth = 0;

                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            System.Diagnostics.Debug.WriteLine($"[SJobListPage] ApplyFilters: _allJobs.Count={_allJobs.Count}, category='{_selectedCategory}', search='{_searchQuery}'");
            
            var result = _allJobs.ToList();

            // Apply category filter
            if (!string.IsNullOrEmpty(_selectedCategory))
            {
                result = result.Where(j => j.jobType != null && j.jobType.Equals(_selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply search query filter
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                result = result.Where(j => 
                    (j.title != null && j.title.ToLower().Contains(_searchQuery)) || 
                    (j.company != null && j.company.ToLower().Contains(_searchQuery))
                ).ToList();
            }

            _filteredJobs = result;
            System.Diagnostics.Debug.WriteLine($"[SJobListPage] ApplyFilters result: {_filteredJobs.Count} jobs after filtering.");
            
            JobsCollectionView.ItemsSource = _filteredJobs;
        }
        private async void OnJobItemTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is JobDto clickedJob)
            {
                // Navigate using Shell routing to map view with job details
                await Shell.Current.Navigation.PushAsync(new SJobsPage { 
                    TargetStaticJobId = clickedJob.jobId.ToString(),
                    TargetJobLat = clickedJob.latitude?.ToString() ?? "",
                    TargetJobLng = clickedJob.longitude?.ToString() ?? ""
                }, false);
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new SJobsPage(), false);
        }

        private async void OnMapViewClicked(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new SJobsPage(), false);
        }
    }
}
