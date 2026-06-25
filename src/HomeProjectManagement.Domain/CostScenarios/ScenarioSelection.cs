using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.CostScenarios;

/// <summary>
/// One choice within a <see cref="CostScenario"/>: in this scenario, a given work package's cost is
/// taken from a given bid (its bill of quantities). A local entity owned by the scenario, holding
/// both references <b>by id</b> — the scenario never holds the work package or bid objects. There is
/// at most one selection per work package (the scenario root enforces this; the database keys on it).
/// </summary>
public sealed class ScenarioSelection
{
    /// <summary>The work package this choice is for (by id).</summary>
    public WorkPackageId WorkPackageId { get; private set; }

    /// <summary>The chosen bid whose BoQ supplies the cost (by id).</summary>
    public BidId BidId { get; private set; }

    // EF Core materialisation constructor.
    private ScenarioSelection()
    {
    }

    // Created only by the CostScenario root (see CostScenario.IncludeBid).
    internal ScenarioSelection(WorkPackageId workPackageId, BidId bidId)
    {
        WorkPackageId = workPackageId;
        BidId = bidId;
    }

    // The chosen bid may change while the work package stays the same (one-per-package upsert).
    internal void ChooseBid(BidId bidId) => BidId = bidId;
}
