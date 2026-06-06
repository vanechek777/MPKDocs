using System.Diagnostics;
using System.Net;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record ApiPingResult(string BaseUrl, bool Ok, long? LatencyMs, string? Error);

/// <summary>GET /health на произвольном базовом URL (без JWT).</summary>
public static class ApiHealthPing
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(12);

    public static async Task<ApiPingResult> PingAsync(
        HttpClient http,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var normalized = ApiEndpointStore.NormalizeUrl(baseUrl);
        if (normalized is null)
            return new ApiPingResult(baseUrl, false, null, "Некорректный URL");

        var healthUri = new Uri(new Uri(normalized + "/"), "health");
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(RequestTimeout);
            using var res = await http.GetAsync(healthUri, cts.Token);
            sw.Stop();
            if (res.IsSuccessStatusCode)
                return new ApiPingResult(normalized, true, sw.ElapsedMilliseconds, null);

            var detail = res.StatusCode == HttpStatusCode.NotFound
                ? "404 — нет /health"
                : $"{(int)res.StatusCode} {res.ReasonPhrase}";
            return new ApiPingResult(normalized, false, null, detail);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new ApiPingResult(normalized, false, null, "Таймаут");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ApiPingResult(normalized, false, null, ex.Message);
        }
    }
}
