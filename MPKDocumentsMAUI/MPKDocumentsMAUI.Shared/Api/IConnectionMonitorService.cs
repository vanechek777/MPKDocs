namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Мониторинг задержки до активного API (для toast «плохая сеть»).</summary>
public interface IConnectionMonitorService
{
    event Action? Changed;

    int? LatencyMs { get; }
    bool IsPoor { get; }
    /// <summary>Нет ответа или задержка выше порога.</summary>
    bool HasConnectionProblem { get; }

    void Start();
    void Stop();
}
