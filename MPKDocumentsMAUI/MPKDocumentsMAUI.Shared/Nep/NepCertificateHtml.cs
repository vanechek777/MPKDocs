using System.Globalization;
using System.Text;
using MPKDocumentsMAUI.Shared.Api;

namespace MPKDocumentsMAUI.Shared.Nep;

/// <summary>HTML для печати свидетельства о НЭП-подписи (отдельный модуль разметки).</summary>
public static class NepCertificateHtml
{
    /// <summary>Дата и время подписи в местном часовом поясе устройства.</summary>
    public static string FormatSignedAtLocalDisplay(string? signedAtUtcIso)
    {
        if (string.IsNullOrWhiteSpace(signedAtUtcIso)) return "—";
        if (!DateTimeOffset.TryParse(
                signedAtUtcIso.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces,
                out var dto))
        {
            return signedAtUtcIso.Trim();
        }

        var local = dto.ToLocalTime();
        return local.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("ru-RU"));
    }

    public static string BuildFromVerification(VerifyEsigResponseDto r)
    {
        var title = string.IsNullOrWhiteSpace(r.document_title) ? "—" : r.document_title!;
        var signer = string.IsNullOrWhiteSpace(r.signer_name) ? "—" : r.signer_name!;
        var tpl = string.IsNullOrWhiteSpace(r.template_name) ? "—" : r.template_name!;
        var docId = r.document_id?.ToString() ?? "—";
        var status = r.ok ? "Подпись действительна для текущего документа в системе" :
            r.crypto_valid ? "Подпись криптографически верна, документ изменён или не совпадает" :
            "Подпись не проходит проверку";
        return BuildHtml(
            title,
            docId,
            tpl,
            signer,
            r.signed_at_utc,
            status,
            r.detail,
            r.document_hash_hex,
            r.signature_hex,
            r.current_document_hash_hex,
            r.document_id);
    }

    public static string BuildFromDocumentDetail(
        string documentTitle,
        int documentId,
        string templateName,
        string initiatorName,
        string signerName,
        string? signedAtUtc,
        bool nepCryptoOk,
        string? documentContentHashHex = null,
        string? myNepDocumentHashHex = null,
        string? myNepSignatureHex = null)
    {
        var status = nepCryptoOk
            ? "НЭП-подпись в системе проходит криптографическую проверку"
            : "НЭП-подпись не проходит проверку (обратитесь в поддержку)";
        return BuildHtml(
            string.IsNullOrWhiteSpace(documentTitle) ? "—" : documentTitle,
            documentId.ToString(),
            string.IsNullOrWhiteSpace(templateName) ? "—" : templateName,
            string.IsNullOrWhiteSpace(signerName) ? "—" : signerName,
            signedAtUtc,
            status,
            $"Отправитель: {initiatorName}",
            myNepDocumentHashHex,
            myNepSignatureHex,
            documentContentHashHex,
            documentId);
    }

    private static string EscapeHtml(string s)
    {
        var b = new StringBuilder(s.Length + 8);
        foreach (var c in s)
        {
            b.Append(c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                _ => c.ToString(),
            });
        }

        return b.ToString();
    }

    private static void AppendHashRow(StringBuilder sb, string th, string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        sb.Append("<tr><th>").Append(EscapeHtml(th)).Append("</th><td class=\"mono\">")
            .Append(EscapeHtml(hex.Trim().ToLowerInvariant()))
            .Append("</td></tr>");
    }

    private static string BuildHtml(
        string documentTitle,
        string documentId,
        string templateName,
        string signerName,
        string? signedAtUtcIso,
        string statusLine,
        string? detail,
        string? documentHashHex,
        string? signatureHex,
        string? currentDocumentHashHex,
        int? nepQrDocumentId)
    {
        var whenDisplay = FormatSignedAtLocalDisplay(signedAtUtcIso);
        var detailBlock = detail is { Length: > 0 }
            ? "<p class=\"sub\">" + EscapeHtml(detail) + "</p>"
            : "";

        var curHex = currentDocumentHashHex?.Trim();
        var docHex = documentHashHex?.Trim();
        if (!string.IsNullOrEmpty(curHex) && !string.IsNullOrEmpty(docHex)
            && string.Equals(curHex, docHex, StringComparison.OrdinalIgnoreCase))
        {
            curHex = null;
        }

        var docIdForQr = nepQrDocumentId ?? (int.TryParse(documentId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid) ? pid : (int?)null);
        var qrPayload = NepQrCode.BuildMobileVerificationPayload(docIdForQr, documentHashHex);
        var qrDataUrl = NepQrCode.ToPngDataUrl(qrPayload);
        var qrBlock = !string.IsNullOrEmpty(qrDataUrl)
            ? "<div class=\"qrwrap\"><img class=\"qrimg\" alt=\"\" width=\"200\" height=\"200\" src=\"" + qrDataUrl + "\"/></div>"
            : "";

        var sb = new StringBuilder(2048);
        sb.Append("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\"/><title>НЭП — свидетельство</title>");
        sb.Append("<style>");
        sb.Append("body{font-family:system-ui,Segoe UI,sans-serif;margin:32px;color:#111;}");
        sb.Append("h1{font-size:20px;margin-bottom:8px;}");
        sb.Append("table{border-collapse:collapse;width:100%;max-width:720px;margin-top:16px;}");
        sb.Append("td,th{border:1px solid #ccc;padding:10px 12px;text-align:left;vertical-align:top;}");
        sb.Append("th{width:38%;background:#f4f4f4;font-weight:600;}");
        sb.Append(".mono{font-family:ui-monospace,Consolas,monospace;font-size:11px;word-break:break-word;}");
        sb.Append(".status{margin-top:20px;font-weight:700;}");
        sb.Append(".sub{font-size:13px;margin-top:12px;color:#111;}");
        sb.Append(".qrwrap{margin-top:20px;}");
        sb.Append(".qrimg{display:block;}");
        sb.Append("</style></head><body>");
        sb.Append("<h1>Свидетельство о проверке НЭП</h1><table>");
        sb.Append("<tr><th>Документ</th><td>").Append(EscapeHtml(documentTitle)).Append("</td></tr>");
        sb.Append("<tr><th>Идентификатор</th><td>").Append(EscapeHtml(documentId)).Append("</td></tr>");
        sb.Append("<tr><th>Тип документа</th><td>").Append(EscapeHtml(templateName)).Append("</td></tr>");
        sb.Append("<tr><th>Подписант</th><td>").Append(EscapeHtml(signerName)).Append("</td></tr>");
        sb.Append("<tr><th>Дата и время подписи</th><td>").Append(EscapeHtml(whenDisplay)).Append("</td></tr>");
        AppendHashRow(sb, "Ключ подписи", documentHashHex);
        AppendHashRow(sb, "Текущий хеш в системе", curHex);
        AppendHashRow(sb, "Код подписи", signatureHex);
        sb.Append("</table>");
        sb.Append(qrBlock);
        sb.Append("<p class=\"status\">").Append(EscapeHtml(statusLine)).Append("</p>");
        sb.Append(detailBlock);
        sb.Append("</body></html>");
        return sb.ToString();
    }
}
