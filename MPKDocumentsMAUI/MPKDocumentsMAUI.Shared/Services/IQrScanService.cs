namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>Распознавание QR (камера / фото) на устройстве; на веб — заглушка.</summary>
public interface IQrScanService
{
    /// <summary>Открыть камеру и вернуть сырой текст QR или null.</summary>
    Task<string?> ScanQrFromCameraAsync(CancellationToken cancellationToken = default);

    /// <summary>Декодировать QR из изображения (скрин, фото из галереи).</summary>
    string? DecodeQrFromImageStream(Stream imageStream);
}
