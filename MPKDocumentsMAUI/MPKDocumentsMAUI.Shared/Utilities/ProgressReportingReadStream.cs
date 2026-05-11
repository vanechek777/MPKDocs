namespace MPKDocumentsMAUI.Shared.Utilities;

/// <summary>
/// Оборачивает поток чтения и сообщает о количестве прочитанных байт (для прогресса HTTP upload).
/// Чтение со стороны HttpClient обычно последовательное — счётчики без блокировок.
/// </summary>
public sealed class ProgressReportingReadStream : Stream
{
    private readonly Stream _inner;
    private readonly long _total;
    private readonly IProgress<(long bytesRead, long total)>? _progress;
    private readonly int _reportEveryBytes;
    private long _read;
    private int _sinceLastReport;

    public ProgressReportingReadStream(
        Stream inner,
        long totalLength,
        IProgress<(long bytesRead, long total)>? progress,
        int reportEveryBytes = 64 * 1024)
    {
        _inner = inner;
        _total = totalLength;
        _progress = progress;
        _reportEveryBytes = Math.Max(4096, reportEveryBytes);
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => _read;
        set => throw new NotSupportedException();
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = _inner.Read(buffer, offset, count);
        AfterRead(n);
        return n;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var n = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        AfterRead(n);
        return n;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var n = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        AfterRead(n);
        return n;
    }

    private void AfterRead(int n)
    {
        if (n <= 0)
        {
            _progress?.Report((_read, _total));
            return;
        }

        _read += n;
        _sinceLastReport += n;
        if (_sinceLastReport >= _reportEveryBytes || _read >= _total)
        {
            _sinceLastReport = 0;
            _progress?.Report((_read, _total));
        }
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <summary>Владелец внешнего потока сам его закрывает (StreamContent вызывает Dispose только на обёртке).</summary>
    protected override void Dispose(bool disposing) => base.Dispose(disposing);
}
