using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record SignerNodeDto(
    int order,
    string? department,
    [property: JsonPropertyName("user_id")] int? user_id,
    [property: JsonPropertyName("user_name")] string? user_name,
    string status,
    [property: JsonPropertyName("processed_at")] DateTime? processed_at,
    [property: JsonPropertyName("signature_type")] string? signature_type
);

public sealed record DocumentDetailDto(
    int id,
    string? status,
    [property: JsonPropertyName("created_at")] DateTime? created_at,
    [property: JsonPropertyName("initiator_id")] int initiator_id,
    [property: JsonPropertyName("initiator_name")] string initiator_name,
    [property: JsonPropertyName("template_id")] int template_id,
    [property: JsonPropertyName("template_name")] string template_name,
    Dictionary<string, object?>? content,
    List<SignerNodeDto> signers,
    [property: JsonPropertyName("signed_count")] int signed_count,
    [property: JsonPropertyName("total_signers")] int total_signers,
    [property: JsonPropertyName("can_act")] bool can_act,
    [property: JsonPropertyName("my_task_status")] string? my_task_status,
    [property: JsonPropertyName("waiting_for_other_signers")] bool waiting_for_other_signers = false,
    [property: JsonPropertyName("recipients_viewed")] int recipients_viewed = 0,
    [property: JsonPropertyName("recipients_total")] int recipients_total = 0,
    [property: JsonPropertyName("has_file_attachment")] bool has_file_attachment = false,
    [property: JsonPropertyName("my_nep_export_available")] bool my_nep_export_available = false,
    [property: JsonPropertyName("document_content_hash_hex")] string? document_content_hash_hex = null,
    [property: JsonPropertyName("my_nep_document_hash_hex")] string? my_nep_document_hash_hex = null,
    [property: JsonPropertyName("my_nep_signature_hex")] string? my_nep_signature_hex = null
);

public sealed record ActionRequestDto(
    [property: JsonPropertyName("otp_code")] string? otp_code,
    string? reason
);

public sealed record ActionResponseDto(
    /// <summary>Если в JSON поле отсутствует, null — не считаем отказом (совместимость с разными сериализаторами).</summary>
    bool? ok,
    [property: JsonPropertyName("document_id")] int document_id,
    [property: JsonPropertyName("document_status")] string? document_status
);

