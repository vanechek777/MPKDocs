#if WINDOWS
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MPKDocumentsMAUI.Platforms.Windows;

internal static class WindowsFileSaveHelper
{
    public static async Task<bool> TrySaveAsync(
        string fileName,
        byte[] data,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = fileName,
        };

        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(ext))
            picker.FileTypeChoices.Add("Файл", new List<string> { ext.ToLowerInvariant() });

        var hwnd = GetWindowHandle();
        if (hwnd == IntPtr.Zero)
            return await SaveToDownloadsFallbackAsync(fileName, data, cancellationToken);

        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        await FileIO.WriteBytesAsync(file, data);
        return true;
    }

    private static IntPtr GetWindowHandle()
    {
        var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (window?.Handler?.PlatformView is not MauiWinUIWindow winUiWindow)
            return IntPtr.Zero;
        return WindowNative.GetWindowHandle(winUiWindow);
    }

    private static async Task<bool> SaveToDownloadsFallbackAsync(string fileName, byte[] data, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        if (File.Exists(path))
        {
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            path = Path.Combine(dir, $"{stem}-{DateTime.Now:yyyyMMdd-HHmmss}{ext}");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await File.WriteAllBytesAsync(path, data, cancellationToken);
        return true;
    }
}
#endif
