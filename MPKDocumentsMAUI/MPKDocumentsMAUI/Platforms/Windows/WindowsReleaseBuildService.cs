#if WINDOWS
using System.Diagnostics;
using System.Text.Json;
using MPKDocumentsMAUI.Shared.Services;

namespace MPKDocumentsMAUI.Platforms.Windows;

public sealed class WindowsReleaseBuildService : IReleaseBuildService
{
    private const string CsprojRelative = @"MPKDocumentsMAUI\MPKDocumentsMAUI\MPKDocumentsMAUI.csproj";
    private const string BuildScriptRelative = @"installer\build-installer.ps1";

    public bool IsAvailable => TryResolveRepoRoot() is not null;

    public Task<LocalProjectVersion?> TryDetectLocalProjectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = TryResolveRepoRoot();
        if (root is null)
            return Task.FromResult<LocalProjectVersion?>(null);

        var csproj = Path.Combine(root, CsprojRelative);
        var read = CsprojVersionWriter.TryRead(csproj);
        if (read is null)
            return Task.FromResult<LocalProjectVersion?>(null);

        return Task.FromResult<LocalProjectVersion?>(
            new LocalProjectVersion(read.Value.Version, read.Value.Build, root, csproj));
    }

    public async Task<ReleaseBuildResult> BuildInstallerAsync(
        string version,
        int build,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var root = TryResolveRepoRoot();
        if (root is null)
        {
            return ReleaseBuildResult.Fail(
                "Не найден репозиторий (installer\\build-installer.ps1). " +
                "Запускайте из Visual Studio или задайте переменную MPK_REPO_ROOT.");
        }

        var csproj = Path.Combine(root, CsprojRelative);
        if (!File.Exists(csproj))
            return ReleaseBuildResult.Fail($"Не найден csproj: {csproj}");

        var script = Path.Combine(root, BuildScriptRelative);
        if (!File.Exists(script))
            return ReleaseBuildResult.Fail($"Не найден скрипт сборки: {script}");

        try
        {
            progress?.Report($"Обновление {Path.GetFileName(csproj)} → v{version} (build {build})…");
            CsprojVersionWriter.Apply(csproj, version, build);

            progress?.Report("Сборка установщика (dotnet publish + Inno Setup)…");
            progress?.Report("Это может занять 2–5 минут.");

            var exitCode = await RunPowerShellAsync(script, root, progress, cancellationToken);
            if (exitCode != 0)
                return ReleaseBuildResult.Fail($"Сборка завершилась с кодом {exitCode}. См. журнал ниже.");

            var publishDir = Path.Combine(root, @"publish\MPKDocumentsMAUI-win-x64");
            var built = TryReadPublishedVersion(publishDir);
            if (built is null)
            {
                return ReleaseBuildResult.Fail(
                    $"Сборка завершилась, но не найден {Path.Combine(publishDir, "appversion.json")}.");
            }

            if (!string.Equals(built.Value.Version, version, StringComparison.OrdinalIgnoreCase)
                || built.Value.Build != build)
            {
                return ReleaseBuildResult.Fail(
                    $"Внутри сборки v{built.Value.Version} (build {built.Value.Build}), " +
                    $"ожидалось v{version} (build {build}).");
            }

            var installerPath = Path.Combine(root, "installer", "output", $"MPKDocuments-Setup-{version}.exe");
            if (!File.Exists(installerPath))
            {
                var latest = Directory.Exists(Path.Combine(root, "installer", "output"))
                    ? new DirectoryInfo(Path.Combine(root, "installer", "output"))
                        .GetFiles("*.exe")
                        .OrderByDescending(f => f.LastWriteTimeUtc)
                        .FirstOrDefault()
                    : null;
                if (latest is null)
                    return ReleaseBuildResult.Fail($"Установщик не найден: {installerPath}");
                installerPath = latest.FullName;
            }

            progress?.Report($"В сборке: v{built.Value.Version} (build {built.Value.Build})");
            progress?.Report($"Готово: {installerPath}");
            var bytes = await File.ReadAllBytesAsync(installerPath, cancellationToken);
            var fileName = Path.GetFileName(installerPath);
            return ReleaseBuildResult.Success(bytes, fileName, installerPath, version, build);
        }
        catch (Exception ex)
        {
            return ReleaseBuildResult.Fail(ex.Message);
        }
    }

    private static string? TryResolveRepoRoot()
    {
        var env = Environment.GetEnvironmentVariable("MPK_REPO_ROOT");
        if (IsRepoRoot(env))
            return Path.GetFullPath(env!);

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 12; i++)
        {
            if (IsRepoRoot(dir))
                return Path.GetFullPath(dir);

            var parent = Directory.GetParent(dir);
            if (parent is null)
                break;
            dir = parent.FullName;
        }

        return null;
    }

    private static bool IsRepoRoot(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && File.Exists(Path.Combine(path, BuildScriptRelative));

    private static (string Version, int Build)? TryReadPublishedVersion(string publishDir)
    {
        var jsonPath = Path.Combine(publishDir, "appversion.json");
        if (!File.Exists(jsonPath))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
            var version = doc.RootElement.GetProperty("version").GetString();
            var build = doc.RootElement.GetProperty("build").GetInt32();
            if (string.IsNullOrWhiteSpace(version) || build < 1)
                return null;
            return (version, build);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<int> RunPowerShellAsync(
        string scriptPath,
        string workingDirectory,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                progress?.Report(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                progress?.Report("ERR: " + e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
#endif
