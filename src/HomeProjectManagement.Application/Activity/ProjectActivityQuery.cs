using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.Activity;

/// <summary>
/// Composes a project's recent-activity feed from the existing repository ports (read-only). It
/// gathers two kinds of activity for the given project: <b>comments</b> (every bid's discussion
/// notes) and <b>updates</b> (each bid opened, each work package defined, and the project's own
/// creation), then orders them newest-first and caps the list.
/// </summary>
/// <remarks>
/// "Updates" are derived from the aggregate-root audit fields (<c>CreatedOn</c>/<c>CreatedBy</c>):
/// only <i>creation</i> events are surfaced. Generic "modified" activity is intentionally omitted —
/// <c>ModifiedOn</c> is stamped on every save (including logging a note), so it would duplicate the
/// note entries and read as noise. Richer history (status changes, awards) would need persisted
/// domain events; this read model can grow to include them without changing the endpoint contract.
/// </remarks>
public sealed class ProjectActivityQuery(
    IProjectRepository projects,
    IWorkPackageRepository workPackages,
    IBidRepository bids,
    IContractorRepository contractors) : IProjectActivityQuery
{
    public async Task<IReadOnlyList<ActivityItemDto>?> GetAsync(
        Guid projectId,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var project = await projects.GetAsync(new ProjectId(projectId), cancellationToken);
        if (project is null)
        {
            return null;
        }

        // Resolve contractor names once (master data is small) so bid/note items can carry them.
        var contractorNames = (await contractors.ListAsync(cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);

        var items = new List<ActivityItemDto>
        {
            // The project's own creation — the feed's earliest entry.
            new(
                ActivityKind.ProjectCreated,
                project.CreatedOn,
                project.CreatedBy.Value,
                WorkPackageId: null,
                WorkPackageName: null,
                BidId: null,
                ContractorName: null,
                NoteType: null,
                Content: null),
        };

        var packages = await workPackages.ListByProjectAsync(project.Id, cancellationToken);
        foreach (var package in packages)
        {
            // A work package was defined.
            items.Add(new ActivityItemDto(
                ActivityKind.WorkPackageAdded,
                package.CreatedOn,
                package.CreatedBy.Value,
                package.Id.Value,
                package.Name,
                BidId: null,
                ContractorName: null,
                NoteType: null,
                Content: null));

            var packageBids = await bids.ListByWorkPackageAsync(package.Id, cancellationToken);
            foreach (var bid in packageBids)
            {
                var contractorName = contractorNames.GetValueOrDefault(bid.ContractorId);

                // A bid was opened on this work package.
                items.Add(new ActivityItemDto(
                    ActivityKind.BidOpened,
                    bid.CreatedOn,
                    bid.CreatedBy.Value,
                    package.Id.Value,
                    package.Name,
                    bid.Id.Value,
                    contractorName,
                    NoteType: null,
                    Content: null));

                // Each discussion note (comment) on the bid.
                foreach (var note in bid.Notes)
                {
                    items.Add(new ActivityItemDto(
                        ActivityKind.NoteLogged,
                        note.OccurredOn,
                        note.AuthorId.Value,
                        package.Id.Value,
                        package.Name,
                        bid.Id.Value,
                        contractorName,
                        note.Type,
                        note.Content));
                }
            }
        }

        return items
            .OrderByDescending(i => i.Timestamp)
            .Take(take)
            .ToList();
    }
}
