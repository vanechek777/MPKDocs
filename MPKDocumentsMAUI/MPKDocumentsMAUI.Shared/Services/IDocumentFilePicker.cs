namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>
/// Платформенный выбор файла для сценариев, где Blazor &lt;InputFile&gt; в WebView2 нестабилен (напр. WinUI unpackaged).
/// </summary>
public interface IDocumentFilePicker
{
    /// <summary>Если true — показывать нативную кнопку выбора вместо &lt;InputFile&gt;.</summary>
    bool SupportsNativePick { get; }

    Task<DocumentPickInfo?> PickDocumentAsync(CancellationToken cancellationToken = default);

    /// <summary>Последний выбор через нативный диалог (MAUI): прочитать байты файла. В Web — null.</summary>
    Task<byte[]?> TryReadLastNativePickAsync(int maxBytes = 25 * 1024 * 1024, CancellationToken cancellationToken = default);
}

public sealed record DocumentPickInfo(string FileName, long SizeBytes);
