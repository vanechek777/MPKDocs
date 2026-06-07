namespace MPKDocumentsMAUI.Shared.Services;

/// <summary>Открытие модалки выбора API-хоста из toast, профиля и т.д.</summary>
public interface IApiHostSettingsUi
{
    event Action? OpenRequested;

    void RequestOpen();
}

public sealed class ApiHostSettingsUi : IApiHostSettingsUi
{
    public event Action? OpenRequested;

    public void RequestOpen() => OpenRequested?.Invoke();
}
