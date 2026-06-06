namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Оповещение об изменении ленты документов (polling feed-stamp).</summary>
public interface IDocumentFeedWatchService
{
    event Action? FeedChanged;

    string? LastStamp { get; }

    void Start();
    void Stop();
}
