using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using MPKDocumentsMAUI.Shared.Services;

namespace MPKDocumentsMAUI.Platforms.Windows;

public sealed class WindowsDocumentFilePicker : IDocumentFilePicker
{
    public bool SupportsNativePick => true;

    private FileResult? _lastPick;

    public async Task<DocumentPickInfo?> PickDocumentAsync(CancellationToken cancellationToken = default)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Выберите документ",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = new[] { ".pdf", ".doc", ".docx" },
            }),
        });

        if (result is null)
        {
            _lastPick = null;
            return null;
        }

        _lastPick = result;

        long sizeBytes;
        var path = result.FullPath;
        if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
        {
            sizeBytes = new System.IO.FileInfo(path).Length;
        }
        else
        {
            await using var stream = await result.OpenReadAsync();
            sizeBytes = stream.CanSeek ? stream.Length : 0;
        }

        return new DocumentPickInfo(result.FileName, sizeBytes);
    }

    public async Task<byte[]?> TryReadLastNativePickAsync(int maxBytes = 25 * 1024 * 1024, CancellationToken cancellationToken = default)
    {
        if (_lastPick is null) return null;
        await using var stream = await _lastPick.OpenReadAsync();
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            total += read;
            if (total > maxBytes)
                return null;
            await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return ms.ToArray();
    }
}
