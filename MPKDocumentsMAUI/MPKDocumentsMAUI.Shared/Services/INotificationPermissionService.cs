namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>Запрос разрешения ОС на push/локальные уведомления (Android 13+, iOS).</summary>
public interface INotificationPermissionService
{
    Task<bool> TryRequestNotificationPermissionAsync(CancellationToken ct = default);
}
