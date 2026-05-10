using Microsoft.Maui.ApplicationModel;
using MPKDocumentsMAUI.Shared.Services;
#if IOS || MACCATALYST
using UserNotifications;
#endif

namespace MPKDocumentsMAUI.Services;

/// <summary>Разрешение на показ локальных/OS-уведомлений на устройстве.</summary>
/// <remarks>Полноценные push (новый документ, документ подписан) нужны регистрация токена на бэкенде,
/// канал Firebase Cloud Messaging или APNs/Notification Hubs и API-события на сервере — в этом репозитории только клиентское разрешение.</remarks>
public sealed class NotificationPermissionService : INotificationPermissionService
{
    public Task<bool> TryRequestNotificationPermissionAsync(CancellationToken ct = default)
    {
#if ANDROID
        return RequestAndroidAsync(ct);
#elif IOS || MACCATALYST
        return RequestAppleAsync(ct);
#else
        return Task.FromResult(true);
#endif
    }

#if ANDROID
    private static async Task<bool> RequestAndroidAsync(CancellationToken ct)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return true;
        var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        return status == PermissionStatus.Granted;
    }
#endif

#if IOS || MACCATALYST
    private static Task<bool> RequestAppleAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>();
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge,
                (granted, _) => tcs.TrySetResult(granted));
        });
        return tcs.Task.WaitAsync(ct);
    }
#endif
}
