namespace MPKDocumentsMAUI.Shared.Services;

public sealed class NullReleaseBuildService : IReleaseBuildService
{
    public bool IsAvailable => false;

    public Task<LocalProjectVersion?> TryDetectLocalProjectAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<LocalProjectVersion?>(null);

    public Task<ReleaseBuildResult> BuildInstallerAsync(
        string version,
        int build,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ReleaseBuildResult.Fail(
            "Сборщик доступен только в Windows-приложении на машине с исходниками проекта."));
}
