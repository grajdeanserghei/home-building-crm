using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Projects;

namespace HomeProjectManagement.Domain.CostScenarios.Events;

/// <summary>Raised when a new cost scenario is created within a project.</summary>
public sealed record CostScenarioCreated(
    CostScenarioId CostScenarioId,
    ProjectId ProjectId,
    string Name,
    DateTimeOffset OccurredOn) : IDomainEvent;
