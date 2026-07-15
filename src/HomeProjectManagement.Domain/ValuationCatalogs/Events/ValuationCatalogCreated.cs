using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;

namespace HomeProjectManagement.Domain.ValuationCatalogs.Events;

/// <summary>Raised when a new valuation catalog (the appraiser's estimate baseline) is created for a project.</summary>
public sealed record ValuationCatalogCreated(
    ValuationCatalogId ValuationCatalogId,
    ProjectId ProjectId,
    DateTimeOffset OccurredOn) : IDomainEvent;
