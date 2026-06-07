using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BagoScoutApp.Pages.AuthUser.Employer
{
    public partial class EPostingsPage : BasePage
    {
        private readonly ApiClient _api = new();
        private JobDto? _currentEditingJob;
        private bool _isDataLoading = false;

        public EPostingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPostingsList();
        }

        private async Task LoadPostingsList()
        {
            _isDataLoading = true;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                var jobs = await _api.GetEmployerJobsAsync() ?? new List<JobDto>();
                BindableLayout.SetItemsSource(PostingsLayout, jobs);
                EmptyStateLayout.IsVisible = jobs.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading job postings: {ex.Message}");
                await ShowAlertAsync("Error", "Failed to fetch job postings.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                _isDataLoading = false;
            }
        }

        private void OnCreateJobClicked(object sender, EventArgs e)
        {
            _currentEditingJob = null;
            FormTitleLabel.Text = "Create Job Posting";

            // Clear inputs
            JobTitleEntry.Text = "";
            JobCompanyEntry.Text = Preferences.Get("CompanyName", ""); // Default to CompanyName if stored
            if (string.IsNullOrEmpty(JobCompanyEntry.Text))
            {
                JobCompanyEntry.Text = Preferences.Get("UserName", "My Company");
            }
            JobAddressEntry.Text = "";
            JobTypePicker.SelectedIndex = 0;
            JobStatusPicker.SelectedIndex = 0;
            JobSalaryEntry.Text = "";
            JobExperienceEntry.Text = "";
            JobDescEditor.Text = "";
            JobReqEditor.Text = "";

            InitializeMap(10.5389, 122.8398);

            JobFormOverlay.IsVisible = true;
        }

        private void OnEditJobClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is JobDto job)
            {
                _currentEditingJob = job;
                FormTitleLabel.Text = "Edit Job Posting";

                // Populate entries
                JobTitleEntry.Text = job.title;
                JobCompanyEntry.Text = job.company;
                JobAddressEntry.Text = job.address;
                JobSalaryEntry.Text = job.salaryRange;
                JobExperienceEntry.Text = job.experienceLevel;
                JobDescEditor.Text = job.description;
                JobReqEditor.Text = job.requirements;

                // Select Picker item
                if (JobTypePicker.ItemsSource is IList<string> items)
                {
                    int index = items.IndexOf(job.jobType);
                    JobTypePicker.SelectedIndex = index >= 0 ? index : 0;
                }
                JobStatusPicker.SelectedIndex = (job.isActive == false) ? 1 : 0;

                InitializeMap(job.latitude ?? 10.5389, job.longitude ?? 122.8398);

                JobFormOverlay.IsVisible = true;
            }
        }

        private async void OnDeleteJobClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is JobDto job)
            {
                bool confirm = await ShowConfirmAsync("Delete Posting?", $"Are you sure you want to delete '{job.title}'? This action cannot be undone.", "Delete", "Cancel");
                if (!confirm) return;

                try
                {
                    var response = await _api.DeleteJobAsync(job.jobId);
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadPostingsList();
                    }
                    else
                    {
                        await ShowAlertAsync("Error", "Failed to delete job posting.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
                }
            }
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            JobFormOverlay.IsVisible = false;
            _currentEditingJob = null;
        }

        private async void OnSaveJobClicked(object sender, EventArgs e)
        {
            var title = JobTitleEntry.Text?.Trim() ?? "";
            var company = JobCompanyEntry.Text?.Trim() ?? "";
            var address = JobAddressEntry.Text?.Trim() ?? "";
            var jobType = JobTypePicker.SelectedItem?.ToString() ?? "Full-Time";
            var salary = JobSalaryEntry.Text?.Trim() ?? "";
            var expLevel = JobExperienceEntry.Text?.Trim() ?? "";
            var description = JobDescEditor.Text?.Trim() ?? "";
            var requirements = JobReqEditor.Text?.Trim() ?? "";

            double.TryParse(JobLatitudeEntry.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var latitude);
            double.TryParse(JobLongitudeEntry.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var longitude);

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(company) || string.IsNullOrEmpty(address) || string.IsNullOrEmpty(description))
            {
                await ShowAlertAsync("Required Fields", "Job title, company, address, and description are required.", "OK");
                return;
            }

            SaveJobBtn.IsEnabled = false;
            SaveJobBtn.Text = "Saving...";

            try
            {
                var payload = new
                {
                    Title = title,
                    Company = company,
                    Address = address,
                    JobType = jobType,
                    SalaryRange = salary,
                    ExperienceLevel = expLevel,
                    Description = description,
                    Requirements = requirements,
                    IsActive = JobStatusPicker.SelectedItem?.ToString() != "Inactive",
                    Latitude = latitude,
                    Longitude = longitude
                };

                HttpResponseMessage response;
                if (_currentEditingJob == null)
                {
                    response = await _api.CreateJobAsync(payload);
                }
                else
                {
                    response = await _api.UpdateJobAsync(_currentEditingJob.jobId, payload);
                }

                if (response.IsSuccessStatusCode)
                {
                    JobFormOverlay.IsVisible = false;
                    _currentEditingJob = null;
                    await LoadPostingsList();
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    await ShowAlertAsync("Error Saving", $"Failed to save job posting: {msg}", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
            }
            finally
            {
                SaveJobBtn.IsEnabled = true;
                SaveJobBtn.Text = "Save Posting";
            }
        }

        private void InitializeMap(double lat, double lng)
        {
            try
            {
                // Default coordinates if empty or zero (Bago City, Negros Occidental)
                if (lat == 0 && lng == 0)
                {
                    lat = 10.5389;
                    lng = 122.8398;
                }

                JobLatitudeEntry.Text = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                JobLongitudeEntry.Text = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var htmlTemplate = @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<title>Display a map</title>
<meta name='viewport' content='initial-scale=1,maximum-scale=1,user-scalable=no'>
<link href='https://api.mapbox.com/mapbox-gl-js/v2.15.0/mapbox-gl.css' rel='stylesheet'>
<script src='https://api.mapbox.com/mapbox-gl-js/v2.15.0/mapbox-gl.js'></script>
<style>
body { margin: 0; padding: 0; }
#map { position: absolute; top: 0; bottom: 0; width: 100%; }
.mapboxgl-ctrl-logo { display: none !important; }
.mapboxgl-ctrl-attrib { display: none !important; }
</style>
</head>
<body>
<div id='map'></div>
<script>
    mapboxgl.accessToken = '{ConfigurationService.Instance.MapboxAccessToken}';
    const map = new mapboxgl.Map({
        container: 'map',
        style: 'mapbox://styles/mapbox/streets-v11',
        center: [{lng}, {lat}],
        zoom: 13
    });

    const marker = new mapboxgl.Marker({
        draggable: true
    })
    .setLngLat([{lng}, {lat}])
    .addTo(map);

    function onDragEnd() {
        const lngLat = marker.getLngLat();
        window.location.href = 'invoke://coordinates?lat=' + lngLat.lat + '&lng=' + lngLat.lng;
    }

    marker.on('dragend', onDragEnd);
    
    map.on('click', function(e) {
        marker.setLngLat(e.lngLat);
        window.location.href = 'invoke://coordinates?lat=' + e.lngLat.lat + '&lng=' + e.lngLat.lng;
    });
</script>
</body>
</html>";
                var htmlSource = new HtmlWebViewSource();
                htmlSource.Html = htmlTemplate
                    .Replace("{lat}", lat.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Replace("{lng}", lng.ToString(System.Globalization.CultureInfo.InvariantCulture));
                MapWebView.Source = htmlSource;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing map: {ex.Message}");
            }
        }

        private async void OnMapNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("invoke://coordinates"))
            {
                e.Cancel = true;
                
                try
                {
                    var uri = new Uri(e.Url);
                    var query = uri.Query.TrimStart('?');
                    var parts = query.Split('&');
                    string? latStr = null;
                    string? lngStr = null;
                    foreach (var part in parts)
                    {
                        var kv = part.Split('=');
                        if (kv.Length == 2)
                        {
                            if (kv[0] == "lat") latStr = kv[1];
                            else if (kv[0] == "lng") lngStr = kv[1];
                        }
                    }
                    
                    if (double.TryParse(latStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                        double.TryParse(lngStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                    {
                        JobLatitudeEntry.Text = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        JobLongitudeEntry.Text = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        
                        await ReverseGeocodeAsync(lat, lng);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing coordinate navigation: {ex.Message}");
                }
            }
        }

        private async Task ReverseGeocodeAsync(double lat, double lng)
        {
            try
            {
                var token = ConfigurationService.Instance.MapboxAccessToken;
                var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var lngStr = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{lngStr},{latStr}.json?access_token={token}";
                
                using var client = new System.Net.Http.HttpClient();
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(content);
                    var features = doc.RootElement.GetProperty("features");
                    if (features.GetArrayLength() > 0)
                    {
                        var placeName = features[0].GetProperty("place_name").GetString();
                        if (!string.IsNullOrEmpty(placeName))
                        {
                            JobAddressEntry.Text = placeName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reverse geocoding error: {ex.Message}");
            }
        }
    }
}
