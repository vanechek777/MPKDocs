using System.Text;
using MPKDocumentsMAUI.Shared.Services;

namespace MPKDocumentsMAUI.Services;

/// <summary>Нативное сохранение файла — обход сбоев WebView2/WinUI при JS showSaveFilePicker.</summary>
public sealed class MauiFileDownloadService : IFileDownloadService
{
    public async Task<bool> TrySaveFileAsync(
        string fileName,
        byte[] data,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        if (data.Length == 0)
            return false;

        var safeName = SanitizeFileName(fileName);

#if WINDOWS
        return await Platforms.Windows.WindowsFileSaveHelper.TrySaveAsync(safeName, data, contentType, cancellationToken);
#else
        return await SaveViaShareAsync(safeName, data, contentType, cancellationToken);
#endif
    }

    private static async Task<bool> SaveViaShareAsync(
        string fileName,
        byte[] data,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllBytesAsync(path, data, cancellationToken);
        await Share.Default.RequestAsync(new ShareFileRequest("Сохранить файл", new ShareFile(path, contentType ?? "application/octet-stream")));
        return true;
    }

    internal static string SanitizeFileName(string fileName)
    {
        var name = (fileName ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return "download";

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        return sb.ToString();
    }
}
