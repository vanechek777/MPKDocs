using System.Text;
using System.Text.Json;
using QRCoder;

namespace MPKDocumentsMAUI.Shared.Nep;

/// <summary>QR для мобильного приложения: JSON с идентификатором документа и ключом подписи (хеш).</summary>
public static class NepQrCode
{
    /// <summary>Компактный JSON для сканирования: <c>mpk</c>, <c>document_hash_hex</c>, при наличии <c>document_id</c>.</summary>
    public static string BuildMobileVerificationPayload(int? documentId, string? documentHashHex)
    {
        if (string.IsNullOrWhiteSpace(documentHashHex)) return "";
        var hash = documentHashHex.Trim().ToLowerInvariant();
        var sb = new StringBuilder(120 + hash.Length);
        sb.Append("{\"mpk\":\"nep\",\"document_hash_hex\":\"");
        JsonEscape(sb, hash);
        sb.Append('"');
        if (documentId is > 0)
            sb.Append(",\"document_id\":").Append(documentId.Value);
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>Сырой текст из QR (или вставка пользователем): JSON с ключами mpk, document_hash_hex.</summary>
    public static bool TryParseMobileVerificationPayload(string? scanned, out int? documentId, out string? documentHashHex)
    {
        documentId = null;
        documentHashHex = null;
        if (string.IsNullOrWhiteSpace(scanned))
            return false;

        var t = scanned.Trim();
        try
        {
            using var doc = JsonDocument.Parse(t);
            var root = doc.RootElement;
            if (!root.TryGetProperty("mpk", out var mpkEl) ||
                !string.Equals(mpkEl.GetString(), "nep", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!root.TryGetProperty("document_hash_hex", out var hashEl))
                return false;
            var hash = hashEl.GetString();
            if (string.IsNullOrWhiteSpace(hash))
                return false;
            documentHashHex = hash.Trim().ToLowerInvariant();

            if (root.TryGetProperty("document_id", out var idEl) && idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt32(out var id) && id > 0)
                documentId = id;

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void JsonEscape(StringBuilder sb, string s)
    {
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ')
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
    }

    public static string? ToPngDataUrl(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(8);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
