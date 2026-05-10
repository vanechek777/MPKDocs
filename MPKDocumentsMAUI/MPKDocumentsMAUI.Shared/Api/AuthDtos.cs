using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record LoginRequest(string phone_number, string password);

public sealed record RegisterRequest(string phone_number, string full_name, string password, string email);

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

