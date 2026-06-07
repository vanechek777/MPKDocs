using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed class AppReleaseDto
{
    [JsonPropertyName("configured")]
    public bool Configured { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("build")]
    public int? Build { get; set; }

    [JsonPropertyName("min_build")]
    public int MinBuild { get; set; }

    [JsonPropertyName("mandatory")]
    public bool Mandatory { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("windows_url")]
    public string? WindowsUrl { get; set; }

    [JsonPropertyName("android_url")]
    public string? AndroidUrl { get; set; }

    [JsonPropertyName("ios_url")]
    public string? IosUrl { get; set; }

    [JsonPropertyName("web_url")]
    public string? WebUrl { get; set; }
}

public sealed class AppUpdateCheckResult
{
    public bool Checked { get; init; }
    public bool UpdateAvailable { get; init; }
    public bool Mandatory { get; init; }
    public AppReleaseDto? Release { get; init; }
    public string? DownloadUrl { get; init; }
    public string? Error { get; init; }

    public static AppUpdateCheckResult Skipped { get; } = new() { Checked = false };
    public static AppUpdateCheckResult UpToDate { get; } = new() { Checked = true };
}
