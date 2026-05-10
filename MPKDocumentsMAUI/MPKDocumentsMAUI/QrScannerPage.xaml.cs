using ZXing.Net.Maui;

namespace MPKDocumentsMAUI;

/// <summary>Нативный полноэкранный QR-сканер с оверлеем (не системный UIScanner).</summary>
public partial class QrScannerPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _completion;
    private int _handled;

    public QrScannerPage(TaskCompletionSource<string?> completion)
    {
        InitializeComponent();
        _completion = completion;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        LoadingOverlay.IsVisible = true;

        try
        {
            CameraView.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.QrCode,
                AutoRotate = true,
                Multiple = false
            };
            CameraView.CameraLocation = CameraLocation.Rear;
        }
        catch
        {
            /* платформы без камеры оставят дефолт */
        }

        /* Нативный preview поднимается долго — не блокируем UI-поток, убираем оверлей после handler + паузы */
        _ = DismissLoaderAfterCameraReadyAsync();
    }

    private async Task DismissLoaderAfterCameraReadyAsync()
    {
        try
        {
            for (var i = 0; i < 70 && CameraView.Handler is null; i++)
                await Task.Delay(32);

            await Task.Delay(340);
        }
        catch
        {
            /* ignore */
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                LoadingOverlay.IsVisible = false;
            }
            catch
            {
                /* страница могла уже исчезнуть */
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            LoadingOverlay.IsVisible = false;
        }
        catch
        {
            /* ignore */
        }
        try
        {
            CameraView.IsTorchOn = false;
            CameraView.Handler?.DisconnectHandler();
        }
        catch
        {
            /* ignore */
        }
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (Interlocked.Exchange(ref _handled, 1) != 0)
            return;

        var raw = e.Results?.FirstOrDefault()?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            Interlocked.Exchange(ref _handled, 0);
            return;
        }

        await FinishAsync(raw);
    }

    private async void OnCloseTapped(object? sender, TappedEventArgs e) => await CancelAsync();

    private async void OnManualTapped(object? sender, TappedEventArgs e) => await CancelAsync();

    private void OnTorchTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            CameraView.IsTorchOn = !CameraView.IsTorchOn;
        }
        catch
        {
            /* ignore */
        }
    }

    private async Task FinishAsync(string value)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await CloseModalSafeAsync();
            _completion.TrySetResult(value);
        });
    }

    private async Task CancelAsync()
    {
        if (Interlocked.Exchange(ref _handled, 1) != 0)
            return;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await CloseModalSafeAsync();
            _completion.TrySetResult(null);
        });
    }

    private async Task CloseModalSafeAsync()
    {
        try
        {
            await Navigation.PopModalAsync(animated: true);
        }
        catch
        {
            try
            {
                if (Parent is NavigationPage np && np.Navigation.ModalStack.Count > 0)
                    await np.Navigation.PopModalAsync(false);
            }
            catch
            {
                /* ignore */
            }
        }
    }
}
