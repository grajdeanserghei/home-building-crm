using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Contracts;
using HomeProjectManagement.Domain.CostScenarios;
using HomeProjectManagement.Domain.Projects;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Application.Valuations;

/// <summary>
/// Resolves one active BoQ per work package for a comparison basis, reusing only existing repository ports
/// (no persisted state). <b>Decided</b> follows the award/selection decision the owners already made;
/// <b>Scenario</b> reads a cost scenario's chosen bids — the same bid→BoQ resolution the cost simulator uses.
/// </summary>
public sealed class RealBoqSelector(
    IWorkPackageRepository workPackages,
    IContractRepository contracts,
    IBidRepository bids,
    IBillOfQuantitiesRepository billsOfQuantities,
    ICostScenarioRepository scenarios) : IRealBoqSelector
{
    public async Task<IReadOnlyDictionary<Guid, Guid>> ResolveAsync(
        Guid projectId, ComparisonBasis basis, CancellationToken cancellationToken = default) =>
        basis switch
        {
            ComparisonBasis.Decided => await ResolveDecidedAsync(new ProjectId(projectId), cancellationToken),
            ComparisonBasis.Scenario scenario => await ResolveScenarioAsync(scenario.CostScenarioId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, "Unknown comparison basis.")
        };

    // Decided: the accepted contract's BoQ, else the selected bid's BoQ; a work package with neither is absent.
    private async Task<IReadOnlyDictionary<Guid, Guid>> ResolveDecidedAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, Guid>();
        var packages = await workPackages.ListByProjectAsync(projectId, cancellationToken);

        foreach (var wp in packages)
        {
            Guid? boqId = null;

            // Prefer the awarded contract's accepted BoQ — the authoritative "what we committed to".
            if (wp.AwardedContractId is not null)
            {
                var contract = await contracts.GetByWorkPackageAsync(wp.Id, cancellationToken);
                if (contract is not null)
                {
                    boqId = contract.AcceptedBoqId.Value;
                }
            }

            // Fall back to the selected bid's BoQ (a decision made, not yet awarded).
            if (boqId is null)
            {
                var wpBids = await bids.ListByWorkPackageAsync(wp.Id, cancellationToken);
                var selected = wpBids.FirstOrDefault(b => b.Status == BidStatus.Selected);
                if (selected is not null)
                {
                    var boq = await billsOfQuantities.GetByBidAsync(selected.Id, cancellationToken);
                    if (boq is not null)
                    {
                        boqId = boq.Id.Value;
                    }
                }
            }

            if (boqId is { } value)
            {
                result[wp.Id.Value] = value;
            }
        }

        return result;
    }

    // Scenario: the scenario's chosen bid per work package → that bid's current BoQ.
    private async Task<IReadOnlyDictionary<Guid, Guid>> ResolveScenarioAsync(Guid costScenarioId, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, Guid>();
        var scenario = await scenarios.GetAsync(new CostScenarioId(costScenarioId), cancellationToken);
        if (scenario is null)
        {
            return result;
        }

        foreach (var selection in scenario.Selections)
        {
            var boq = await billsOfQuantities.GetByBidAsync(selection.BidId, cancellationToken);
            if (boq is not null)
            {
                result[selection.WorkPackageId.Value] = boq.Id.Value;
            }
        }

        return result;
    }
}
