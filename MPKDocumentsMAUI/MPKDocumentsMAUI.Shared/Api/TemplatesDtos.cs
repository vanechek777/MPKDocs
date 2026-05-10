using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record TemplateListItemDto(
    int id,
    string name,
    string? category,
    [property: JsonPropertyName("is_active")] bool? is_active
);

public sealed record TemplateDetailDto(
    int id,
    string name,
    [property: JsonPropertyName("form_schema")] Dictionary<string, object?> form_schema,
    [property: JsonPropertyName("template_path")] string template_path
);

