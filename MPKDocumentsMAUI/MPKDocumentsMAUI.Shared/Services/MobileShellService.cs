namespace MPKDocumentsMAUI.Shared.Services;

public sealed class MobileShellService : IMobileShellService
{
    private readonly IFormFactor _formFactor;

    public MobileShellService(IFormFactor formFactor) => _formFactor = formFactor;

    public bool IsSigningOnlyShell
    {
        get
        {
            var idiom = _formFactor.GetFormFactor();
            return string.Equals(idiom, "Phone", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(idiom, "Tablet", StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool IsPathAllowed(string absolutePath)
    {
        if (!IsSigningOnlyShell)
            return true;

        var p = (absolutePath ?? "/").TrimEnd('/').ToLowerInvariant();
        if (string.IsNullOrEmpty(p))
            p = "/";

        if (p == "/" || p == "")
            return false;

        if (p.StartsWith("/signing", StringComparison.Ordinal))
            return true;
        if (p.StartsWith("/verify-nep", StringComparison.Ordinal))
            return true;
        if (p.StartsWith("/document/", StringComparison.Ordinal))
            return true;
        if (p == "/login" || p == "/register")
            return true;

        return false;
    }
}
