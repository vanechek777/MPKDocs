namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>Заглушка: всегда используем Blazor &lt;InputFile&gt;.</summary>
public sealed class NullDocumentFilePicker : IDocumentFilePicker
{
    public bool SupportsNativePick => false;

    public Task<DocumentPickInfo?> PickDocumentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<DocumentPickInfo?>(null);

    public Task<byte[]?> TryReadLastNativePickAsync(int maxBytes = 25 * 1024 * 1024, CancellationToken cancellationToken = default) =>
        Task.FromResult<byte[]?>(null);
}
