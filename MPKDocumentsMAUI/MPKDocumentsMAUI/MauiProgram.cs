using Microsoft.Extensions.Logging;
using MPKDocumentsMAUI.Services;
using MPKDocumentsMAUI.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MPKDocumentsMAUI.Shared.Auth;
using MPKDocumentsMAUI.Shared.Api;

namespace MPKDocumentsMAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(_ => { });

            // Add device-specific services used by the MPKDocumentsMAUI.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<IMobileShellService, MobileShellService>();
#if WINDOWS
            builder.Services.AddSingleton<IDocumentFilePicker, Platforms.Windows.WindowsDocumentFilePicker>();
#else
            builder.Services.AddSingleton<IDocumentFilePicker, NullDocumentFilePicker>();
#endif

            builder.Services.AddMauiBlazorWebView();

            // API + auth
            // Читает базовый URL из ApiOptions по умолчанию (продакшен).
            // Локально: builder.Services.AddSingleton(new ApiOptions { BaseUrl = "http://localhost:8000" });
            // Эмулятор Android к хосту: http://10.0.2.2:8000
            builder.Services.AddSingleton(new ApiOptions());
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<IAuthTokenStore, SecureAuthTokenStore>();
            builder.Services.AddSingleton<AuthApiClient>();
            builder.Services.AddSingleton<DocumentsApiClient>();
            builder.Services.AddAuthorizationCore();
            // Important: register provider both as itself and as base type.
            builder.Services.AddSingleton<ApiAuthenticationStateProvider>();
            builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<INotificationPermissionService, NotificationPermissionService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
