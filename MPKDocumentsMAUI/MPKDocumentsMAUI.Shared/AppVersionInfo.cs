using System.Reflection;
using System.Text.Json;

namespace MPKDocumentsMAUI.Shared;

public static class AppVersionInfo
{
    private static string? _configuredDisplay;
    private static int? _configuredBuild;
    private static (string Display, int Build)? _resolved;

    public static string DisplayVersion => Get().Display;
    public static int Build => Get().Build;

    public static string FullLabel => $"{DisplayVersion} ({Build})";

    /// <summary>Вызывается из MAUI при старте (AppInfo — надёжный источник на устройстве).</summary>
    public static void Configure(string displayVersion, int build)
    {
        if (string.IsNullOrWhiteSpace(displayVersion) || build < 1)
            return;

        _configuredDisplay = displayVersion.Trim();
        _configuredBuild = build;
        _resolved = null;
    }

    private static (string Display, int Build) Get()
    {
        if (_configuredDisplay is not null && _configuredBuild is { } cb)
            return (_configuredDisplay, cb);

        _resolved ??= Resolve();
        return _resolved.Value;
    }

    private static (string Display, int Build) Resolve()
    {
        if (TryReadPackagedVersion(out var display, out var build))
            return (display, build);

        var asm = ResolveMainAssembly();
        return (ReadDisplayVersion(asm), ReadBuild(asm));
    }

    private static Assembly ResolveMainAssembly()
    {
        var entry = Assembly.GetEntryAssembly();
        if (entry is not null && IsMainAppAssembly(entry))
            return entry;

        var fromDomain = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(IsMainAppAssembly);
        if (fromDomain is not null)
            return fromDomain;

        return typeof(AppVersionInfo).Assembly;
    }

    private static bool IsMainAppAssembly(Assembly asm)
    {
        var name = asm.GetName().Name ?? "";
        return name.Equals("MPKDocumentsMAUI", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadPackagedVersion(out string display, out int build)
    {
        display = "";
        build = 0;

        foreach (var path in GetVersionFileCandidates())
        {
            if (TryParseVersionFile(path, out display, out build))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> GetVersionFileCandidates()
    {
        var baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "appversion.json");
        yield return Path.Combine(baseDir, "appversion.txt");
        yield return Path.Combine(baseDir, "Resources", "Raw", "appversion.json");
        yield return Path.Combine(baseDir, "Resources", "Raw", "appversion.txt");
    }

    private static bool TryParseVersionFile(string path, out string display, out int build)
    {
        display = "";
        build = 0;
        try
        {
            if (!File.Exists(path))
                return false;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            display = root.GetProperty("version").GetString() ?? "";
            build = root.TryGetProperty("build", out var b) ? b.GetInt32() : 0;
            return !string.IsNullOrWhiteSpace(display) && build > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string ReadDisplayVersion(Assembly asm)
    {
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plus = info.IndexOf('+');
            return plus >= 0 ? info[..plus] : info;
        }

        var v = asm.GetName().Version;
        return v is null ? "1.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
    }

    private static int ReadBuild(Assembly asm)
    {
        var meta = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, "MpkBuild", StringComparison.OrdinalIgnoreCase))
            ?.Value;
        if (int.TryParse(meta, out var fromMeta) && fromMeta > 0)
            return fromMeta;

        var v = asm.GetName().Version;
        return Math.Max(1, v?.Build ?? 1);
    }
}
