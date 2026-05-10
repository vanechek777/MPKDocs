namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>
/// Передача файла, сброшенного нативным жестом MAUI (Windows), в Blazor без WebView2 drag&amp;drop.
/// </summary>
public static class HostFileDropHub
{
    public static Action<string, long>? Callback { get; set; }

    public static void Raise(string path, long sizeBytes) => Callback?.Invoke(path, sizeBytes);
}
