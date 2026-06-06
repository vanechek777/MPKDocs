using System.Linq;
using System.Text;

namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Форматирование и нормализация российского номера (+7).</summary>
public static class PhoneDisplay
{
    /// <summary>11 цифр: код страны 7 + 10 цифр абонента.</summary>
    public const int RuTotalDigits = 11;

    public const int RuNationalDigits = 10;

    /// <summary>Длина отформатированной строки: +7 (999) 999-99-99</summary>
    public const int FormattedMaxLength = 18;

    public static string MaskRu(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "+7 (***) ***-**-****";
        var digits = ExtractRuDigits(phone);
        if (digits.Length >= 4)
        {
            var tail = digits[^4..];
            return $"+7 (***) ***-**-{tail}";
        }
        return "+7 (***) ***-**-****";
    }

    /// <summary>Маска ввода: +7 (XXX) XXX-XX-XX, не более 11 цифр.</summary>
    public static string FormatInput(string? raw)
    {
        var full = ExtractRuDigits(raw);
        if (full.Length == 0)
            return "+7 ";

        var national = full.Length > 1 ? full[1..] : "";
        if (national.Length == 0)
            return "+7 ";

        var sb = new StringBuilder("+7 (");
        sb.Append(national[..Math.Min(3, national.Length)]);
        if (national.Length <= 3)
            return sb.ToString();

        sb.Append(") ");
        sb.Append(national[3..Math.Min(6, national.Length)]);
        if (national.Length <= 6)
            return sb.ToString();

        sb.Append('-');
        sb.Append(national[6..Math.Min(8, national.Length)]);
        if (national.Length <= 8)
            return sb.ToString();

        sb.Append('-');
        sb.Append(national[8..Math.Min(10, national.Length)]);
        return sb.ToString();
    }

    /// <summary>Нормализация для API: +7XXXXXXXXXX (до 11 цифр).</summary>
    public static string NormalizeE164(string? raw)
    {
        var d = ExtractRuDigits(raw);
        return d.Length == 0 ? "" : "+" + d;
    }

    public static bool IsCompleteRu(string? raw)
    {
        var d = ExtractRuDigits(raw);
        return d.Length == RuTotalDigits && d[0] == '7' && d[1..].All(char.IsDigit);
    }

    public static int CountDigits(string? raw) => DigitsOnly(raw).Length;

    /// <summary>Нормализует к виду 7XXXXXXXXXX, обрезает до 11 цифр.</summary>
    public static string ExtractRuDigits(string? raw)
    {
        var d = DigitsOnly(raw);
        if (d.Length == 0)
            return "";

        if (d[0] == '8')
            d = "7" + d[1..];
        else if (d[0] != '7')
            d = "7" + d;

        return d.Length > RuTotalDigits ? d[..RuTotalDigits] : d;
    }

    private static string DigitsOnly(string? s) =>
        new string((s ?? "").Where(char.IsDigit).ToArray());
}
