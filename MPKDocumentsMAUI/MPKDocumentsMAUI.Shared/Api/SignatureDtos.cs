using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record VerifyEsigResponseDto(
    bool ok,
    [property: JsonPropertyName("crypto_valid")] bool crypto_valid,
    [property: JsonPropertyName("matches_current_document")] bool matches_current_document,
    [property: JsonPropertyName("document_id")] int? document_id,
    [property: JsonPropertyName("document_exists")] bool document_exists,
    [property: JsonPropertyName("document_title")] string? document_title,
    [property: JsonPropertyName("template_name")] string? template_name,
    [property: JsonPropertyName("signer_user_id")] int? signer_user_id,
    [property: JsonPropertyName("signer_name")] string? signer_name,
    [property: JsonPropertyName("signed_at_utc")] string? signed_at_utc,
    [property: JsonPropertyName("document_hash_hex")] string? document_hash_hex,
    [property: JsonPropertyName("signature_hex")] string? signature_hex,
    [property: JsonPropertyName("current_document_hash_hex")] string? current_document_hash_hex,
    string? detail
);
