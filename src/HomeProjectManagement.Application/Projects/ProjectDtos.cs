using HomeProjectManagement.Domain.Projects;

namespace HomeProjectManagement.Application.Projects;

/// <summary>
/// Read model returned to clients. Mirrors the frontend's <c>Project</c> type in
/// <c>app/lib/api.ts</c>. <c>DueDate</c> is the API name for the domain's
/// <c>TargetCompletionDate</c>; <c>CreatedAt</c> comes from the aggregate's audit fields.
/// </summary>
public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DueDate,
    int ApartmentUnits);

/// <summary>Input for creating a project (matches the frontend POST payload).</summary>
public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTimeOffset? DueDate,
    int ApartmentUnits = 1);

/// <summary>Input for updating a project (matches the frontend PUT payload).</summary>
public sealed record UpdateProjectCommand(
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTimeOffset? DueDate,
    int ApartmentUnits = 1);
