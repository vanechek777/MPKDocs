using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record DepartmentListItemDto(
    int id,
    string name
);

