using System.Diagnostics;
using BagoScoutApp.Services;

namespace BagoScoutApp.Pages.AuthUser.Seeker;

public partial class SDistancePickerPage : ContentPage
{
    private readonly SPreferencesPage _preferencesPage;
    private readonly double _centerLat;
    private readonly double _centerLng;
    private int _currentDistance;
    private bool _mapReady;

    // Token from appsettings.json
private static readonly string MapboxToken = ConfigurationService.Instance.MapboxAccessToken;
    public SDistancePickerPage(SPreferencesPage preferencesPage, double lat, double lng, string address, int initialDistance)
    {
        InitializeComponent();
        _preferencesPage = preferencesPage;
        _centerLat = lat;
        _centerLng = lng;
        _currentDistance = initialDistance;

        DistanceSlider.Value = initialDistance;
        DistanceValueLabel.Text = $"{initialDistance} km";
        SubtitleLabel.Text = "Adjust the range to see jobs within this distance from your location";

        Debug.WriteLine($"[SDistancePickerPage] Init — center: ({_centerLat}, {_centerLng}), distance: {initialDistance} km");

        // Start hidden — reveal only once Mapbox signals it's fully rendered
        MapWebView.IsVisible = false;
        MapPlaceholder.IsVisible = true;
        MapPlaceholder.Text = "Loading map...";

        // JS fires mauibridge://map_ready when the map load event completes
        MapWebView.Navigating += OnMapNavigating;

        _ = LoadMapAsync();
    }

    private async Task LoadMapAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("map_distance_picker.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            html = html
                .Replace("{{TOKEN}}", MapboxToken)
                .Replace("{{LAT}}", _centerLat.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Replace("{{LNG}}", _centerLng.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Replace("{{DISTANCE}}", _currentDistance.ToString());

            MapWebView.Source = new HtmlWebViewSource { Html = html };
            Debug.WriteLine("[SDistancePickerPage] HTML loaded into WebView");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SDistancePickerPage] LoadMapAsync error: {ex.Message}");
            MapPlaceholder.Text = "Map unavailable";
        }
    }

    // JS does: window.location.href = 'mauibridge://map_ready'
    // MAUI's WebView fires Navigating for every URL change — we intercept it here.
    private void OnMapNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("mauibridge://", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true; // don't actually try to load this URL

            var signal = e.Url.Replace("mauibridge://", "", StringComparison.OrdinalIgnoreCase);
            Debug.WriteLine($"[SDistancePickerPage] Bridge signal: {signal}");

            if (signal == "map_ready")
            {
                RevealMap();
            }
            else if (signal == "map_error")
            {
                Dispatcher.Dispatch(() => MapPlaceholder.Text = "Map unavailable");
            }
        }
    }

    private void RevealMap()
    {
        if (_mapReady) return;
        _mapReady = true;
        Dispatcher.Dispatch(() =>
        {
            MapWebView.IsVisible = true;
            MapPlaceholder.IsVisible = false;
            Debug.WriteLine("[SDistancePickerPage] Map ready — WebView revealed");
        });
    }

    private void OnDistanceChanged(object? sender, ValueChangedEventArgs e)
    {
        _currentDistance = (int)e.NewValue;
        DistanceValueLabel.Text = $"{_currentDistance} km";

        if (_mapReady)
        {
            Debug.WriteLine($"[SDistancePickerPage] Updating circle → {_currentDistance} km");
            MapWebView.Eval($"updateCircle({_currentDistance});");
        }
    }

    private async void OnConfirmTapped(object sender, EventArgs e)
    {
        _preferencesPage.SetDistance(_currentDistance);
        await Navigation.PopAsync();
    }

    private async void OnCancelTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
