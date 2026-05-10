using MPKDocumentsMAUI.Shared.Services;

namespace MPKDocumentsMAUI.Web.Services;

public sealed class WebNotificationPermissionService : INotificationPermissionService
{
    public Task<bool> TryRequestNotificationPermissionAsync(CancellationToken ct = default) =>
        Task.FromResult(true);
}
