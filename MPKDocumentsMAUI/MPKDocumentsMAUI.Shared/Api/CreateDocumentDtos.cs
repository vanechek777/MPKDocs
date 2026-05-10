using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record CreateDocumentRequestDto(
    [property: JsonPropertyName("template_id")] int template_id,
    Dictionary<string, object?>? content,
    [property: JsonPropertyName("signer_user_ids")] List<int> signer_user_ids,
    [property: JsonPropertyName("signer_department_id")] int? signer_department_id,
    [property: JsonPropertyName("save_as_draft")] bool save_as_draft
);

public sealed record CreateDocumentResponseDto(
    [property: JsonPropertyName("document_id")] int document_id,
    string status
);

