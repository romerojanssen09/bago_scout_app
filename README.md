# BagoScout App

A .NET MAUI cross-platform mobile application for job seekers and employers.

## Features

- **Job Seekers**: Browse jobs, view on maps, apply to positions, track applications
- **Employers**: Post jobs, manage candidates, messaging system
- **Authentication**: Secure login and registration with Firebase
- **Maps Integration**: Mapbox for location-based job search
- **Push Notifications**: Firebase Cloud Messaging for real-time updates

## Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 with .NET MAUI workload
- Android SDK (for Android development)
- Xcode (for iOS development, macOS only)

### Configuration

1. Clone the repository
2. Copy `BagoScoutApp/appsettings.json.example` to `BagoScoutApp/appsettings.json`
3. Update the configuration values:
   - `Mapbox.AccessToken`: Your Mapbox API access token
   - `Api.BaseUrl`: Your backend API URL
4. Set the `MAPBOX_ACCESS_TOKEN` environment variable for Gradle builds:
   - Windows: `setx MAPBOX_ACCESS_TOKEN "your_token_here"`
   - macOS/Linux: `export MAPBOX_ACCESS_TOKEN="your_token_here"`

### Firebase Setup

1. Place your `google-services.json` (Android) in `BagoScoutApp/Platforms/Android/`
2. Place your `GoogleService-Info.plist` (iOS) in `BagoScoutApp/Platforms/iOS/`

### Build and Run

```bash
dotnet build
dotnet run --framework net8.0-android
# or
dotnet run --framework net8.0-ios
```

## Project Structure

- `Pages/` - UI pages and components
  - `AuthUser/Employer/` - Employer-specific pages
  - `AuthUser/Seeker/` - Job seeker-specific pages
  - `Register/` - Registration flow pages
  - `Components/` - Reusable UI components
- `Services/` - Business logic and API clients
- `Models/` - Data models
- `Platforms/` - Platform-specific code (Android, iOS, Windows, etc.)

## Security Notes

- Never commit `appsettings.json` with real credentials
- Keep your Firebase configuration files secure
- Use environment variables for sensitive data in CI/CD pipelines

## License

[Your License Here]
