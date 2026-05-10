using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record SigningListItemDto(
    [property: JsonPropertyName("document_id")] int document_id,
    string title,
    string? status,
    [property: JsonPropertyName("received_at")] DateTime? received_at,
    [property: JsonPropertyName("signed_at")] DateTime? signed_at,
    [property: JsonPropertyName("initiator_name")] string initiator_name,
    [property: JsonPropertyName("signed_count")] int signed_count,
    [property: JsonPropertyName("total_signers")] int total_signers,
    [property: JsonPropertyName("has_viewed")] bool has_viewed = false,
    [property: JsonPropertyName("waiting_for_other_signers")] bool waiting_for_other_signers = false,
    [property: JsonPropertyName("recipients_viewed")] int recipients_viewed = 0,
    [property: JsonPropertyName("recipients_total")] int recipients_total = 0
);

