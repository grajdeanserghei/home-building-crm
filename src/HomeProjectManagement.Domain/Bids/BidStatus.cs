namespace HomeProjectManagement.Domain.Bids;

/// <summary>
/// Standing of a contractor's participation in one work package's selection process.
/// Persisted as its string name. See the domain model's <c>BidStatus</c> enum.
/// </summary>
public enum BidStatus
{
    /// <summary>Discussions have begun, possibly before any priced quote exists.</summary>
    InDiscussion,

    /// <summary>The contractor has submitted at least one priced BoQ.</summary>
    Quoted,

    /// <summary>Kept in contention as a serious candidate.</summary>
    Shortlisted,

    /// <summary>Chosen for the work package; the contract is created from this bid's accepted BoQ.</summary>
    Selected,

    /// <summary>Not chosen.</summary>
    Rejected,

    /// <summary>The contractor pulled out of the selection. Terminal.</summary>
    Withdrawn
}
