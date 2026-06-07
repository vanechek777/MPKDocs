using System.Globalization;
using System.Text;

namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Формирование .txt журнала на клиенте (тот же формат, что GET /admin/activity/export).</summary>
public static class ActivityLogExport
{
    public static byte[] ToUtf8Bytes(
        IReadOnlyList<AdminActivityItemDto> rows,
        DateOnly? from,
        DateOnly? to,
        int limit)
    {
        var lines = new List<string>
        {
            $"Журнал действий — {AppBranding.DisplayName}",
            $"Сформировано (UTC): {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.ffffff}",
        };
        if (from is not null || to is not null)
        {
            var f = from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "…";
            var t = to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "…";
            lines.Add($"Период: {f} — {t}");
        }

        lines.Add($"Записей в выгрузке: {rows.Count} (лимит {limit})");
        lines.Add("");
        lines.Add("Время (UTC)\tПользователь\tДействие\tДетали");
        lines.Add(new string('-', 72));

        foreach (var r in rows)
        {
            var uname = string.IsNullOrWhiteSpace(r.UserName)
                ? (r.UserId?.ToString(CultureInfo.InvariantCulture) ?? "")
                : r.UserName.Trim();
            var detail = (r.Detail ?? "")
                .Replace('\t', ' ')
                .Replace('\r', ' ')
                .Replace('\n', ' ');
            lines.Add($"{r.CreatedAt ?? ""}\t{uname}\t{r.Action}\t{detail}");
        }

        return Encoding.UTF8.GetBytes(string.Join('\n', lines) + "\n");
    }
}
