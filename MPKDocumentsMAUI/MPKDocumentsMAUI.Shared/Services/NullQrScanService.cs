namespace MPKDocumentsMAUI.Shared.Services;

public sealed class NullQrScanService : IQrScanService
{
    public Task<string?> ScanQrFromCameraAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public string? DecodeQrFromImageStream(Stream imageStream) => null;
}
