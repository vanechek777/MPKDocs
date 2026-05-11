using System.Text.Json.Serialization;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed record AdminDashboardDto(
    [property: JsonPropertyName("server_time_utc")] string ServerTimeUtc,
    [property: JsonPropertyName("app_version")] string AppVersion,
    [property: JsonPropertyName("database_ok")] bool DatabaseOk,
    [property: JsonPropertyName("database_latency_ms")] double? DatabaseLatencyMs,
    [property: JsonPropertyName("online_users_5m")] int OnlineUsers5m,
    [property: JsonPropertyName("users_total")] int UsersTotal,
    [property: JsonPropertyName("documents_total")] int DocumentsTotal,
    [property: JsonPropertyName("templates_active")] int TemplatesActive,
    [property: JsonPropertyName("categories_total")] int CategoriesTotal);

public sealed record AdminActivityItemDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("created_at")] string? CreatedAt,
    [property: JsonPropertyName("user_id")] int? UserId,
    [property: JsonPropertyName("user_name")] string? UserName,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("detail")] string? Detail);

public sealed record AdminUserDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("phone_number")] string PhoneNumber,
    [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("is_admin")] bool IsAdmin,
    [property: JsonPropertyName("status")] bool? Status);

public sealed record AdminCategoryDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sort_order")] int SortOrder,
    [property: JsonPropertyName("is_active")] bool? IsActive);

public sealed record AdminCategoryCreateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sort_order")] int SortOrder = 0);

public sealed record AdminCategoryPatchRequest(
    [property: JsonPropertyName("is_active")] bool? IsActive = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("sort_order")] int? SortOrder = null);

public sealed record AdminSetAdminRequest([property: JsonPropertyName("is_admin")] bool IsAdmin);

public sealed record AdminPromoteByPhoneRequest([property: JsonPropertyName("phone_number")] string PhoneNumber);

public sealed record AdminTemplateDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category_id")] int? CategoryId,
    [property: JsonPropertyName("category_name")] string? CategoryName,
    [property: JsonPropertyName("is_active")] bool? IsActive,
    [property: JsonPropertyName("template_path")] string TemplatePath);

public sealed record AdminTemplateCreateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category_id")] int? CategoryId,
    [property: JsonPropertyName("template_path")] string? TemplatePath,
    [property: JsonPropertyName("form_schema")] Dictionary<string, object?>? FormSchema);

public sealed record AdminTemplatePatchRequest(
    [property: JsonPropertyName("is_active")] bool? IsActive = null,
    [property: JsonPropertyName("category_id")] int? CategoryId = null,
    [property: JsonPropertyName("name")] string? Name = null);
