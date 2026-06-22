using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.Bids.Events;

/// <summary>
/// Raised when a contractor's bid on a work package is opened — the start of that
/// contractor's participation in the work package's selection process.
/// </summary>
public sealed record BidOpened(
    BidId BidId,
    WorkPackageId WorkPackageId,
    ContractorId ContractorId,
    DateTimeOffset OccurredOn) : IDomainEvent;
