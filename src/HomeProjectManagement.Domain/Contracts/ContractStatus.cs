namespace HomeProjectManagement.Domain.Contracts;

/// <summary>
/// Lifecycle of a contract awarded for a work package. Persisted as its string name. See the
/// domain model's <c>ContractStatus</c> enum.
/// </summary>
public enum ContractStatus
{
    /// <summary>Drawn up from the accepted BoQ but not yet signed; terms may still change.</summary>
    Draft,

    /// <summary>Signed by both parties. <c>SignedOn</c> is recorded.</summary>
    Signed,

    /// <summary>The contracted work is underway.</summary>
    Active,

    /// <summary>The work is finished. <c>ActualEndDate</c> is recorded. Terminal.</summary>
    Completed,

    /// <summary>Ended early (cancelled by either party). Terminal.</summary>
    Terminated
}
