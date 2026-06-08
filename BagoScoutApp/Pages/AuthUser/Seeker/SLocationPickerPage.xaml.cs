using BagoScoutApp.Services;
using System.Diagnostics;

namespace BagoScoutApp.Pages.AuthUser.Seeker
{
    public partial class SLocationPickerPage : ContentPage
    {
        private readonly SPreferencesPage? _preferencesPage;
        private string _selectedAddress = "";
        private bool _mapReady;

        // Awaitable source for the JS → C# coordinate response
        private TaskCompletionSource<(double Lat, double Lng)>? _coordTcs;

        private static readonly string MapboxToken =
            ConfigurationService.Instance.MapboxAccessToken;

        private static readonly System.Net.Http.HttpClient GeocodeClient = new();

        public SLocationPickerPage(SPreferencesPage? preferencesPage = null)
        {
            InitializeComponent();
            _preferencesPage = preferencesPage;
            MapWebView.Navigating += OnWebViewNavigating;
            _ = LoadMapAsync();
        }

        // ── HTML load ─────────────────────────────────────────────────────────

        private async Task LoadMapAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("location_picker.html");
                using var reader = new StreamReader(stream);
                var html = await reader.ReadToEndAsync();
                html = html.Replace("{{TOKEN}}", MapboxToken);
                MapWebView.Source = new HtmlWebViewSource { Html = html };
                Debug.WriteLine("[SLocationPickerPage] HTML loaded");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SLocationPickerPage] LoadMapAsync error: {ex.Message}");
            }
        }

        // ── Bridge: JS → C# ──────────────────────────────────────────────────

        private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            // Handle iframe-based signals (mauibridge://)
            if (e.Url.StartsWith("mauibridge://", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                var signal = e.Url["mauibridge://".Length..];
                Debug.WriteLine($"[SLocationPickerPage] Bridge: {signal}");
                Dispatcher.Dispatch(() => HandleSignal(signal));
                return;
            }

            // Handle https fake-URL signals (https://mauibridge.local/...)
            // Used for coordinate responses where custom scheme throws SyntaxError
            if (e.Url.StartsWith("https://mauibridge.local/", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                var encoded = e.Url["https://mauibridge.local/".Length..];
                var signal = Uri.UnescapeDataString(encoded);
                Debug.WriteLine($"[SLocationPickerPage] Bridge (nav): {signal}");
                Dispatcher.Dispatch(() => HandleSignal(signal));
            }
        }

        private void HandleSignal(string signal)
        {
            if (signal == "map_ready")
            {
                _mapReady = true;
                MapLoader.IsRunning = false;
                MapLoader.IsVisible = false;
                return;
            }

            if (!signal.StartsWith("location_selected:")) return;

            // Format: location_selected:lat,lng
            var coords = signal["location_selected:".Length..].Split(',');
            if (coords.Length < 2) return;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (!double.TryParse(coords[0], System.Globalization.NumberStyles.Any, inv, out var lat)) return;
            if (!double.TryParse(coords[1], System.Globalization.NumberStyles.Any, inv, out var lng)) return;

            // Resolve the awaitable if Confirm is waiting for coordinates
            if (_coordTcs != null && !_coordTcs.Task.IsCompleted)
            {
                _coordTcs.TrySetResult((lat, lng));
                return;
            }

            // Otherwise update the info panel (tap-to-select flow)
            ShowLocationInfo(lat, lng);
        }

        private void ShowLocationInfo(double lat, double lng)
        {
            LocationInfoBorder.IsVisible = true;
            CoordinatesLabel.Text = $"{lat:F6}, {lng:F6}";
            SelectedLocationLabel.Text = "Loading address...";
            _ = ReverseGeocodeAsync(lat, lng);
        }

        // ── Confirm: read map center from JS then proceed ─────────────────────

        private async void OnConfirmTapped(object sender, EventArgs e)
        {
            if (!_mapReady) return;

            // Request the current map center from JS; wait up to 3 s for the response
            _coordTcs = new TaskCompletionSource<(double, double)>();
            MapWebView.Eval("confirmLocation();");

            (double lat, double lng) coords;
            try
            {
                coords = await _coordTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("[SLocationPickerPage] Coordinate request timed out");
                await DisplayAlert("Error", "Could not read map position. Please try again.", "OK");
                return;
            }
            finally
            {
                _coordTcs = null;
            }

            // Reverse geocode to get a human-readable address
            SelectedLocationLabel.Text = "Loading address...";
            LocationInfoBorder.IsVisible = true;
            CoordinatesLabel.Text = $"{coords.lat:F6}, {coords.lng:F6}";

            await ReverseGeocodeAsync(coords.lat, coords.lng);

            _preferencesPage?.SetLocation(coords.lat, coords.lng, _selectedAddress);
            await Navigation.PopAsync();
        }

        // ── Reverse geocode ───────────────────────────────────────────────────

        private async Task ReverseGeocodeAsync(double lat, double lng)
        {
            try
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/" +
                          $"{lng.ToString(inv)},{lat.ToString(inv)}.json?access_token={MapboxToken}";

                var json = await GeocodeClient.GetStringAsync(url);
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                _selectedAddress =
                    doc.RootElement.TryGetProperty("features", out var features) &&
                    features.GetArrayLength() > 0 &&
                    features[0].TryGetProperty("place_name", out var name)
                        ? name.GetString() ?? $"{lat:F6}, {lng:F6}"
                        : $"{lat:F6}, {lng:F6}";

                SelectedLocationLabel.Text = _selectedAddress;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SLocationPickerPage] ReverseGeocode error: {ex.Message}");
                _selectedAddress = $"{lat:F6}, {lng:F6}";
                SelectedLocationLabel.Text = _selectedAddress;
            }
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private async void OnCancelTapped(object sender, EventArgs e)
            => await Navigation.PopAsync();

        private async void OnBackTapped(object sender, EventArgs e)
            => await Navigation.PopAsync();
    }
}
