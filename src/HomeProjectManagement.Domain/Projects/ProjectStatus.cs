namespace HomeProjectManagement.Domain.Projects;

/// <summary>
/// Lifecycle of a project. Persisted as its string name (matching the frontend's union
/// type in <c>app/lib/api.ts</c>).
/// </summary>
/// <remarks>
/// Note: the domain model draft lists the first value as <c>Planning</c>, but the existing
/// frontend contract uses <c>Planned</c>. We keep <c>Planned</c> here to avoid breaking the
/// running UI; revisit if the ubiquitous language settles on "Planning".
/// </remarks>
public enum ProjectStatus
{
    Planned,
    InProgress,
    OnHold,
    Completed
}
