#if WINDOWS
using Microsoft.Maui.Media;
#endif
using MPKDocumentsMAUI.Shared.Services;
using SkiaSharp;
using ZXing.SkiaSharp;

namespace MPKDocumentsMAUI.Services;

public sealed class MauiQrScanService : IQrScanService
{
    public string? DecodeQrFromImageStream(Stream imageStream)
    {
        try
        {
            using var ms = new MemoryStream();
            imageStream.CopyTo(ms);
            ms.Position = 0;
            using var bitmap = SKBitmap.Decode(ms);
            if (bitmap is null) return null;
            var reader = new BarcodeReader();
            return reader.Decode(bitmap)?.Text;
        }
        catch
        {
            return null;
        }
    }

    public Task<string?> ScanQrFromCameraAsync(CancellationToken cancellationToken = default) =>
#if WINDOWS
        ScanWithSinglePhotoFallbackAsync(cancellationToken);
#else
        ScanWithLiveCameraAsync(cancellationToken);
#endif

#if WINDOWS

    private static async Task<string?> ScanWithSinglePhotoFallbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            FileResult? photo;
            if (MediaPicker.Default.IsCaptureSupported)
            {
                photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions { Title = "QR-код" })
                    .WaitAsync(cancellationToken);
            }
            else
            {
                photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Выберите фото с QR" })
                    .WaitAsync(cancellationToken);
            }

            if (photo is null) return null;
            await using var stream = await photo.OpenReadAsync();
            var reader = new BarcodeReader();
            using var sk = SKBitmap.Decode(stream);
            return sk is null ? null : reader.Decode(sk)?.Text;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

#else

    /// <remarks>Открывает нативную <see cref="QrScannerPage"/> с живой камерой (iOS/Android/MacCatalyst).</remarks>
    private static async Task<string?> ScanWithLiveCameraAsync(CancellationToken cancellationToken)
    {
        var host = ResolveHostPage();
        if (host is null)
            return null;

        var tcs = new TaskCompletionSource<string?>();
        var page = new QrScannerPage(tcs);

        await MainThread.InvokeOnMainThreadAsync(() => host.Navigation.PushModalAsync(page));

        try
        {
            return await tcs.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (host.Navigation.ModalStack.Count > 0)
                        await host.Navigation.PopModalAsync(animated: false);
                }
                catch
                {
                    /* ignore */
                }
            });
            return null;
        }
    }

    private static Page? ResolveHostPage()
    {
        var root = Application.Current?.Windows.FirstOrDefault()?.Page;
        return root switch
        {
            NavigationPage nav => nav.CurrentPage ?? nav,
            { } page => page,
            _ => null
        };
    }

#endif
}
