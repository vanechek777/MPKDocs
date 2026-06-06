using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using MPKDocumentsMAUI.Services;
using MPKDocumentsMAUI.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MPKDocumentsMAUI.Shared.Auth;
using MPKDocumentsMAUI.Shared.Api;
using ZXing.Net.Maui.Controls;

namespace MPKDocumentsMAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(_ => { });

            // Add device-specific services used by the MPKDocumentsMAUI.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<IMobileShellService, MobileShellService>();
            builder.Services.AddSingleton<IQrScanService, MauiQrScanService>();
            builder.Services.AddSingleton<IFileDownloadService, MauiFileDownloadService>();
#if WINDOWS
            builder.Services.AddSingleton<IDocumentFilePicker, Platforms.Windows.WindowsDocumentFilePicker>();
#else
            builder.Services.AddSingleton<IDocumentFilePicker, NullDocumentFilePicker>();
#endif

            builder.Services.AddMauiBlazorWebView();

            // API + auth: BaseUrl из Resources/Raw/appsettings.txt (JSON, ключ Api:BaseUrl). Расширение .txt — обход dotnet/maui#17078 (iOS/macOS не принимают .json в MauiAsset).
            // Эмулятор Android к хосту: http://10.0.2.2:8000
            builder.Services.AddSingleton<IApiEndpointStore>(_ => new ApiEndpointStore(LoadPackagedApiBaseUrl()));
            builder.Services.AddSingleton<ApiOptions>();
            builder.Services.AddSingleton<IApiPingLiveService, ApiPingLiveService>();
            // Явный таймаут: иначе при «молчащем» API кнопка «Отправляем…» висит бесконечно.
            builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromMinutes(3) });
            builder.Services.AddSingleton<IAuthTokenStore, SecureAuthTokenStore>();
            builder.Services.AddSingleton<AuthApiClient>();
            builder.Services.AddSingleton<DocumentsApiClient>();
            builder.Services.AddSingleton<AdminApiClient>();
            builder.Services.AddAuthorizationCore();
            // Important: register provider both as itself and as base type.
            builder.Services.AddSingleton<ApiAuthenticationStateProvider>();
            builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<IConnectionMonitorService, ConnectionMonitorService>();
            builder.Services.AddSingleton<IDocumentFeedWatchService, DocumentFeedWatchService>();
            builder.Services.AddSingleton<StaffRegisterApiClient>();
            builder.Services.AddSingleton<INotificationPermissionService, NotificationPermissionService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static string? LoadPackagedApiBaseUrl()
        {
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.txt").GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("Api", out var api) &&
                    api.TryGetProperty("BaseUrl", out var urlEl))
                {
                    return urlEl.GetString();
                }
            }
            catch
            {
                // нет файла или неверный JSON
            }

            return null;
        }
    }
}
