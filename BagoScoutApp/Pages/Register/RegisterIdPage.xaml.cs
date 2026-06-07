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
            if (MediaPicker.Default.IsCaptureSupported)
            {
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
                }
            }
        }

        async void OnPickSelfie(object sender, EventArgs e)
        {
            try
            {
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error picking selfie: {ex.Message}");
            }
        }

        async void OnCaptureId(object sender, EventArgs e)
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
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
                }
            }
        }

        async void OnPickId(object sender, EventArgs e)
        {
            try
            {
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error picking ID: {ex.Message}");
            }
        }

        async void OnUpload(object sender, EventArgs e)
        {
            var resp = await _api.UploadPhotosAsync(RegistrationState.UserId, RegistrationState.SelfiePath, RegistrationState.IdPath);
            if (resp.IsSuccessStatusCode)
            {
                await Shell.Current.GoToAsync(nameof(RegisterSkillsPage), false);
            }
            else
            {
                await ShowAlertAsync("Error", "Upload failed.", "OK");
            }
        }
    }
}
