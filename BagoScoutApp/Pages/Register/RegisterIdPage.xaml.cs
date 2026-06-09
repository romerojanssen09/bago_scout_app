using BagoScoutApp.Models;
using BagoScoutApp.Services;
using BagoScoutApp.Pages.Components;

namespace BagoScoutApp.Pages.Register
{
    public partial class RegisterIdPage : BasePage
    {
        readonly ApiClient _api = new();
        public RegisterIdPage()
        {
            InitializeComponent();
        }
        
        async void OnCloseTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("../../../..", false);
        }
        
        async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..", false);
        }
        
        async void OnLoginTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("../../../..", false);
        }

        async void OnCaptureSelfie(object sender, EventArgs e)
        {
            try
            {
                var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    await ShowAlertAsync("Permission Required", "Camera permission is needed to take a photo.", "OK");
                    return;
                }

                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await ShowAlertAsync("Not Supported", "Camera capture is not supported on this device.", "OK");
                    return;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    var path = Path.Combine(FileSystem.CacheDirectory, $"selfie_{DateTime.Now.Ticks}.jpg");
                    using var stream = await photo.OpenReadAsync();
                    using var fs = File.OpenWrite(path);
                    await stream.CopyToAsync(fs);
                    RegistrationState.SelfiePath = path;
                    SelfiePreview.Source = ImageSource.FromFile(path);
                    SelfiePreviewBorder.IsVisible = true;
                    SelfieErrorLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error capturing selfie: {ex.Message}");
                await ShowAlertAsync("Error", "Could not capture photo. Please try picking from gallery instead.", "OK");
            }
        }

        async void OnPickSelfie(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Photos>();
                if (status != PermissionStatus.Granted)
                {
                    await ShowAlertAsync("Permission Required", "Photo library permission is needed to pick a photo.", "OK");
                    return;
                }

                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null)
                {
                    var path = Path.Combine(FileSystem.CacheDirectory, $"selfie_{DateTime.Now.Ticks}.jpg");
                    using var stream = await photo.OpenReadAsync();
                    using var fs = File.OpenWrite(path);
                    await stream.CopyToAsync(fs);
                    RegistrationState.SelfiePath = path;
                    SelfiePreview.Source = ImageSource.FromFile(path);
                    SelfiePreviewBorder.IsVisible = true;
                    SelfieErrorLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error picking selfie: {ex.Message}");
                await ShowAlertAsync("Error", "Could not pick photo. Please try again.", "OK");
            }
        }

        async void OnCaptureId(object sender, EventArgs e)
        {
            try
            {
                var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    await ShowAlertAsync("Permission Required", "Camera permission is needed to take a photo.", "OK");
                    return;
                }

                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await ShowAlertAsync("Not Supported", "Camera capture is not supported on this device.", "OK");
                    return;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    var path = Path.Combine(FileSystem.CacheDirectory, $"id_{DateTime.Now.Ticks}.jpg");
                    using var stream = await photo.OpenReadAsync();
                    using var fs = File.OpenWrite(path);
                    await stream.CopyToAsync(fs);
                    RegistrationState.IdPath = path;
                    IdPreview.Source = ImageSource.FromFile(path);
                    IdPreviewBorder.IsVisible = true;
                    IdErrorLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error capturing ID: {ex.Message}");
                await ShowAlertAsync("Error", "Could not capture photo. Please try picking from gallery instead.", "OK");
            }
        }

        async void OnPickId(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Photos>();
                if (status != PermissionStatus.Granted)
                {
                    await ShowAlertAsync("Permission Required", "Photo library permission is needed to pick a photo.", "OK");
                    return;
                }

                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null)
                {
                    var path = Path.Combine(FileSystem.CacheDirectory, $"id_{DateTime.Now.Ticks}.jpg");
                    using var stream = await photo.OpenReadAsync();
                    using var fs = File.OpenWrite(path);
                    await stream.CopyToAsync(fs);
                    RegistrationState.IdPath = path;
                    IdPreview.Source = ImageSource.FromFile(path);
                    IdPreviewBorder.IsVisible = true;
                    IdErrorLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error picking ID: {ex.Message}");
                await ShowAlertAsync("Error", "Could not pick photo. Please try again.", "OK");
            }
        }

        async void OnUpload(object sender, EventArgs e)
        {
            // Validate both photos are provided
            bool hasSelfie = !string.IsNullOrEmpty(RegistrationState.SelfiePath) && File.Exists(RegistrationState.SelfiePath);
            bool hasId = !string.IsNullOrEmpty(RegistrationState.IdPath) && File.Exists(RegistrationState.IdPath);

            SelfieErrorLabel.IsVisible = !hasSelfie;
            IdErrorLabel.IsVisible = !hasId;

            if (!hasSelfie || !hasId)
                return;

            try
            {
                var resp = await _api.UploadPhotosAsync(
                    RegistrationState.UserId,
                    RegistrationState.SelfiePath,
                    RegistrationState.IdPath);

                if (resp.IsSuccessStatusCode)
                {
                    await Shell.Current.GoToAsync(nameof(RegisterSkillsPage), false);
                }
                else
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Upload failed: {(int)resp.StatusCode} {body}");
                    await ShowAlertAsync("Upload Failed", "Could not upload photos. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload exception: {ex.Message}");
                await ShowAlertAsync("Error", "Could not connect to server. Please check your connection and try again.", "OK");
            }
        }
    }
}
