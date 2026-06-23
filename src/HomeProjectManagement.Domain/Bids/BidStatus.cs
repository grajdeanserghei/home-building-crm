namespace HomeProjectManagement.Domain.Bids;

/// <summary>
/// Standing of a contractor's participation in one work package's selection process.
/// Persisted as its string name. See the domain model's <c>BidStatus</c> enum.
/// </summary>
public enum BidStatus
{
    /// <summary>Discussions have begun, possibly before any priced quote exists.</summary>
    InDiscussion,

    /// <summary>
    /// The contractor has committed to send a priced BoQ (a <c>deviz</c>) by an
    /// <see cref="Bid.ExpectedBoqDate"/>, but it has not arrived yet.
    /// </summary>
    BoqExpected,

    /// <summary>
    /// A priced BoQ has been received. Supersedes the former <c>Quoted</c> — a received priced
    /// BoQ <i>is</i> the quote.
    /// </summary>
    BoqReceived,

    /// <summary>Kept in contention as a serious candidate.</summary>
    Shortlisted,

    /// <summary>Chosen for the work package; the contract is created from this bid's accepted BoQ.</summary>
    Selected,

    /// <summary>Not chosen.</summary>
    Rejected,

    /// <summary>The contractor pulled out of the selection. Terminal.</summary>
    Withdrawn
}
