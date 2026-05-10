using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record LoginRequest(string phone_number, string password);

public sealed record RegisterRequest(string phone_number, string full_name, string password, string email);

/* --- Контракт для бэкенда (FastAPI и т.п.) — см. методы AuthApiClient c путём /auth/email/... ---
 * POST /auth/email/login/send        body: { "email": "..." }           → { "ok": true, "dev_code": optional }
 * POST /auth/email/login/verify      body: { "email", "code" }           → TokenResponse
 * POST /auth/email/register/start    body: RegisterRequest               → как login/send (код на почту)
 * POST /auth/email/register/verify     body: { "email", "code" }           → TokenResponse
 * Сервер должен сохранять hash пароля между start и verify (KV/кэш/сессии) либо хранить «pending» пользователя.
 *
 * Уведомления по почте («вам документ», «ваш документ подписали»): при назначении задания и при смене статуса документа
 * отправлять письма через SMTP/sendgrid на recipient.email — клиент здесь только отображает; логику реализует API.
 */

public sealed record EmailLoginSendRequest(string email);

public sealed record EmailLoginVerifyRequest(string email, string code);

public sealed record RegisterEmailVerifyRequest(string email, string code);

public sealed record EmailCodeSendResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("dev_code")] string? DevCode);

public sealed record TokenResponse(string access_token, string token_type);

public sealed record MeResponse(
    int id,
    string phone_number,
    string full_name,
    string? email,
    string? department = null,
    string? position = null,
    [property: JsonPropertyName("is_admin")] bool is_admin = false);

public sealed record MePatchRequest(
    string? full_name = null,
    string? phone_number = null,
    string? email = null);

/// <summary>Ответ <c>POST /auth/otp/send</c>.</summary>
public sealed record OtpSendResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("dev_code")] string? DevCode);

