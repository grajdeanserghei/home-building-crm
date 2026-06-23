namespace HomeProjectManagement.Application.Budgeting;

/// <summary>
/// Read/query use case (driving port) that composes a project's cost picture across aggregates —
/// work packages, their bids' bills of quantities, and any awarded contract. It is read-only: it
/// loads through the existing repository ports and never mutates or commits. The "which figure wins
/// and how it sums" rule lives in the one implementation so the budget page and any future view
/// cannot drift apart.
/// </summary>
public interface IProjectBudgetQuery
{
    /// <summary>
    /// The budget rollup for one project, or null if the project does not exist (→ 404 at the edge).
    /// </summary>
    Task<ProjectBudgetDto?> GetAsync(Guid projectId, CancellationToken cancellationToken = default);
}
