using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.CostScenarios.Events;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.CostScenarios;

/// <summary>
/// A saved "what-if" cost combination for a project: for each work package, a chosen bid whose bill
/// of quantities supplies the cost. Answers "what will the build cost if we award <i>this</i> bid for
/// the foundation, <i>that</i> bid for the roof, …?" — a single, named combination, in contrast to
/// the rule-driven range the project budget shows.
/// </summary>
/// <remarks>
/// Aggregate root. It references its owning <see cref="ProjectId"/> <b>by identity</b> and owns a set
/// of <see cref="ScenarioSelection"/>s (each holding a work-package and bid id), with at most one
/// selection per work package. It holds <b>only ids</b> — the money lives in the bills of quantities
/// and is computed at read time by an application query. Cross-aggregate validity (the work package
/// belongs to the project, the bid belongs to the work package) is the application service's job, not
/// the domain's. Construction goes through the <see cref="Create"/> factory.
/// </remarks>
public sealed class CostScenario : AggregateRoot<CostScenarioId>
{
    private readonly List<ScenarioSelection> _selections = [];

    /// <summary>The owning project (by id).</summary>
    public ProjectId ProjectId { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    /// <summary>
    /// The chosen bid per work package (internal entities). Mutated only through
    /// <see cref="IncludeBid"/> and <see cref="RemoveWorkPackage"/>; EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<ScenarioSelection> Selections => _selections.AsReadOnly();

    // EF Core materialisation constructor.
    private CostScenario()
    {
    }

    private CostScenario(CostScenarioId id, ProjectId projectId, string name) : base(id)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
    }

    /// <summary>
    /// Factory: create a new cost scenario within a project, validating its invariants.
    /// <paramref name="now"/> is supplied by the caller (from <c>TimeProvider</c>) rather than read
    /// inside the domain. A freshly created scenario has no selections.
    /// </summary>
    public static CostScenario Create(
        ProjectId projectId,
        string name,
        DateTimeOffset now,
        string? description = null)
    {
        var scenario = new CostScenario(CostScenarioId.New(), projectId, NormalizeName(name))
        {
            Description = Trim(description)
        };

        scenario.Raise(new CostScenarioCreated(scenario.Id, projectId, scenario.Name, now));
        return scenario;
    }

    /// <summary>Rename the scenario.</summary>
    public void Rename(string name) => Name = NormalizeName(name);

    /// <summary>Update the free-text description.</summary>
    public void Describe(string? description) => Description = Trim(description);

    /// <summary>
    /// Choose a bid for a work package (upsert): replace any existing choice for that work package,
    /// keeping the "one bid per work package" invariant, otherwise add a new selection. Validating
    /// that the work package belongs to this scenario's project and the bid belongs to that work
    /// package is the application service's responsibility.
    /// </summary>
    public void IncludeBid(WorkPackageId workPackageId, BidId bidId)
    {
        var existing = _selections.FirstOrDefault(s => s.WorkPackageId == workPackageId);
        if (existing is not null)
        {
            existing.ChooseBid(bidId);
            return;
        }

        _selections.Add(new ScenarioSelection(workPackageId, bidId));
    }

    /// <summary>
    /// Drop the selection for a work package (exclude it from the scenario). Returns false if the
    /// work package was not included.
    /// </summary>
    public bool RemoveWorkPackage(WorkPackageId workPackageId)
    {
        var existing = _selections.FirstOrDefault(s => s.WorkPackageId == workPackageId);
        if (existing is null)
        {
            return false;
        }

        _selections.Remove(existing);
        return true;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Cost scenario name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
