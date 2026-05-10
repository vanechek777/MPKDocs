using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record UserListItemDto(
    int id,
    [property: JsonPropertyName("full_name")] string full_name,
    string? department,
    string? position
);

