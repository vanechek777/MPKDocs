using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record DocumentListItemDto(
    int id,
    string title,
    string? status,
    [property: JsonPropertyName("created_at")] DateTime? created_at,
    [property: JsonPropertyName("initiator_name")] string initiator_name,
    [property: JsonPropertyName("signed_count")] int signed_count,
    [property: JsonPropertyName("total_signers")] int total_signers,
    [property: JsonPropertyName("my_signed")] bool my_signed,
    [property: JsonPropertyName("is_sent")] bool is_sent,
    [property: JsonPropertyName("has_viewed")] bool has_viewed = false,
    [property: JsonPropertyName("waiting_for_other_signers")] bool waiting_for_other_signers = false,
    [property: JsonPropertyName("recipients_viewed")] int recipients_viewed = 0,
    [property: JsonPropertyName("recipients_total")] int recipients_total = 0,
    [property: JsonPropertyName("recipients_viewed_names")] IReadOnlyList<string>? recipients_viewed_names = null,
    [property: JsonPropertyName("recipients_row_caption")] string? recipients_row_caption = null
);

