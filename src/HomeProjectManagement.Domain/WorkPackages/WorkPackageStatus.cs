namespace HomeProjectManagement.Domain.WorkPackages;

/// <summary>
/// Lifecycle of a work package, from definition through award to completion. Persisted as
/// its string name. See the domain model's <c>WorkPackageStatus</c> enum.
/// </summary>
public enum WorkPackageStatus
{
    /// <summary>Scope defined up front, before any contractor is sought.</summary>
    Defined,

    /// <summary>Actively soliciting and comparing bids.</summary>
    OpenForBids,

    /// <summary>A bid was selected and a contract created; <c>AwardedContractId</c> is set.</summary>
    Awarded,

    /// <summary>Work is underway under the awarded contract.</summary>
    InProgress,

    /// <summary>Work is finished.</summary>
    Completed,

    /// <summary>Abandoned without award/completion.</summary>
    Cancelled
}
