namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// Standing of a contractor's priced quote (<c>deviz</c>) within a bid. Persisted as its string
/// name. See the domain model's <c>BoqStatus</c> enum.
/// </summary>
public enum BoqStatus
{
    /// <summary>Being entered/negotiated; freely editable.</summary>
    Draft,

    /// <summary>Handed over by the contractor as a firm quote; still editable while in review.</summary>
    Submitted,

    /// <summary>Accepted — a contract is created from this BoQ. Locked against structural edits.</summary>
    Accepted,

    /// <summary>Turned down. Terminal.</summary>
    Rejected,

    /// <summary>Pulled back by the contractor or superseded by a later version. Terminal.</summary>
    Withdrawn
}
