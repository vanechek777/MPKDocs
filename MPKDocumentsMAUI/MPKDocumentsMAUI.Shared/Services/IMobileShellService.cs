namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>
/// Упрощённый «мобильный» режим: только подписание и проверка НЭП (телефон / планшет в MAUI).
/// </summary>
public interface IMobileShellService
{
    bool IsSigningOnlyShell { get; }

    /// <summary>
    /// Относительный путь (например /signing или /document/12), без query.
    /// </summary>
    bool IsPathAllowed(string absolutePath);
}
