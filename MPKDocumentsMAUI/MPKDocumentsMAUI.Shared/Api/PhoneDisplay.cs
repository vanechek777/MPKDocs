using System.Linq;

namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Маска номера для подсказки в UI (без полного номера).</summary>
public static class PhoneDisplay
{
    public static string MaskRu(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "+7 (***) ***-**-****";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length >= 4)
        {
            var tail = digits[^4..];
            return $"+7 (***) ***-**-{tail}";
        }
        return "+7 (***) ***-**-****";
    }
}
