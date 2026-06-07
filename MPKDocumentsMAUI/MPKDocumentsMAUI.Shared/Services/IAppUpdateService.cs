using MPKDocumentsMAUI.Shared.Api;

namespace MPKDocumentsMAUI.Shared.Services;

public interface IAppUpdateService
{
    event Action? Changed;

    AppUpdateCheckResult? LastResult { get; }

    bool PromptVisible { get; }

    Task<AppUpdateCheckResult> CheckForUpdatesAsync(bool force = false, CancellationToken cancellationToken = default);

    void DismissCurrentOffer();
}
