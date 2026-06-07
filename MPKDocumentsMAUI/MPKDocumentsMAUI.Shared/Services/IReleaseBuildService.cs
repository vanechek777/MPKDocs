namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>
/// Локальная сборка установщика Windows из исходников (только dev-машина с SDK и Inno Setup).
/// </summary>
public interface IReleaseBuildService
{
    bool IsAvailable { get; }

    Task<LocalProjectVersion?> TryDetectLocalProjectAsync(CancellationToken cancellationToken = default);

    Task<ReleaseBuildResult> BuildInstallerAsync(
        string version,
        int build,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record LocalProjectVersion(string Version, int Build, string RepoRoot, string CsprojPath);

public sealed class ReleaseBuildResult
{
    public bool Ok { get; init; }
    public string? Error { get; init; }
    public byte[]? InstallerBytes { get; init; }
    public string? InstallerFileName { get; init; }
    public string? InstallerPath { get; init; }
    public string? BuiltVersion { get; init; }
    public int BuiltBuild { get; init; }

    public static ReleaseBuildResult Fail(string error) => new() { Error = error };

    public static ReleaseBuildResult Success(
        byte[] bytes,
        string fileName,
        string path,
        string version,
        int build) =>
        new()
        {
            Ok = true,
            InstallerBytes = bytes,
            InstallerFileName = fileName,
            InstallerPath = path,
            BuiltVersion = version,
            BuiltBuild = build,
        };
}
