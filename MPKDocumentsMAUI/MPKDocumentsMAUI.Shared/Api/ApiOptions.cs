namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>
/// Базовый URL HTTP API (без завершающего /). Продакшен: mpk-docs.ru.tuna.am.
/// Для локальной разработки переопределите BaseUrl при регистрации DI (MAUI MauiProgram или Web Program).
/// </summary>
public sealed class ApiOptions
{
    public string BaseUrl { get; init; } = "https://mpk-docs.ru.tuna.am";
}

