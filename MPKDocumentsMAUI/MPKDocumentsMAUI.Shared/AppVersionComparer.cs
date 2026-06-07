namespace MPKDocumentsMAUI.Shared;

public static class AppVersionComparer
{
    public static int Compare(string? left, string? right)
    {
        var a = ParseParts(left);
        var b = ParseParts(right);
        var len = Math.Max(a.Length, b.Length);
        for (var i = 0; i < len; i++)
        {
            var av = i < a.Length ? a[i] : 0;
            var bv = i < b.Length ? b[i] : 0;
            if (av != bv)
                return av.CompareTo(bv);
        }

        return 0;
    }

    private static int[] ParseParts(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => int.TryParse(p, out var n) ? n : 0)
            .ToArray();
    }
}
