namespace MPKDocumentsMAUI.Shared.Ui;

/// <summary>Результат отправки кода из модального окна OTP.</summary>
public enum OtpSubmitResult
{
    /// <summary>Неверный или просроченный OTP — подсветить ячейки.</summary>
    InvalidCode,

    /// <summary>Ошибка сети/сервера или нет задания — модалку не закрывать, показать текст.</summary>
    FailedRetryable,

    /// <summary>Подписание принято, модалку закрыть.</summary>
    Succeeded,
}
