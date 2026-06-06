namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>Сохранение скачанного с API файла на устройстве (без передачи больших base64 в JS interop).</summary>
public interface IFileDownloadService
{
    /// <summary>Предложить пользователю сохранить файл. false — отмена или не удалось.</summary>
    Task<bool> TrySaveFileAsync(
        string fileName,
        byte[] data,
        string? contentType = null,
        CancellationToken cancellationToken = default);
}
