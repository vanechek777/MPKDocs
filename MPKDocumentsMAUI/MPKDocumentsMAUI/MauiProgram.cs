using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using MPKDocumentsMAUI.Services;
using MPKDocumentsMAUI.Shared;
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
            builder.Services.AddSingleton<IReleaseBuildService, Platforms.Windows.WindowsReleaseBuildService>();
#else
            builder.Services.AddSingleton<IDocumentFilePicker, NullDocumentFilePicker>();
            builder.Services.AddSingleton<IReleaseBuildService, NullReleaseBuildService>();
#endif

            builder.Services.AddMauiBlazorWebView();

            // API + auth: BaseUrl из Resources/Raw/appsettings.txt (JSON, ключ Api:BaseUrl). Расширение .txt — обход dotnet/maui#17078 (iOS/macOS не принимают .json в MauiAsset).
            // Эмулятор Android к хосту: http://10.0.2.2:8000
            builder.Services.AddSingleton<IApiEndpointStore>(sp =>
            {
                var store = new ApiEndpointStore(LoadPackagedApiBaseUrl());
                store.AttachHttp(sp.GetRequiredService<HttpClient>());
                return store;
            });
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
            builder.Services.AddSingleton<IApiHostSettingsUi, ApiHostSettingsUi>();
            builder.Services.AddSingleton<IConnectionMonitorService, ConnectionMonitorService>();
            builder.Services.AddSingleton<IDocumentFeedWatchService, DocumentFeedWatchService>();
            builder.Services.AddSingleton<StaffRegisterApiClient>();
            builder.Services.AddSingleton<INotificationPermissionService, NotificationPermissionService>();
            builder.Services.AddSingleton<IAppUpdateService, AppUpdateService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            ConfigureAppVersion();
            return app;
        }

        private static void ConfigureAppVersion()
        {
            try
            {
                var version = AppInfo.Current.VersionString?.Trim();
                var buildText = AppInfo.Current.BuildString?.Trim();
                if (!string.IsNullOrWhiteSpace(version)
                    && int.TryParse(buildText, out var build)
                    && build > 0)
                {
                    AppVersionInfo.Configure(version, build);
                    return;
                }
            }
            catch
            {
                /* AppInfo недоступен до полной инициализации платформы */
            }

            foreach (var assetName in new[] { "appversion.txt", "appversion.json" })
            {
                try
                {
                    using var stream = FileSystem.OpenAppPackageFileAsync(assetName).GetAwaiter().GetResult();
                    using var doc = JsonDocument.Parse(stream);
                    var version = doc.RootElement.GetProperty("version").GetString();
                    var build = doc.RootElement.GetProperty("build").GetInt32();
                    if (!string.IsNullOrWhiteSpace(version) && build > 0)
                    {
                        AppVersionInfo.Configure(version, build);
                        return;
                    }
                }
                catch
                {
                    /* пробуем следующий MauiAsset */
                }
            }
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
