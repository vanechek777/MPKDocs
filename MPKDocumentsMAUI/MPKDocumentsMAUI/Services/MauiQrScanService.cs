using Microsoft.Maui.Media;
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

    public async Task<string?> ScanQrFromCameraAsync(CancellationToken cancellationToken = default)
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
            return DecodeQrFromImageStream(stream);
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
}
