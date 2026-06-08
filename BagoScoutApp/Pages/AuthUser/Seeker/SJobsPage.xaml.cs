using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BagoScoutApp.Pages.AuthUser.Seeker
{
    [QueryProperty(nameof(TargetJobId), "jobId")]
    public partial class SJobsPage : BasePage
    {
        private readonly ApiClient _api = new();
        private List<JobDto> _allJobs = new();
        private JobDto? _currentSelectedJob;
        private Location? _lastUserLocation;
        private bool _jobsLoaded; // skip reload when returning from sub-pages

        public string TargetJobId
        {
            get => _targetJobId;
            set
            {
                _targetJobId = value;
                OnPropertyChanged();
                if (!string.IsNullOrEmpty(_targetJobId) && int.TryParse(_targetJobId, out var jobId))
                {
                    ShowJobDetailsById(jobId);
                }
            }
        }

        public string TargetStaticJobId = "";
        public string TargetJobLat = "";
        public string TargetJobLng = "";

        private string _targetJobId = "";

        public SJobsPage()
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(TargetStaticJobId) && int.TryParse(TargetStaticJobId, out var staticJobId))
            {
                TargetJobId = TargetStaticJobId;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Only do the full load once; on return from sub-pages just restore state
            if (!_jobsLoaded)
            {
                _jobsLoaded = true;
                await LoadJobsList();
            }
            else
            {
                // Re-push jobs in case the map WebView reloaded while hidden
                if (_allJobs.Any())
                    JobMap.Jobs = _allJobs;
            }
            
            // Check location permission status on page appearance
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted && _lastUserLocation != null)
            {
                JobMap.UpdateUserLocation(_lastUserLocation.Latitude, _lastUserLocation.Longitude);
            }

            // Check if coordinates were supplied for direct centering (Requirement 5.1)
            if (!string.IsNullOrEmpty(TargetJobLat) && !string.IsNullOrEmpty(TargetJobLng) &&
                double.TryParse(TargetJobLat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(TargetJobLng, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
            {
                JobMap.CenterOnLocation(lat, lng, 15, 800);

                if (!string.IsNullOrEmpty(TargetStaticJobId) && int.TryParse(TargetStaticJobId, out var staticJobId))
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(850); // Complete animation before modal (Requirement 5.3)
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ShowJobDetailsById(staticJobId);
                        });
                    });
                }
            }
            else
            {
                // Restore map center and zoom if no parameters supplied (Requirement 12.2)
                RestoreMapState();
            }

            // Check if passed via query property
            if (!string.IsNullOrEmpty(TargetJobId) && int.TryParse(TargetJobId, out var jobId))
            {
                ShowJobDetailsById(jobId);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Save map center and zoom level (Requirement 12.1)
            SaveMapState();
        }

        private void SaveMapState()
        {
            try
            {
                var (center, zoom) = JobMap.GetCenterAndZoom();
                Preferences.Set("LastMapCenterLat", center.X);
                Preferences.Set("LastMapCenterLng", center.Y);
                Preferences.Set("LastMapZoom", zoom);
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Saved map state: Lat {center.X}, Lng {center.Y}, Zoom {zoom}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Error saving map state: {ex.Message}");
            }
        }

        private void RestoreMapState()
        {
            try
            {
                if (Preferences.ContainsKey("LastMapCenterLat") && Preferences.ContainsKey("LastMapCenterLng"))
                {
                    var lat = Preferences.Get("LastMapCenterLat", 10.5306);
                    var lng = Preferences.Get("LastMapCenterLng", 122.8428);
                    var zoom = Preferences.Get("LastMapZoom", 12f);

                    JobMap.CenterOnLocation(lat, lng, zoom, 0); // instant jump
                    System.Diagnostics.Debug.WriteLine($"[SJobsPage] Restored map state: Lat {lat}, Lng {lng}, Zoom {zoom}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Error restoring map state: {ex.Message}");
            }
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
                        System.Diagnostics.Debug.WriteLine($"[SJobsPage] Loaded {_allJobs.Count} cached jobs on startup.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Error loading cached jobs: {ex.Message}");
            }
        }

        private void SaveJobsToCache(List<JobDto> jobs)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(jobs);
                Preferences.Set("CachedJobsList", json);
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Saved {jobs.Count} jobs to cache.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Error saving jobs to cache: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("[SJobsPage] LoadJobsList called.");
            
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
                System.Diagnostics.Debug.WriteLine("[SJobsPage] Fetching fresh jobs list from API...");
                var jobs = await _api.GetAllJobsAsync();
                
                if (jobs != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SJobsPage] API returned {jobs.Count} jobs.");
                    _allJobs = jobs;
                    SaveJobsToCache(_allJobs);
                    
                    if (_lastUserLocation != null)
                    {
                        UpdateJobsDistanceDisplay(_lastUserLocation);
                    }
                }

                // Initialize map with jobs (Requirement 1.6)
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Initializing map with {_allJobs.Count} jobs.");
                InitializeMap(_allJobs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Error loading jobs: {ex.Message}\n{ex.StackTrace}");
                InitializeMap(_allJobs);
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }

            // Geolocation permissions and user location acquisition
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
                                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Location acquired: {userLoc.Latitude}, {userLoc.Longitude}.");
                                UpdateJobsDistanceDisplay(userLoc);

                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    // Show user dot on map
                                    JobMap.UpdateUserLocation(userLoc.Latitude, userLoc.Longitude);

                                    // Set initial center if no saved state and no target coords
                                    if (string.IsNullOrEmpty(TargetJobLat) && !Preferences.ContainsKey("LastMapCenterLat"))
                                    {
                                        JobMap.SetInitialCenter(userLoc.Latitude, userLoc.Longitude, 12);
                                    }
                                    InitializeMap(_allJobs);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SJobsPage] Background location error: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SJobsPage] Location permission error: {ex.Message}");
            }
        }

        private void OpenJobDetails(JobDto job)
        {
            _currentSelectedJob = job;

            DetailTitleLabel.Text = job.title;
            DetailCompanyLabel.Text = job.company;
            DetailSalaryLabel.Text = string.IsNullOrEmpty(job.salaryRange) ? "Negotiable" : job.salaryRange;
            DetailTypeLabel.Text = job.jobType;
            DetailAddressLabel.FormattedText = new FormattedString
            {
                Spans = 
                {
                    new Span { Text = "\uf3c5 ", FontFamily = "FASolid" },
                    new Span { Text = job.address }
                }
            };
            DetailDescLabel.Text = job.description;
            DetailReqLabel.Text = string.IsNullOrEmpty(job.requirements) ? "No specific requirements listed." : job.requirements;

            CoverLetterEditor.Text = "";
            DetailsOverlay.IsVisible = true;
            // Keep map visible under the dark overlay — hiding it shows the grey page background
            MyLocationButton.IsVisible = false;
            JobListButton.IsVisible = false; 
        }

        private void OnCloseDetailsTapped(object sender, EventArgs e)
        {
            DetailsOverlay.IsVisible = false;
            MyLocationButton.IsVisible = true;
            JobListButton.IsVisible = true; 
            _currentSelectedJob = null;
            TargetJobId = "";
            
            // Clear selected marker highlight
            JobMap.SelectedJob = null;
        }

        private async void ShowJobDetailsById(int jobId)
        {
            try
            {
                var job = _allJobs.FirstOrDefault(j => j.jobId == jobId);
                if (job == null)
                {
                    job = await _api.GetJobByIdAsync(jobId);
                }

                if (job != null)
                {
                    OpenJobDetails(job);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving job by ID {jobId}: {ex.Message}");
            }
        }

        private async void OnSubmitApplicationClicked(object sender, EventArgs e)
        {
            if (_currentSelectedJob == null) return;

            ApplyBtn.IsEnabled = false;
            ApplyBtn.Text = "Submitting...";

            try
            {
                var coverLetter = CoverLetterEditor.Text?.Trim() ?? "";
                var response = await _api.ApplyToJobAsync(_currentSelectedJob.jobId, coverLetter);

                if (response.IsSuccessStatusCode)
                {
                    await ShowAlertAsync("Application Submitted!", $"You have successfully applied for {_currentSelectedJob.title} at {_currentSelectedJob.company}.", "Awesome");
                    DetailsOverlay.IsVisible = false;
                    MyLocationButton.IsVisible = true;
                    JobListButton.IsVisible = true;
                    _currentSelectedJob = null;
                    TargetJobId = "";
                    JobMap.SelectedJob = null;
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content.Contains("already applied"))
                    {
                        await ShowAlertAsync("Already Applied", "You have already submitted an application for this job posting.", "OK");
                    }
                    else
                    {
                        await ShowAlertAsync("Error", "Unable to submit application. Please try again later.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Connection Failed", $"Error connecting to the server: {ex.Message}", "OK");
            }
            finally
            {
                ApplyBtn.IsEnabled = true;
                ApplyBtn.Text = "Apply to Position";
            }
        }

        private async void OnJobListClicked(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new SJobListPage(), false);
        }

        private async void OnMyLocationClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[SJobsPage] OnMyLocationClicked: checking/requesting location permissions.");
                
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    // Alert explains permission requirement and settings details (Requirement 10.2, 10.3)
                    await ShowAlertAsync("Permission Required", "Location permission is required to display your current location on the map. Please enable it in device Settings > Apps > Bago Scout > Permissions.", "OK");
                    return;
                }

                // Temporarily disable button (Requirement 6.5)
                MyLocationButton.IsEnabled = false;
                MyLocationButton.BackgroundColor = Color.FromArgb("#E5E7EB");

                var userLoc = await Geolocation.Default.GetLastKnownLocationAsync();
                if (userLoc == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
                    userLoc = await Geolocation.Default.GetLocationAsync(request);
                }

                if (userLoc != null)
                {
                    _lastUserLocation = userLoc;
                    UpdateJobsDistanceDisplay(userLoc);

                    // Show user dot and fly to location
                    JobMap.UpdateUserLocation(userLoc.Latitude, userLoc.Longitude);
                    JobMap.CenterOnLocation(userLoc.Latitude, userLoc.Longitude, 15, 800);

                    await Task.Delay(850);
                }
                else
                {
                    await ShowAlertAsync("Location Unavailable", "Unable to acquire current location. Please verify GPS is enabled in your device settings.", "OK");
                }
            }
            catch (FeatureNotEnabledException)
            {
                // Alert explains disabled GPS services (Requirement 10.4)
                await ShowAlertAsync("Location Services Disabled", "GPS/Location services are turned off. Please enable them in your device settings under Settings > Location.", "OK");
            }
            catch (PermissionException)
            {
                await ShowAlertAsync("Permission Required", "Location permission was denied. Please allow location access in your device settings.", "OK");
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"An error occurred while fetching location: {ex.Message}", "OK");
            }
            finally
            {
                MyLocationButton.IsEnabled = true;
                MyLocationButton.BackgroundColor = Colors.White;
            }
        }

        private void InitializeMap(List<JobDto> jobs)
        {
            try
            {
                JobMap.Jobs = jobs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating seeker map: {ex.Message}");
            }
        }

        private void OnJobSelectedOnMap(object sender, JobDto job)
        {
            _currentSelectedJob = job;
        }

        private void OnViewDetailsClickedOnMap(object sender, JobDto job)
        {
            OpenJobDetails(job);
        }
    }
}
