namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Маскирование email для подсказок в UI (OTP на почту).</summary>
public static class EmailDisplay
{
    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "";

        var e = email.Trim();
        var at = e.IndexOf('@');
        if (at <= 0 || at >= e.Length - 1)
            return e;

        var local = e[..at];
        var domain = e[(at + 1)..];
        var maskedLocal = local.Length switch
        {
            1 => $"{local[0]}***",
            2 => $"{local[0]}***",
            _ => $"{local[0]}***{local[^1]}",
        };
        return $"{maskedLocal}@{domain}";
    }
}
