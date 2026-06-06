using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record StaffPositionItem(int id, string name);

public sealed record StaffDepartmentItem(int id, string name);

public sealed record StaffSuggestItem(
    int id,
    string full_name,
    int position_id,
    string position_name,
    int department_id,
    string department_name);

public sealed record StaffImportResult(
    [property: JsonPropertyName("rows_total")] int RowsTotal,
    [property: JsonPropertyName("rows_imported")] int RowsImported,
    [property: JsonPropertyName("departments_upserted")] int DepartmentsUpserted,
    [property: JsonPropertyName("positions_upserted")] int PositionsUpserted,
    [property: JsonPropertyName("staff_upserted")] int StaffUpserted);

public sealed record OneCConfigDto(
    [property: JsonPropertyName("base_url")] string? BaseUrl,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("has_password")] bool HasPassword);

public sealed record OneCConfigUpdateRequest(
    [property: JsonPropertyName("base_url")] string? BaseUrl,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("password")] string? Password);

public sealed record OneCTestResult(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("latency_ms")] long? LatencyMs,
    [property: JsonPropertyName("message")] string? Message);

public sealed record StaffStatsDto(
    [property: JsonPropertyName("staff_total")] int StaffTotal,
    [property: JsonPropertyName("positions_total")] int PositionsTotal,
    [property: JsonPropertyName("departments_total")] int DepartmentsTotal);
