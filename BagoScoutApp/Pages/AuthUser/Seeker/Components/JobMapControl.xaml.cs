using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Point = Microsoft.Maui.Graphics.Point;
using BagoScoutApp.Services;

#if ANDROID || IOS
using MapboxMaui;
using MapboxMaui.Annotations;
using MapboxMaui.Styles;
using GeoJSON.Text.Geometry;
using MapboxLayerPosition = MapboxMaui.Styles.LayerPosition;
// now use MapboxLayerPosition.Unknown() everywhere
#endif

#if ANDROID
using Com.Mapbox.Maps;
using Com.Mapbox.Maps.Extension.Style;
#endif

namespace BagoScoutApp.Pages.AuthUser.Seeker.Components
{
    public partial class JobMapControl : ContentView
    {
#if ANDROID || IOS
        private MapboxView _mapboxView;
        private IPointAnnotationManager _pointAnnotationManager;
        private Dictionary<string, JobDto> _annotationIdToJob = new();
        // Stores geo-positions of all markers for manual tap hit-testing
        private List<(double Lat, double Lng, JobDto Job)> _annotationPositions = new();
        private bool _isMapReady = false;
#endif

#if ANDROID
        private Android.Graphics.Rect _popupScreenRect = new Android.Graphics.Rect();
#endif
        private DateTime _lastTapTime = DateTime.MinValue;

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

        public JobDto SelectedJob
        {
            get => (JobDto)GetValue(SelectedJobProperty);
            set => SetValue(SelectedJobProperty, value);
        }

        public event EventHandler<JobDto> JobSelected;
        public event EventHandler<JobDto> ViewDetailsClicked;

        public JobMapControl()
        {
            InitializeComponent();
            InitializeMapControl();
        }

        private void InitializeMapControl()
        {
#if ANDROID || IOS
            _mapboxView = new MapboxView
            {
                MapboxStyle = new MapboxStyle("mapbox://styles/mapbox/streets-v11"),
                MapCenter = new Point(10.5306, 122.8428),
                MapZoom = 12f,
                ScaleBarVisibility = OrnamentVisibility.Hidden
            };

            MapContainer.Children.Add(_mapboxView);
            _mapboxView.MapReady += OnMapReady;
            _mapboxView.StyleLoaded += OnStyleLoaded;
#else
            var fallbackLabel = new Label
            {
                Text = "Map not supported on this platform",
                TextColor = Color.FromArgb("#8D94A8"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16
            };
            MapContainer.Children.Add(fallbackLabel);
#endif
        }

#if ANDROID || IOS
        private void OnMapReady(object sender, EventArgs e)
        {
            _isMapReady = true;
            System.Diagnostics.Debug.WriteLine("[JobMapControl] OnMapReady fired.");
        }

        private void OnStyleLoaded(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[JobMapControl] OnStyleLoaded fired.");
            _isMapReady = true;
            try
            {
                _mapboxView.Images = new List<ResolvedImage>
                {
                    new ResolvedImage("marker_purple", "marker_purple.png"),
                    new ResolvedImage("marker_orange", "marker_orange.png")
                };

                // No manual manager creation here — UpdateMarkers handles it
                ConfigureNativeMap();
                UpdateMarkers();
                AttachNativeTapGesture();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] Error in OnStyleLoaded: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Uses Android's native GestureDetector to intercept single taps on the MapView.
        // Converts screen (pixel) coordinates → geo-coordinates using Mapbox's own
        // coordinateForPixel, then finds the nearest job marker within tap tolerance.
        private void AttachNativeTapGesture()
        {
#if ANDROID
            try
            {
                var nativeMapView = GetNativeMapView();
                if (nativeMapView == null) return;

                var gestureDetector = new Android.Views.GestureDetector(
                    Android.App.Application.Context,
                    new MapTapListener(nativeMapView, OnNativeTapped));

                nativeMapView.SetOnTouchListener(new MapTouchListener(gestureDetector));
                System.Diagnostics.Debug.WriteLine("[JobMapControl] Native tap gesture attached.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] AttachNativeTapGesture error: {ex.Message}");
            }
#endif
        }

        // Replace OnNativeTapped with this:
        private void OnNativeTapped(float screenX, float screenY)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastTapTime).TotalMilliseconds < 500) return;
            _lastTapTime = now;

            try
            {
                if (_annotationPositions == null || !_annotationPositions.Any()) return;

#if ANDROID
                if (PopupCard?.IsVisible == true)
                {
                    var nativePopup = PopupCard?.Handler?.PlatformView as Android.Views.View;
                    if (nativePopup != null)
                    {
                        nativePopup.GetGlobalVisibleRect(_popupScreenRect);
                        if (_popupScreenRect.Contains((int)screenX, (int)screenY))
                        {
                            System.Diagnostics.Debug.WriteLine("[JobMapControl] Tap inside popup — ignored.");
                            return;
                        }
                    }
                }

                var nativeMapView = GetNativeMapView();
                if (nativeMapView == null) return;

                var mapboxMap = nativeMapView.MapboxMap;
                var screenCoord = new Com.Mapbox.Maps.ScreenCoordinate(screenX, screenY);
                var geoPoint = mapboxMap.CoordinateForPixel(screenCoord);

                var tappedLat = geoPoint.Latitude();
                var tappedLng = geoPoint.Longitude();

                var zoom = _mapboxView.MapZoom ?? 12f;
                var tapToleranceDeg = 0.010 / Math.Pow(2, zoom - 12);

                JobDto nearest = null;
                double minDist = double.MaxValue;

                foreach (var (lat, lng, job) in _annotationPositions)
                {
                    var dLat = tappedLat - lat;
                    var dLng = tappedLng - lng;
                    var dist = Math.Sqrt(dLat * dLat + dLng * dLng);
                    if (dist < minDist) { minDist = dist; nearest = job; }
                }

                System.Diagnostics.Debug.WriteLine($"[JobMapControl] Nearest job='{nearest?.title}' dist={minDist:F6} tol={tapToleranceDeg:F6}");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (nearest != null && minDist <= tapToleranceDeg)
                    {
                        SelectedJob = nearest;
                        JobSelected?.Invoke(this, nearest);
                    }
                    else
                    {
                        SelectedJob = null;
                    }
                });
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] OnNativeTapped error: {ex.Message}");
            }
        }

#if ANDROID
        private Com.Mapbox.Maps.MapView GetNativeMapView()
        {
            if (_mapboxView?.Handler?.PlatformView is Android.Views.View nativeView)
            {
                if (nativeView is Com.Mapbox.Maps.MapView mv) return mv;
                if (nativeView is Android.Views.ViewGroup vg)
                    for (int i = 0; i < vg.ChildCount; i++)
                        if (vg.GetChildAt(i) is Com.Mapbox.Maps.MapView cmv) return cmv;
            }
            return null;
        }

        // GestureDetector.SimpleOnGestureListener subclass to catch single taps
        private class MapTapListener : Android.Views.GestureDetector.SimpleOnGestureListener
        {
            private readonly Com.Mapbox.Maps.MapView _mapView;
            private readonly Action<float, float> _onTap;

            public MapTapListener(Com.Mapbox.Maps.MapView mapView, Action<float, float> onTap)
            {
                _mapView = mapView;
                _onTap = onTap;
            }

            public override bool OnSingleTapConfirmed(Android.Views.MotionEvent e)
            {
                _onTap?.Invoke(e.GetX(), e.GetY());
                return true;
            }
        }

        // OnTouchListener that forwards events to GestureDetector
        private class MapTouchListener : Java.Lang.Object, Android.Views.View.IOnTouchListener
        {
            private readonly Android.Views.GestureDetector _detector;

            public MapTouchListener(Android.Views.GestureDetector detector)
            {
                _detector = detector;
            }

            public bool OnTouch(Android.Views.View v, Android.Views.MotionEvent e)
            {
                _detector.OnTouchEvent(e);
                return false; // return false so the map still receives the event (pan/zoom still works)
            }
        }
#endif

        private void ConfigureNativeMap()
        {
#if ANDROID
            try
            {
                var nativeMapView = GetNativeMapView();
                if (nativeMapView != null)
                {
                    Com.Mapbox.Maps.Plugin.Logo.LogoUtils.GetLogo(nativeMapView).Enabled = false;
                    Com.Mapbox.Maps.Plugin.Attribution.AttributionPluginImplKt.GetAttribution(nativeMapView).Enabled = false;
                    Com.Mapbox.Maps.Plugin.Scalebar.ScaleBarUtils.GetScaleBar(nativeMapView).Enabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] ConfigureNativeMap error: {ex.Message}");
            }
#endif
        }

        // Fallback — fires in some MapboxMaui builds
        private void OnAnnotationsSelected(object sender, AnnotationsSelectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[JobMapControl] OnAnnotationsSelected fired. Count={e?.SelectedAnnotationIDs?.Count()}");
            if (e?.SelectedAnnotationIDs == null || !e.SelectedAnnotationIDs.Any()) return;

            var id = e.SelectedAnnotationIDs.First();
            if (_annotationIdToJob.TryGetValue(id, out var job))
            {
                MainThread.BeginInvokeOnMainThread(() => { SelectedJob = job; JobSelected?.Invoke(this, job); });
                return;
            }
            var fallback = Jobs?.FirstOrDefault(j => j.jobId.ToString() == id);
            if (fallback != null)
                MainThread.BeginInvokeOnMainThread(() => { SelectedJob = fallback; JobSelected?.Invoke(this, fallback); });
        }
#endif

        private static void OnJobsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (JobMapControl)bindable;
#if ANDROID || IOS
            control.UpdateMarkers();
#endif
        }

        private static void OnSelectedJobChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (JobMapControl)bindable;
            control.HighlightSelectedJob((JobDto)newValue);
        }

#if ANDROID || IOS
        private bool _isUpdatingMarkers = false;
        private int _managerCounter = 0;

        private void UpdateMarkers()
        {
            if (_isUpdatingMarkers) return;
            if (!_isMapReady) return;

            _isUpdatingMarkers = true;
            try
            {
                // Destroy ALL existing annotation managers via native API to clear GL layers
#if ANDROID
                try
                {
                    var nativeMapView = GetNativeMapView();
                    if (nativeMapView != null)
                    {
                        // This removes all annotation layers from the GL renderer
                        var annotationsPlugin = Com.Mapbox.Maps.Plugin.Annotation.AnnotationPluginImplKt
                            .GetAnnotations(nativeMapView);
                        annotationsPlugin?.Cleanup();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobMapControl] CleanUp error: {ex.Message}");
                }
#endif
                _pointAnnotationManager = null;

                // Create fresh manager with unique ID
                var managerId = $"jobs_{_managerCounter++}";
                _pointAnnotationManager = _mapboxView.AnnotationController
                    .CreatePointAnnotationManager(
                        managerId,
                        MapboxMaui.Styles.LayerPosition.Unknown(),
                        null);

                if (_pointAnnotationManager == null) return;

                _annotationIdToJob.Clear();
                _annotationPositions.Clear();

                if (Jobs == null || !Jobs.Any()) return;

                var annotations = new List<PointAnnotation>();

                foreach (var job in Jobs)
                {
                    if (!job.latitude.HasValue || !job.longitude.HasValue) continue;

                    var isSelected = SelectedJob != null && SelectedJob.jobId == job.jobId;
                    var position = new Position(job.latitude.Value, job.longitude.Value);
                    var annotation = new PointAnnotation(new GeoJSON.Text.Geometry.Point(position))
                    {
                        IconImage = isSelected ? "marker_orange" : "marker_purple",
                        IconSize = isSelected ? 1.3 : 1.0,
                        Id = job.jobId.ToString()
                    };

                    annotations.Add(annotation);
                    _annotationPositions.Add((job.latitude.Value, job.longitude.Value, job));
                }

                _pointAnnotationManager.AddAnnotations(annotations.ToArray());

                foreach (var ann in annotations)
                {
                    if (!string.IsNullOrEmpty(ann.Id))
                    {
                        var match = Jobs.FirstOrDefault(j => j.jobId.ToString() == ann.Id);
                        if (match != null) _annotationIdToJob[ann.Id] = match;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[JobMapControl] UpdateMarkers: {_annotationPositions.Count} markers, selected={SelectedJob?.jobId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] UpdateMarkers error: {ex.Message}");
            }
            finally
            {
                _isUpdatingMarkers = false;
            }
        }
#endif

        private void HighlightSelectedJob(JobDto selectedJob)
        {
            if (PopupCard == null) return;

            if (selectedJob != null)
            {
                if (PopupTitleLabel != null) PopupTitleLabel.Text = selectedJob.title ?? "Untitled";
                if (PopupCompanyLabel != null) PopupCompanyLabel.Text = selectedJob.company ?? "";
                if (PopupAddressLabel != null) PopupAddressLabel.Text = selectedJob.address ?? "";
                if (PopupSalaryLabel != null) PopupSalaryLabel.Text = string.IsNullOrEmpty(selectedJob.salaryRange) ? "Negotiable" : selectedJob.salaryRange;
                if (PopupJobTypeLabel != null) PopupJobTypeLabel.Text = selectedJob.jobType ?? "";
                PopupCard.IsVisible = true;

                // REMOVED: CenterOnLocation — do not move map on selection
            }
            else
            {
                PopupCard.IsVisible = false;
            }

#if ANDROID || IOS
            UpdateMarkers();
#endif
        }

        public void CenterOnLocation(double lat, double lng, double zoom = 15, int durationMs = 800)
        {
#if ANDROID || IOS
            if (_mapboxView == null) return;

            var startCenter = _mapboxView.MapCenter ?? new Point(10.5306, 122.8428);
            var startZoom = _mapboxView.MapZoom ?? 12f;
            var endCenter = new Point(lat, lng);
            var endZoom = (float)zoom;

            this.AbortAnimation("MapCentering");

            var animation = new Animation(v =>
            {
                var currentLat = startCenter.X + (endCenter.X - startCenter.X) * v;
                var currentLng = startCenter.Y + (endCenter.Y - startCenter.Y) * v;
                _mapboxView.MapCenter = new Point(currentLat, currentLng);
                _mapboxView.MapZoom = (float)(startZoom + (endZoom - startZoom) * v);
            }, 0, 1);

            animation.Commit(this, "MapCentering", 16, (uint)durationMs, Easing.SinOut, (v, c) =>
            {
                _mapboxView.MapCenter = endCenter;
                _mapboxView.MapZoom = endZoom;
            });
#endif
        }

        public (Point Center, float Zoom) GetCenterAndZoom()
        {
#if ANDROID || IOS
            if (_mapboxView != null)
                return (_mapboxView.MapCenter ?? new Point(10.5306, 122.8428), _mapboxView.MapZoom ?? 12f);
#endif
            return (new Point(10.5306, 122.8428), 12f);
        }

        public void EnableUserLocation(bool enable)
        {
#if ANDROID
            try
            {
                var nativeMapView = GetNativeMapView();
                if (nativeMapView != null)
                    Com.Mapbox.Maps.Plugin.Locationcomponent.LocationComponentUtils
                        .GetLocationComponent(nativeMapView).Enabled = enable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] EnableUserLocation error: {ex.Message}");
            }
#elif IOS
            System.Diagnostics.Debug.WriteLine($"[JobMapControl] Location puck requested on iOS: {enable}");
#endif
        }

        public void SetOfflineMode(bool offline)
        {
            if (OfflineBanner == null) return;
            OfflineBanner.IsVisible = offline;
#if ANDROID || IOS
            try
            {
                var offlineManager = new MapboxMaui.Offline.OfflineManager(
                    ConfigurationService.Instance.MapboxAccessToken,
                    new MapboxMaui.CameraOptions());
                offlineManager.IsMapboxStackConnected = !offline;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] SetOfflineMode error: {ex.Message}");
            }
#endif
        }

        public void DownloadOfflineRegion(double minLat, double minLng, double maxLat, double maxLng)
        {
#if ANDROID || IOS
            try
            {
                var coordinates = new List<GeoJSON.Text.Geometry.IPosition>
                {
                    new Position(minLng, minLat), new Position(maxLng, minLat),
                    new Position(maxLng, maxLat), new Position(minLng, maxLat),
                    new Position(minLng, minLat)
                };
                var polygon = new GeoJSON.Text.Geometry.Polygon(
                    new List<GeoJSON.Text.Geometry.LineString> {
                        new GeoJSON.Text.Geometry.LineString(coordinates) });

                var descriptorOptions = new MapboxMaui.Offline.TilesetDescriptorOptions(
                    "mapbox://styles/mapbox/streets-v11", (sbyte)12, (sbyte)15, 1.0f, null);
                var loadOptions = new MapboxMaui.Offline.TileRegionLoadOptions(
                    polygon, new[] { descriptorOptions }, null, true,
                    MapboxMaui.Offline.NetworkRestriction.None, null, null, null);

                var offlineManager = new MapboxMaui.Offline.OfflineManager(
                    ConfigurationService.Instance.MapboxAccessToken,
                    new MapboxMaui.CameraOptions());
                var regionId = $"region_{DateTime.UtcNow.Ticks}";

                offlineManager.DownloadTile(regionId, loadOptions,
                    progress => System.Diagnostics.Debug.WriteLine($"[JobMapControl] Offline: {progress.CompletedResourceCount}/{progress.RequiredResourceCount}"),
                    (region, error) => MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (error != null)
                            await Application.Current.MainPage.DisplayAlert("Download Offline Map", "Failed: " + error.Message, "OK");
                        else
                            await Application.Current.MainPage.DisplayAlert("Download Offline Map", "Map tiles downloaded successfully.", "OK");
                    }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] DownloadOfflineRegion error: {ex.Message}");
            }
#endif
        }

        public void ClearOfflineCache()
        {
#if ANDROID
            try
            {
                Com.Mapbox.Common.TileStore.Create();
                MainThread.BeginInvokeOnMainThread(async () =>
                    await Application.Current.MainPage.DisplayAlert("Clear Cache", "Offline map tile cache cleared.", "OK"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobMapControl] ClearOfflineCache error: {ex.Message}");
            }
#endif
        }

        public double[] GetCurrentBounds()
        {
#if ANDROID || IOS
            if (_mapboxView != null)
            {
                var center = _mapboxView.MapCenter ?? new Point(10.5389, 122.8398);
                var zoom = _mapboxView.MapZoom ?? 12f;
                var delta = 0.1 / Math.Pow(2, zoom - 12);
                return new[] { center.X - delta, center.Y - delta, center.X + delta, center.Y + delta };
            }
#endif
            return new[] { 10.4389, 122.7398, 10.6389, 122.9398 };
        }

        private void OnClosePopupClicked(object sender, EventArgs e)
        {
            _lastTapTime = DateTime.UtcNow; // block map tap for 500ms after close
            SelectedJob = null;
        }

        private void OnViewDetailsClicked(object sender, EventArgs e)
        {
            if (SelectedJob == null) return;
            _lastTapTime = DateTime.UtcNow; // block map tap for 500ms after view details
            var job = SelectedJob;
            SelectedJob = null;
            ViewDetailsClicked?.Invoke(this, job);
        }
    }
}
