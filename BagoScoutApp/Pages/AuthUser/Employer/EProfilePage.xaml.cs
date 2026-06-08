using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BagoScoutApp.Pages.AuthUser.Employer
{
    public partial class EProfilePage : BasePage
    {
        private readonly ApiClient _api = new();
        private CompanyProfileDto? _profile;
        private readonly string[] _industryOptions = new[] { "Technology", "Healthcare", "Finance", "Education", "Retail", "Manufacturing", "Construction", "Hospitality", "Other" };
        private readonly string[] _sizeOptions = new[] { "1-10", "11-50", "51-200", "201-500", "501-1000", "1000+" };
        private string _selectedIndustry = "";
        private string _selectedSize = "";

        public EProfilePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCompanyProfile();
        }

        private async Task LoadCompanyProfile()
        {
            try
            {
                _profile = await _api.GetCompanyProfileAsync();
                if (_profile != null)
                {
                    // Populate header info
                    CompanyNameLabel.Text = string.IsNullOrEmpty(_profile.companyName) ? "My Company" : _profile.companyName;
                    CompanyContactLabel.Text = $"{_profile.firstName} {_profile.lastName}".Trim();
                    
                    // Populate company details
                    CompanyNameEntry.Text = _profile.companyName;
                    _selectedIndustry = _profile.companyIndustry ?? "";
                    CompanyIndustryLabel.Text = string.IsNullOrEmpty(_selectedIndustry) ? "Select Industry" : _selectedIndustry;
                    CompanyWebsiteEntry.Text = _profile.companyWebsite;
                    CompanyAddressEntry.Text = _profile.companyAddress;
                    CompanyDescEditor.Text = _profile.companyDescription;

                    // Setup size value
                    _selectedSize = _profile.companySize ?? "";
                    CompanySizeLabel.Text = string.IsNullOrEmpty(_selectedSize) ? "Select Size" : _selectedSize;

                    // Setup company location coordinates & map
                    var lat = _profile.companyLatitude ?? 10.5389;
                    var lng = _profile.companyLongitude ?? 122.8398;
                    if (lat == 0 && lng == 0)
                    {
                        lat = 10.5389;
                        lng = 122.8398;
                    }
                    InitializeMap(lat, lng);

                    // Populate representative contact info
                    FirstNameEntry.Text = _profile.firstName;
                    LastNameEntry.Text = _profile.lastName;
                    ContactPhoneEntry.Text = _profile.phoneNumber;
                    ContactEmailEntry.Text = _profile.email;

                    // Load Logo Image
                    if (!string.IsNullOrEmpty(_profile.companyLogoPath))
                    {
                        var path = _profile.companyLogoPath.Replace('\\', '/');
                        var logoUrl = path.StartsWith("http")
                            ? path
                            : $"{_api.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

                        CompanyLogoImage.Source = new UriImageSource
                        {
                            Uri = new Uri(logoUrl),
                            CachingEnabled = false
                        };
                        CompanyLogoImage.IsVisible = true;
                        LogoPlaceholder.IsVisible = false;
                    }
                    else
                    {
                        CompanyLogoImage.IsVisible = false;
                        LogoPlaceholder.IsVisible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading company profile: {ex.Message}");
                await ShowAlertAsync("Error", "Failed to load company profile.", "OK");
            }
        }

        private async void OnChangeLogoClicked(object sender, EventArgs e)
        {
            FileResult? logo = null;
            try
            {
                logo = await MediaPicker.Default.PickPhotoAsync();

                if (logo != null)
                {
                    var localPath = logo.FullPath;
                    var response = await _api.UploadCompanyLogoAsync(localPath);

                    if (response.IsSuccessStatusCode)
                    {
                        await ShowAlertAsync("Success", "Company logo updated successfully.", "OK");
                        await LoadCompanyProfile();
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Upload logo failed: {errorBody}");
                        await ShowAlertAsync("Upload Failed", "Failed to upload logo image.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logo selection/upload error: {ex.Message}");
                await ShowAlertAsync("Error", "An error occurred while uploading company logo.", "OK");
            }
        }

        private async void OnSaveProfileClicked(object sender, EventArgs e)
        {
            if (_profile == null) return;

            var companyName = CompanyNameEntry.Text?.Trim() ?? "";
            var industry = _selectedIndustry;
            var website = CompanyWebsiteEntry.Text?.Trim() ?? "";
            var address = CompanyAddressEntry.Text?.Trim() ?? "";
            var desc = CompanyDescEditor.Text?.Trim() ?? "";
            var size = _selectedSize;

            var firstName = FirstNameEntry.Text?.Trim() ?? "";
            var lastName = LastNameEntry.Text?.Trim() ?? "";
            var phone = ContactPhoneEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                await ShowAlertAsync("Required Fields", "Company name, first name, and last name are required.", "OK");
                return;
            }

            double.TryParse(CompanyLatitudeEntry.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double latitude);
            double.TryParse(CompanyLongitudeEntry.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double longitude);

            SaveProfileBtn.IsEnabled = false;
            SaveProfileBtn.Text = "Saving changes...";

            try
            {
                var payload = new
                {
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = phone,
                    CompanyName = companyName,
                    CompanyDescription = desc,
                    CompanyWebsite = website,
                    CompanyAddress = address,
                    CompanyLatitude = latitude,
                    CompanyLongitude = longitude,
                    CompanyIndustry = industry,
                    CompanySize = size
                };

                var response = await _api.UpdateCompanyProfileAsync(payload);

                if (response.IsSuccessStatusCode)
                {
                    Preferences.Set("UserName", $"{firstName} {lastName}".Trim());
                    Preferences.Set("CompanyName", companyName);

                    await ShowAlertAsync("Success", "Profile updated successfully.", "OK");
                    await LoadCompanyProfile();
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    await ShowAlertAsync("Error Saving", $"Failed to update company profile: {msg}", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
            }
            finally
            {
                SaveProfileBtn.IsEnabled = true;
                SaveProfileBtn.Text = "Save All Changes";
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await ShowConfirmAsync("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (answer)
            {
                await _api.LogoutAsync();
                await Shell.Current.GoToAsync("//MainPage", false);
            }
        }

        private async void OnIndustryPickerClicked(object sender, EventArgs e)
        {
            int currentIndex = Array.IndexOf(_industryOptions, _selectedIndustry);
            var result = await ShowSelectOptionAsync("Select Industry", "Cancel", _industryOptions, currentIndex);

            if (result != "Cancel")
            {
                _selectedIndustry = result;
                CompanyIndustryLabel.Text = _selectedIndustry;
            }
        }

        private async void OnSizePickerClicked(object sender, EventArgs e)
        {
            int currentIndex = Array.IndexOf(_sizeOptions, _selectedSize);
            var result = await ShowSelectOptionAsync("Select Company Size", "Cancel", _sizeOptions, currentIndex);

            if (result != "Cancel")
            {
                _selectedSize = result;
                CompanySizeLabel.Text = _selectedSize;
            }
        }

        private void InitializeMap(double lat, double lng)
        {
            try
            {
                if (lat == 0 && lng == 0)
                {
                    lat = 10.5389;
                    lng = 122.8398;
                }

                CompanyLatitudeEntry.Text = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                CompanyLongitudeEntry.Text = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var token = ConfigurationService.Instance.MapboxAccessToken;
                var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var lngStr = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='initial-scale=1,maximum-scale=1,user-scalable=no'>
<link href='https://api.mapbox.com/mapbox-gl-js/v2.15.0/mapbox-gl.css' rel='stylesheet'>
<script src='https://api.mapbox.com/mapbox-gl-js/v2.15.0/mapbox-gl.js'></script>
<style>
body {{ margin: 0; padding: 0; }}
#map {{ position: absolute; top: 0; bottom: 0; width: 100%; }}
.mapboxgl-ctrl-logo {{ display: none !important; }}
.mapboxgl-ctrl-attrib {{ display: none !important; }}
</style>
</head>
<body>
<div id='map'></div>
<script>
    mapboxgl.accessToken = '{token}';
    const map = new mapboxgl.Map({{
        container: 'map',
        style: 'mapbox://styles/mapbox/streets-v12',
        center: [{lngStr}, {latStr}],
        zoom: 13,
        attributionControl: false
    }});

    const marker = new mapboxgl.Marker({{ draggable: true }})
        .setLngLat([{lngStr}, {latStr}])
        .addTo(map);

    function onDragEnd() {{
        const lngLat = marker.getLngLat();
        window.location.href = 'invoke://coordinates?lat=' + lngLat.lat + '&lng=' + lngLat.lng;
    }}

    marker.on('dragend', onDragEnd);

    map.on('click', function(e) {{
        marker.setLngLat(e.lngLat);
        window.location.href = 'invoke://coordinates?lat=' + e.lngLat.lat + '&lng=' + e.lngLat.lng;
    }});
</script>
</body>
</html>";

                MapWebView.Source = new HtmlWebViewSource { Html = html };
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
                        CompanyLatitudeEntry.Text = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        CompanyLongitudeEntry.Text = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        
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
                            CompanyAddressEntry.Text = placeName;
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
