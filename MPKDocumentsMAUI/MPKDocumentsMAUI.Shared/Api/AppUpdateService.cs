using System.Net.Http.Json;
using System.Text.Json;
using MPKDocumentsMAUI.Shared.Services;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed class AppUpdateService : IAppUpdateService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly ApiOptions _options;
    private readonly IApiEndpointStore _endpoints;
    private readonly IFormFactor _formFactor;
    private bool _dismissed;

    public AppUpdateService(
        HttpClient http,
        ApiOptions options,
        IApiEndpointStore endpoints,
        IFormFactor formFactor)
    {
        _http = http;
        _options = options;
        _endpoints = endpoints;
        _formFactor = formFactor;
    }

    public event Action? Changed;

    public AppUpdateCheckResult? LastResult { get; private set; }

    public bool PromptVisible { get; private set; }

    public async Task<AppUpdateCheckResult> CheckForUpdatesAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (force)
                _dismissed = false;

            var release = await FetchReleaseAsync(cancellationToken);
            if (release is null || !release.Configured || release.Build is null or < 1
                || string.IsNullOrWhiteSpace(release.Version))
            {
                LastResult = AppUpdateCheckResult.UpToDate;
                PromptVisible = false;
                Changed?.Invoke();
                return LastResult;
            }

            var downloadUrl = ResolveDownloadUrl(release);
            var newer = IsRemoteNewer(release);
            var belowMinBuild = AppVersionInfo.Build < release.MinBuild;
            var behind = newer || belowMinBuild;

            // mandatory на сервере — только для тех, кто ещё не на этой версии
            if (!behind)
            {
                LastResult = AppUpdateCheckResult.UpToDate;
                PromptVisible = false;
                Changed?.Invoke();
                return LastResult;
            }

            var mandatory = belowMinBuild || release.Mandatory;

            if (!force && !mandatory && _dismissed
                && LastResult?.Release?.Build == release.Build
                && LastResult?.Release?.Version == release.Version)
            {
                return LastResult;
            }

            _dismissed = false;
            LastResult = new AppUpdateCheckResult
            {
                Checked = true,
                UpdateAvailable = true,
                Mandatory = mandatory,
                Release = release,
                DownloadUrl = downloadUrl,
            };
            PromptVisible = force || mandatory || !_dismissed;
            Changed?.Invoke();
            return LastResult;
        }
        catch (Exception ex)
        {
            LastResult = new AppUpdateCheckResult
            {
                Checked = true,
                Error = ex.Message,
            };
            Changed?.Invoke();
            return LastResult;
        }
    }

    public void DismissCurrentOffer()
    {
        _dismissed = true;
        PromptVisible = false;
        Changed?.Invoke();
    }

    private async Task<AppReleaseDto?> FetchReleaseAsync(CancellationToken cancellationToken)
    {
        AppReleaseDto? best = null;
        foreach (var baseUrl in BootstrapUrls())
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(20));

                var uri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "config/app-release");
                var dto = await _http.GetFromJsonAsync<AppReleaseDto>(uri, JsonOpts, cts.Token);
                if (dto is null || !dto.Configured)
                    continue;
                if (best is null || IsReleaseNewerThan(dto, best))
                    best = dto;
            }
            catch
            {
                /* пробуем следующий хост */
            }
        }

        return best;
    }

    private static bool IsReleaseNewerThan(AppReleaseDto candidate, AppReleaseDto current)
    {
        var candidateBuild = candidate.Build ?? 0;
        var currentBuild = current.Build ?? 0;
        if (candidateBuild != currentBuild)
            return candidateBuild > currentBuild;

        return AppVersionComparer.Compare(candidate.Version, current.Version) > 0;
    }

    private IEnumerable<string> BootstrapUrls()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string?> candidates = _endpoints.IsLoaded
            ? _endpoints.Endpoints.Select(e => e.Url)
            : [];

        candidates = candidates.Concat(
        [
            _endpoints.ActiveBaseUrl,
            _options.BaseUrl,
            ApiEndpointStore.DefaultBaseUrl,
        ]);

        foreach (var candidate in candidates)
        {
            var u = ApiEndpointStore.NormalizeUrl(candidate);
            if (u is null || !seen.Add(u))
                continue;
            yield return u;
        }
    }

    private static bool IsRemoteNewer(AppReleaseDto release)
    {
        var remoteBuild = release.Build ?? 0;
        if (remoteBuild > AppVersionInfo.Build)
            return true;
        if (remoteBuild < AppVersionInfo.Build)
            return false;

        return AppVersionComparer.Compare(release.Version, AppVersionInfo.DisplayVersion) > 0;
    }

    private string? ResolveDownloadUrl(AppReleaseDto release)
    {
        var key = DetectPlatformKey();
        return key switch
        {
            "windows" => release.WindowsUrl ?? release.WebUrl,
            "android" => release.AndroidUrl ?? release.WebUrl,
            "ios" => release.IosUrl ?? release.WebUrl,
            _ => release.WebUrl ?? release.WindowsUrl ?? release.AndroidUrl ?? release.IosUrl,
        };
    }

    private string DetectPlatformKey()
    {
        var p = _formFactor.GetPlatform();
        if (p.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "android";
        if (p.Contains("iOS", StringComparison.OrdinalIgnoreCase)
            || p.Contains("MacCatalyst", StringComparison.OrdinalIgnoreCase))
            return "ios";
        if (p.Contains("WinUI", StringComparison.OrdinalIgnoreCase)
            || p.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return "windows";
        return "web";
    }
}
