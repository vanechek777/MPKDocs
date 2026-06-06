using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Перевод типичных HTTP-ответов и сетевых сбоев в сообщения для UI.</summary>
public static class HttpApiErrorFormatter
{
    public static string Humanize(HttpStatusCode status, string? detailOrReason)
    {
        var d = TranslateDetail(detailOrReason ?? "").Trim();

        if (status == HttpStatusCode.NotFound)
        {
            return
                "Сервер вернул 404: по этому адресу нет API (часто туннель выключен или неверный URL). "
                + "Выберите или добавьте рабочий адрес в админ-панели (раздел «Базовый URL API»). "
                + "Пример локального API: http://localhost:8000";
        }

        if (status == HttpStatusCode.Unauthorized)
        {
            if (string.Equals(d, "Invalid credentials", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(d))
                return "Неверный телефон или пароль.";
        }

        if (!string.IsNullOrEmpty(d))
            return d;

        return status switch
        {
            HttpStatusCode.Unauthorized => "Неверный телефон или пароль.",
            HttpStatusCode.ServiceUnavailable => "Сервис временно недоступен. Попробуйте позже.",
            HttpStatusCode.BadGateway => "Ошибка шлюза (502). Проверьте бэкенд и почтовый сервер.",
            HttpStatusCode.GatewayTimeout => "Превышено время ожидания ответа сервера.",
            HttpStatusCode.RequestTimeout => "Превышено время ожидания запроса.",
            HttpStatusCode.Conflict => "Конфликт данных: запись уже существует.",
            HttpStatusCode.TooManyRequests => "Слишком много запросов. Подождите и повторите.",
            _ => "Ошибка запроса",
        };
    }

    public static string HumanizeException(Exception ex)
    {
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode is { } code)
            {
                return !string.IsNullOrWhiteSpace(httpEx.Message)
                    ? httpEx.Message
                    : Humanize(code, null);
            }

            var msg = httpEx.Message;
            if (!string.IsNullOrWhiteSpace(msg) && !msg.StartsWith("Response status code", StringComparison.Ordinal))
                return msg;

            if (IsNetworkProblem(httpEx))
                return "Проблемы с сетью. Проверьте подключение к интернету и адрес сервера API.";
        }

        if (ex is TaskCanceledException or OperationCanceledException)
            return "Проблемы с сетью: превышено время ожидания ответа сервера.";

        if (ex.InnerException is not null)
        {
            var inner = HumanizeException(ex.InnerException);
            if (inner.Contains("сет", StringComparison.OrdinalIgnoreCase))
                return inner;
        }

        if (IsNetworkProblem(ex))
            return "Проблемы с сетью. Проверьте подключение к интернету и адрес сервера API.";

        return string.IsNullOrWhiteSpace(ex.Message) ? "Неизвестная ошибка." : ex.Message;
    }

    private static bool IsNetworkProblem(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is SocketException or IOException)
                return true;
            var m = e.Message ?? "";
            if (m.Contains("No connection", StringComparison.OrdinalIgnoreCase)
                || m.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
                || m.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
                || m.Contains("host unreachable", StringComparison.OrdinalIgnoreCase)
                || m.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase)
                || m.Contains("SSL", StringComparison.OrdinalIgnoreCase)
                || m.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                || m.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string TranslateDetail(string d)
    {
        if (string.IsNullOrWhiteSpace(d))
            return d;

        return d.Trim() switch
        {
            "Invalid credentials" => "Неверный телефон или пароль.",
            "Email already registered" => "Этот email уже зарегистрирован.",
            "Phone number already registered" => "Этот номер телефона уже зарегистрирован.",
            "Wait before requesting another code" => "Подождите перед повторной отправкой кода.",
            "Неверный или просроченный код." => "Неверный или просроченный код.",
            "Этот номер телефона уже зарегистрирован" => "Этот номер телефона уже зарегистрирован.",
            _ when d.Contains("Email already registered", StringComparison.OrdinalIgnoreCase)
                => "Этот email уже зарегистрирован.",
            _ when d.Contains("Phone number already registered", StringComparison.OrdinalIgnoreCase)
                => "Этот номер телефона уже зарегистрирован.",
            "Вы не найдены в кадровом справочнике. Обратитесь к администратору."
                => "Вы не найдены в кадровом справочнике. Обратитесь к администратору.",
            _ when d.Contains("кадровом справочнике", StringComparison.OrdinalIgnoreCase)
                => d,
            _ => d,
        };
    }
}
