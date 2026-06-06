namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>
/// Текущий базовый URL HTTP API (без завершающего /).
/// Источник — <see cref="IApiEndpointStore"/> (список и выбор в админ-панели).
/// Стартовое значение: <c>appsettings.txt</c> (MAUI) или <c>Api:BaseUrl</c> (Web).
/// </summary>
public sealed class ApiOptions(IApiEndpointStore endpoints)
{
    public string BaseUrl => endpoints.ActiveBaseUrl;
}
