using BagoScoutApp.Services;
using BagoScoutApp.Pages.Components;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace BagoScoutApp.Pages.AuthUser.Seeker
{
    public partial class SPreferencesPage : BasePage
    {
        private readonly ApiClient _api = new();
        private JobPreferencesDto? _currentPreferences;
        private double? _selectedLatitude;
        private double? _selectedLongitude;
        private string _selectedLocationAddress = "";
        private bool _hasLocalChanges = false; // Flag to prevent reload on return

        // Public properties for other pages to access/update
        public double? PreferredLatitude => _selectedLatitude;
        public double? PreferredLongitude => _selectedLongitude;
        public string PreferredLocationAddress => _selectedLocationAddress;

        public void SetLocation(double lat, double lng, string address)
        {
            _selectedLatitude = lat;
            _selectedLongitude = lng;
            _selectedLocationAddress = address;
            _hasLocalChanges = true;

            LocationLabel.Text = _selectedLocationAddress;
            LocationLabel.TextColor = Color.FromArgb("#0F172A");
        }

        public void SetDistance(int distance)
        {
            _selectedDistance = $"{distance} km";
            _hasLocalChanges = true;

            DistanceLabel.Text = _selectedDistance;
            DistanceLabel.TextColor = Color.FromArgb("#0F172A");
        }

        private readonly string[] _jobTypeOptions = { "Any", "Full-time", "Part-time", "Contract", "Internship", "Freelance" };
        private readonly string[] _experienceOptions = { "Any", "Entry Level", "Mid Level", "Senior Level", "Executive" };
        private readonly string[] _distanceOptions = { "Any", "5 km", "10 km", "20 km", "50 km", "100 km" };

        private string _selectedJobType = "Any";
        private string _selectedExperience = "Any";
        private string _selectedDistance = "Any";

        public SPreferencesPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Only reload from API if no local changes were made
            if (!_hasLocalChanges)
            {
                await LoadPreferences();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private async Task LoadPreferences()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[SPreferencesPage] Loading preferences from API...");
                _currentPreferences = await _api.GetJobPreferencesAsync();
                
                System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] Preferences loaded: {_currentPreferences != null}");
                
                if (_currentPreferences != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] Location from API: {_currentPreferences.preferredLocation} ({_currentPreferences.preferredLatitude}, {_currentPreferences.preferredLongitude})");
                    
                    // Job Type
                    if (!string.IsNullOrEmpty(_currentPreferences.preferredJobType))
                    {
                        _selectedJobType = _currentPreferences.preferredJobType;
                        JobTypeLabel.Text = _selectedJobType;
                        JobTypeLabel.TextColor = Color.FromArgb("#0F172A");
                    }

                    // Job Titles
                    JobTitlesEntry.Text = _currentPreferences.preferredJobTitles;

                    // Experience Level
                    if (!string.IsNullOrEmpty(_currentPreferences.preferredExperienceLevel))
                    {
                        _selectedExperience = _currentPreferences.preferredExperienceLevel;
                        ExperienceLabel.Text = _selectedExperience;
                        ExperienceLabel.TextColor = Color.FromArgb("#0F172A");
                    }

                    // Salary
                    MinSalaryEntry.Text = _currentPreferences.minSalary?.ToString() ?? "";
                    MaxSalaryEntry.Text = _currentPreferences.maxSalary?.ToString() ?? "";

                    // Location - populate local variables from DB if available
                    if (!string.IsNullOrEmpty(_currentPreferences.preferredLocation))
                    {
                        _selectedLocationAddress = _currentPreferences.preferredLocation;
                        _selectedLatitude = _currentPreferences.preferredLatitude;
                        _selectedLongitude = _currentPreferences.preferredLongitude;
                        LocationLabel.Text = _selectedLocationAddress;
                        LocationLabel.TextColor = Color.FromArgb("#0F172A");
                        
                        System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] ✓ Location SET from DB: {_selectedLocationAddress} ({_selectedLatitude}, {_selectedLongitude})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[SPreferencesPage] ✗ No location in DB preferences");
                    }

                    // Max Distance
                    if (_currentPreferences.maxDistance.HasValue)
                    {
                        _selectedDistance = $"{_currentPreferences.maxDistance} km";
                        DistanceLabel.Text = _selectedDistance;
                        DistanceLabel.TextColor = Color.FromArgb("#0F172A");
                        System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] Distance from DB: {_selectedDistance}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SPreferencesPage] ✗ No preferences returned from API");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] Error loading preferences: {ex.Message}");
                await DisplayAlert("Error", "Failed to load preferences.", "OK");
            }
        }

        private async void OnJobTypePickerTapped(object sender, EventArgs e)
        {
            int currentIndex = Array.IndexOf(_jobTypeOptions, _selectedJobType);
            var result = await ShowSelectOptionAsync("Select Job Type", "Cancel", _jobTypeOptions, currentIndex);

            if (result != "Cancel")
            {
                _selectedJobType = result;
                JobTypeLabel.Text = _selectedJobType;
                JobTypeLabel.TextColor = Color.FromArgb("#0F172A");
            }
        }

        private async void OnExperiencePickerTapped(object sender, EventArgs e)
        {
            int currentIndex = Array.IndexOf(_experienceOptions, _selectedExperience);
            var result = await ShowSelectOptionAsync("Select Experience Level", "Cancel", _experienceOptions, currentIndex);

            if (result != "Cancel")
            {
                _selectedExperience = result;
                ExperienceLabel.Text = _selectedExperience;
                ExperienceLabel.TextColor = Color.FromArgb("#0F172A");
            }
        }

        private async void OnDistancePickerTapped(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] OnDistancePickerTapped - Local: ({_selectedLatitude}, {_selectedLongitude}), DB: ({_currentPreferences?.preferredLatitude}, {_currentPreferences?.preferredLongitude})");
            
            // Use location from local variables if set, otherwise use from current preferences loaded from DB
            double? lat = _selectedLatitude ?? _currentPreferences?.preferredLatitude;
            double? lng = _selectedLongitude ?? _currentPreferences?.preferredLongitude;
            string address = !string.IsNullOrEmpty(_selectedLocationAddress) 
                ? _selectedLocationAddress 
                : _currentPreferences?.preferredLocation ?? "";

            System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] Final location: ({lat}, {lng}), Address: {address}");

            // Check if location is available (either from local changes or database)
            if (!lat.HasValue || !lng.HasValue || lat.Value == 0 || lng.Value == 0)
            {
                System.Diagnostics.Debug.WriteLine("[SPreferencesPage] Location not available - showing alert");
                await DisplayAlert("Location Required", "Please select a preferred location first before setting maximum distance.", "OK");
                return;
            }

            // Pass current distance and location to distance picker
            int currentDistance = 50; // default
            if (_currentPreferences?.maxDistance.HasValue == true)
            {
                currentDistance = _currentPreferences.maxDistance.Value;
                System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] Using distance from DB: {currentDistance}");
            }
            else if (_selectedDistance != "Any" && _selectedDistance.Contains("km"))
            {
                int.TryParse(_selectedDistance.Replace(" km", ""), out currentDistance);
                System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] Using distance from local: {currentDistance}");
            }

            System.Diagnostics.Debug.WriteLine($"[SPreferencesPage] Opening distance picker with: ({lat.Value}, {lng.Value}), {address}, {currentDistance}km");
            
            var distancePicker = new SDistancePickerPage(
                this,
                lat.Value,
                lng.Value,
                address,
                currentDistance
            );
            await Navigation.PushAsync(distancePicker);
        }

        private async void OnLocationPickerTapped(object sender, EventArgs e)
        {
            var locationPicker = new SLocationPickerPage(this);
            await Navigation.PushAsync(locationPicker);
        }

        private async void OnSavePreferencesTapped(object sender, EventArgs e)
        {
            // Show loading
            SaveIndicator.IsVisible = true;
            SaveIndicator.IsRunning = true;
            SaveButtonLabel.Text = "Saving...";

            try
            {
                // Validate salary range
                int? minSalary = null;
                int? maxSalary = null;

                if (!string.IsNullOrWhiteSpace(MinSalaryEntry.Text))
                {
                    if (!int.TryParse(MinSalaryEntry.Text, out var minSal))
                    {
                        await DisplayAlert("Invalid Input", "Please enter a valid minimum salary.", "OK");
                        return;
                    }
                    minSalary = minSal;
                }

                if (!string.IsNullOrWhiteSpace(MaxSalaryEntry.Text))
                {
                    if (!int.TryParse(MaxSalaryEntry.Text, out var maxSal))
                    {
                        await DisplayAlert("Invalid Input", "Please enter a valid maximum salary.", "OK");
                        return;
                    }
                    maxSalary = maxSal;
                }

                if (minSalary.HasValue && maxSalary.HasValue && minSalary > maxSalary)
                {
                    await DisplayAlert("Invalid Range", "Minimum salary cannot be greater than maximum salary.", "OK");
                    return;
                }

                // Prepare preferences DTO
                var preferences = new JobPreferencesDto
                {
                    preferredJobType = _selectedJobType == "Any" ? "" : _selectedJobType,
                    preferredJobTitles = JobTitlesEntry.Text?.Trim() ?? "",
                    preferredExperienceLevel = _selectedExperience == "Any" ? "" : _selectedExperience,
                    minSalary = minSalary,
                    maxSalary = maxSalary,
                    preferredLocation = _selectedLocationAddress,
                    preferredLatitude = _selectedLatitude,
                    preferredLongitude = _selectedLongitude,
                    maxDistance = _selectedDistance == "Any" ? null : int.Parse(_selectedDistance.Replace(" km", ""))
                };

                // Save preferences
                var response = await _api.SaveJobPreferencesAsync(preferences);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Job preferences saved successfully!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to save preferences. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving preferences: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while saving preferences.", "OK");
            }
            finally
            {
                // Hide loading
                SaveIndicator.IsVisible = false;
                SaveIndicator.IsRunning = false;
                SaveButtonLabel.Text = "Save Preferences";
            }
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
