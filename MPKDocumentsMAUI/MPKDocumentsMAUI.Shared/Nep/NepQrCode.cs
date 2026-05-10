using System.Text;
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
