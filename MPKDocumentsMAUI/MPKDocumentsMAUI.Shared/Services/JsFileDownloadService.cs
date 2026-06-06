using Microsoft.JSInterop;

namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>Fallback для браузера / когда нет нативного сохранения.</summary>
public sealed class JsFileDownloadService(IJSRuntime js) : IFileDownloadService
{
    public async Task<bool> TrySaveFileAsync(
        string fileName,
        byte[] data,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        if (data.Length == 0)
            return false;

        var modUrl = "_content/MPKDocumentsMAUI.Shared/mpk-download.js";
        await using var mod = await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, modUrl);
        await mod.InvokeVoidAsync(
            "downloadBase64",
            cancellationToken,
            fileName,
            Convert.ToBase64String(data),
            contentType ?? "application/octet-stream");
        return true;
    }
}
