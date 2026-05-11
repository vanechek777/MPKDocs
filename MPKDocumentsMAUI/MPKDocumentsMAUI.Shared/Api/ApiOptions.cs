namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>
/// Базовый URL HTTP API (без завершающего /). Дефолт: mpk-docs.ru.tuna.am (если туннель выключен — будет 404).
/// MAUI: переопределите в <c>Resources/Raw/appsettings.txt</c> (JSON) → <c>Api:BaseUrl</c>.
/// Web: <c>appsettings.json</c> / <c>appsettings.Development.json</c> → <c>Api:BaseUrl</c>.
/// </summary>
public sealed class ApiOptions
{
    public string BaseUrl { get; init; } = "https://mpk-docs.ru.tuna.am";
}

