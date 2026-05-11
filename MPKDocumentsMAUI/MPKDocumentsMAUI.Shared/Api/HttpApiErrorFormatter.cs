using System.Net;

namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Перевод типичных HTTP-ответов в сообщения для экрана входа и профиля.</summary>
public static class HttpApiErrorFormatter
{
    public static string Humanize(HttpStatusCode status, string? detailOrReason)
    {
        var d = (detailOrReason ?? "").Trim();

        if (status == HttpStatusCode.NotFound)
        {
            return
                "Сервер вернул 404: по этому адресу нет API (часто туннель выключен или неверный URL). "
                + "Укажите рабочий адрес бэкенда: в MAUI — файл Resources/Raw/appsettings.txt (JSON, ключ Api:BaseUrl), "
                + "в веб-проекте — appsettings.json. Пример для локального API: http://localhost:8000";
        }

        if (status == HttpStatusCode.Unauthorized &&
            string.Equals(d, "Invalid credentials", StringComparison.OrdinalIgnoreCase))
            return "Неверный телефон или пароль.";

        if (string.IsNullOrEmpty(d))
            return status switch
            {
                HttpStatusCode.ServiceUnavailable => "Сервис временно недоступен (503).",
                HttpStatusCode.BadGateway => "Ошибка шлюза (502). Проверьте бэкенд и SMTP/SMS.",
                _ => "Ошибка запроса",
            };

        return d;
    }
}
