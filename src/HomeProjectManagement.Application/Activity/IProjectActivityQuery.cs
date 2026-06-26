namespace HomeProjectManagement.Application.Activity;

/// <summary>
/// Read/query use case (driving port) that composes a project's recent-activity feed across
/// aggregates — discussion notes (comments) plus structural additions (bids opened, work packages
/// defined, the project created). It is read-only: it loads through the existing repository ports and
/// never mutates or commits. The "what counts as activity and how it is ordered" rule lives in the one
/// implementation so every view of the feed stays consistent.
/// </summary>
public interface IProjectActivityQuery
{
    /// <summary>
    /// The most recent activity for one project, newest first (at most <paramref name="take"/> items),
    /// or null if the project does not exist (→ 404 at the edge).
    /// </summary>
    Task<IReadOnlyList<ActivityItemDto>?> GetAsync(
        Guid projectId,
        int take = 30,
        CancellationToken cancellationToken = default);
}
