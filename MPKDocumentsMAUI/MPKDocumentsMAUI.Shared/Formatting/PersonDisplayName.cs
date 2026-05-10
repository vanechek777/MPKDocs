using System.Text;

namespace MPKDocumentsMAUI.Shared.Formatting;

/// <summary>Сокращение отображаемых ФИО для списков и карточек (например «Дьячков Е. Ю.»).</summary>
public static class PersonDisplayName
{
    public static string Abbreviate(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "";

        var parts = fullName.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "";
        if (parts.Length == 1)
            return parts[0].Trim();

        var sb = new StringBuilder();
        sb.Append(parts[0].Trim());
        for (var i = 1; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (p.Length == 0)
                continue;
            var initial = char.ToUpperInvariant(p[0]);
            sb.Append(' ');
            sb.Append(initial);
            sb.Append('.');
        }

        return sb.ToString();
    }
}
