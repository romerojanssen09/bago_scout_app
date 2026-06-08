using BagoScoutApp.Services;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Text.Json;

namespace BagoScoutApp.Pages.AuthUser.Seeker.Components
{
    public partial class JobMapControl : ContentView
    {
        // ── Bindable properties ───────────────────────────────────────────────

        public static readonly BindableProperty JobsProperty =
            BindableProperty.Create(nameof(Jobs), typeof(List<JobDto>), typeof(JobMapControl), null,
                propertyChanged: OnJobsChanged);

        public static readonly BindableProperty SelectedJobProperty =
            BindableProperty.Create(nameof(SelectedJob), typeof(JobDto), typeof(JobMapControl), null,
                propertyChanged: OnSelectedJobChanged);

        public List<JobDto> Jobs
        {
            get => (List<JobDto>)GetValue(JobsProperty);
            set => SetValue(JobsProperty, value);
        }

        public JobDto? SelectedJob
        {
            get => (JobDto?)GetValue(SelectedJobProperty);
            set => SetValue(SelectedJobProperty, value);
        }

        // ── Events raised to SJobsPage ────────────────────────────────────────

        public event EventHandler<JobDto>? JobSelected;
        public event EventHandler<JobDto>? ViewDetailsClicked;

        // ── State ─────────────────────────────────────────────────────────────

        private bool _mapReady;
        private List<JobDto> _pendingJobs = new();
        private double _initLat = 10.5306;
        private double _initLng = 122.8428;
        private double _initZoom = 12;
        private bool _hasPendingUser;
        private double _pendingUserLat;
        private double _pendingUserLng;

        /// <summary>
        /// Call before the map is ready to set the initial camera position.
        /// If the map is already loaded, flies to the location immediately.
        /// </summary>
        public void SetInitialCenter(double lat, double lng, double zoom = 12)
        {
            _initLat = lat;
            _initLng = lng;
            _initZoom = zoom;

            if (_mapReady)
                CenterOnLocation(lat, lng, zoom, 0);
        }

        public JobMapControl()
        {
            InitializeComponent();
            MapWebView.Navigating += OnWebViewNavigating;

            // Try to use the last saved map position for the initial camera
            if (Preferences.ContainsKey("LastMapCenterLat"))
            {
                _initLat  = Preferences.Get("LastMapCenterLat", 10.5306);
                _initLng  = Preferences.Get("LastMapCenterLng", 122.8428);
                _initZoom = Preferences.Get("LastMapZoom", 12f);
            }

            _ = LoadMapHtmlAsync();
        }

        // ── HTML loading ──────────────────────────────────────────────────────

        private async Task LoadMapHtmlAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("job_map.html");
                using var reader = new StreamReader(stream);
                var html = await reader.ReadToEndAsync();

                var inv = System.Globalization.CultureInfo.InvariantCulture;
                html = html
                    .Replace("{{TOKEN}}",    ConfigurationService.Instance.MapboxAccessToken)
                    .Replace("{{INIT_LAT}}", _initLat.ToString(inv))
                    .Replace("{{INIT_LNG}}", _initLng.ToString(inv))
                    .Replace("{{INIT_ZOOM}}",_initZoom.ToString(inv));

                MapWebView.Source = new HtmlWebViewSource { Html = html };
                Debug.WriteLine($"[JobMapControl] HTML loaded — init center ({_initLat}, {_initLng}) zoom {_initZoom}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JobMapControl] LoadMapHtmlAsync error: {ex.Message}");
            }
        }

        // ── Bridge: JS → C# ──────────────────────────────────────────────────
        // JS uses an iframe src change so the main page is never navigated away.
        // MAUI's WebView on Android still fires Navigating for iframe loads.
        private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("mauibridge://", StringComparison.OrdinalIgnoreCase))
                return;

            e.Cancel = true;
            var signal = e.Url["mauibridge://".Length..];
            Debug.WriteLine($"[JobMapControl] Bridge: {signal}");

            // view_details must open the full-screen overlay — dispatch to main thread
            // but do NOT block here; the WebView Navigating callback must return fast.
            Dispatcher.Dispatch(() => HandleBridgeSignal(signal));
        }

        private void HandleBridgeSignal(string signal)
        {
            if (signal == "map_ready")
            {
                _mapReady = true;
                MapLoader.IsRunning = false;
                MapLoader.IsVisible = false;

                // Flush pending jobs
                if (_pendingJobs.Count > 0)
                {
                    PushJobsToMap(_pendingJobs);
                    _pendingJobs.Clear();
                }

                // Flush pending user location dot
                if (_hasPendingUser)
                {
                    _hasPendingUser = false;
                    var latStr = _pendingUserLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var lngStr = _pendingUserLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    MainThread.BeginInvokeOnMainThread(() =>
                        MapWebView.Eval($"updateUserLocation({latStr}, {lngStr});"));
                }
            }
            else if (signal.StartsWith("job_selected:"))
            {
                if (int.TryParse(signal["job_selected:".Length..], out var jobId))
                {
                    var job = Jobs?.FirstOrDefault(j => j.jobId == jobId);
                    if (job != null)
                    {
                        SelectedJob = job;
                        JobSelected?.Invoke(this, job);
                    }
                }
            }
            else if (signal.StartsWith("view_details:"))
            {
                if (int.TryParse(signal["view_details:".Length..], out var jobId))
                {
                    var job = Jobs?.FirstOrDefault(j => j.jobId == jobId);
                    if (job != null)
                    {
                        // Clear selection state so the popup in JS is already gone
                        SelectedJob = null;
                        ViewDetailsClicked?.Invoke(this, job);
                    }
                }
            }
            else if (signal == "popup_closed")
            {
                SelectedJob = null;
            }
        }

        // ── Bindable property callbacks ───────────────────────────────────────

        private static void OnJobsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (JobMapControl)bindable;
            var jobs = (List<JobDto>?)newValue ?? new List<JobDto>();

            if (!control._mapReady)
            {
                control._pendingJobs = jobs;
                return;
            }
            control.PushJobsToMap(jobs);
        }

        private static void OnSelectedJobChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // When C# clears selection (null), close the popup in JS too
            if (newValue == null)
            {
                var control = (JobMapControl)bindable;
                if (control._mapReady)
                    control.MapWebView.Eval("clearSelection();");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void PushJobsToMap(List<JobDto> jobs)
        {
            try
            {
                var slim = jobs
                    .Where(j => j.latitude.HasValue && j.longitude.HasValue)
                    .Select(j => new
                    {
                        j.jobId,
                        j.title,
                        j.company,
                        j.address,
                        j.salaryRange,
                        j.jobType,
                        j.latitude,
                        j.longitude
                    });

                var json = JsonSerializer.Serialize(slim);
                // Use base64 to safely pass arbitrary JSON into JS — avoids all
                // quoting/escaping issues with apostrophes, backslashes, newlines, etc.
                var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
                MapWebView.Eval($"setJobsBase64('{b64}');");
                Debug.WriteLine($"[JobMapControl] Pushed {jobs.Count} jobs to map");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JobMapControl] PushJobsToMap error: {ex.Message}");
            }
        }

        // ── Public API (called by SJobsPage) ──────────────────────────────────

        public void CenterOnLocation(double lat, double lng, double zoom = 15, int durationMs = 800)
        {
            if (!_mapReady) return;
            var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lngStr = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var zoomStr = zoom.ToString(System.Globalization.CultureInfo.InvariantCulture);
            MapWebView.Eval($"centerOn({latStr}, {lngStr}, {zoomStr}, {durationMs});");
        }

        public void EnableUserLocation(bool enable)
        {
            // No-op — user dot is handled by UpdateUserLocation
        }

        /// <summary>Places/moves the blue user-location dot on the map.</summary>
        public void UpdateUserLocation(double lat, double lng)
        {
            var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lngStr = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (_mapReady)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    MapWebView.Eval($"updateUserLocation({latStr}, {lngStr});"));
            }
            else
            {
                // Queue: will be called once map_ready fires
                _pendingUserLat = lat;
                _pendingUserLng = lng;
                _hasPendingUser = true;
            }
        }

        public void SetOfflineMode(bool offline)
        {
            if (!_mapReady) return;
            MapWebView.Eval($"showOfflineBanner({(offline ? "true" : "false")});");
        }

        /// <summary>Returns a best-effort center + zoom from the last known state (used for save/restore).</summary>
        public (Microsoft.Maui.Graphics.Point Center, float Zoom) GetCenterAndZoom()
        {
            // WebView doesn't expose map state synchronously — return stored preference values
            var lat = Preferences.Get("LastMapCenterLat", 10.5306);
            var lng = Preferences.Get("LastMapCenterLng", 122.8428);
            var zoom = Preferences.Get("LastMapZoom", 12f);
            return (new Microsoft.Maui.Graphics.Point(lat, lng), zoom);
        }

        public void DownloadOfflineRegion(double minLat, double minLng, double maxLat, double maxLng)
        {
            // Not available in WebView — no-op
            Debug.WriteLine("[JobMapControl] DownloadOfflineRegion: not supported in WebView mode");
        }

        public void ClearOfflineCache()
        {
            Debug.WriteLine("[JobMapControl] ClearOfflineCache: not supported in WebView mode");
        }

        public double[] GetCurrentBounds()
        {
            var lat = Preferences.Get("LastMapCenterLat", 10.5389);
            var lng = Preferences.Get("LastMapCenterLng", 122.8398);
            return new[] { lat - 0.1, lng - 0.1, lat + 0.1, lng + 0.1 };
        }
    }
}
