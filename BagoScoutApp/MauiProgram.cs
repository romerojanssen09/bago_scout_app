using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using BagoScoutApp.Pages;
using BagoScoutApp.Pages.Register;
using BagoScoutApp.Pages.AuthUser.Employer;
using BagoScoutApp.Pages.AuthUser.Seeker;
using BagoScoutApp.Services;

#if ANDROID || IOS
using MapboxMaui;
#endif

namespace BagoScoutApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Font Awesome 7 Brands-Regular-400.otf", "FABrands");
                    fonts.AddFont("Font Awesome 7 Free-Regular-400.otf", "FARegular");
                    fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FASolid");
                });

            // Register pages for dependency injection
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<ForgotPasswordPage>();
            builder.Services.AddTransient<RegisterTypePage>();
            builder.Services.AddTransient<RegisterAccountPage>();
            builder.Services.AddTransient<RegisterVerifyPage>();
            builder.Services.AddTransient<RegisterIdPage>();
            builder.Services.AddTransient<RegisterSkillsPage>();
            
            // Register authenticated user pages
            builder.Services.AddTransient<SDashboardPage>();
            builder.Services.AddTransient<SJobsPage>();
            builder.Services.AddTransient<SApplicationsPage>();
            builder.Services.AddTransient<SMessagesPage>();
            builder.Services.AddTransient<SProfilePage>();
            
            builder.Services.AddTransient<EDashboardPage>();
            builder.Services.AddTransient<EPostingsPage>();
            builder.Services.AddTransient<ECandidatesPage>();
            builder.Services.AddTransient<EMessagesPage>();
            builder.Services.AddTransient<EProfilePage>();

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.Background = null;
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

#if ANDROID || IOS
            builder.UseMapbox(ConfigurationService.Instance.MapboxAccessToken);
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
