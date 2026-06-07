using System.Text.Json;

namespace BagoScoutApp.Services
{
    public class ConfigurationService
    {
        private static ConfigurationService? _instance;
        private AppConfiguration? _config;

        public static ConfigurationService Instance => _instance ??= new ConfigurationService();

        private ConfigurationService()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                _config = JsonSerializer.Deserialize<AppConfiguration>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                _config = new AppConfiguration();
            }
        }

        public string MapboxAccessToken => _config?.Mapbox?.AccessToken ?? string.Empty;
        public string ApiBaseUrl => _config?.Api?.BaseUrl ?? string.Empty;
    }

    public class AppConfiguration
    {
        public MapboxConfig? Mapbox { get; set; }
        public ApiConfig? Api { get; set; }
    }

    public class MapboxConfig
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    public class ApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
    }
}
