namespace HomeProjectManagement.Application.BillsOfQuantities;

/// <summary>
/// Driving (primary) port for Bill-of-Quantities use cases — capturing and pricing a contractor's
/// <c>deviz</c> within a bid. The minimal-API endpoints in ApiService are the adapter that calls
/// this; the host never touches EF Core or the domain directly. Methods that mutate structure or
/// price report invariant violations (e.g. an inactive unit, a currency mismatch, editing a closed
/// BoQ) as <see cref="InvalidOperationException"/>, which the endpoints map to HTTP 409.
/// </summary>
public interface IBillOfQuantitiesAppService
{
    /// <summary>The BoQ versions submitted within a bid.</summary>
    Task<IReadOnlyList<BillOfQuantitiesDto>> ListByBidAsync(Guid bidId, CancellationToken cancellationToken = default);

    Task<BillOfQuantitiesDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Draft a new BoQ version for a bid. Returns null if the bid does not exist. The version
    /// number is assigned automatically (next after the bid's existing versions).
    /// </summary>
    Task<BillOfQuantitiesDto?> DraftAsync(Guid bidId, DraftBillOfQuantitiesCommand command, CancellationToken cancellationToken = default);

    Task<BillOfQuantitiesDto?> UpdateAsync(Guid id, UpdateBillOfQuantitiesCommand command, CancellationToken cancellationToken = default);

    /// <summary>Transition the BoQ's status. Returns null if it does not exist.</summary>
    Task<BillOfQuantitiesDto?> ChangeStatusAsync(Guid id, ChangeBoqStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>Add a section. Returns null if the BoQ does not exist.</summary>
    Task<BillOfQuantitiesDto?> AddSectionAsync(Guid id, SectionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Rename/reorder a section. Returns null if the BoQ or section is absent.</summary>
    Task<BillOfQuantitiesDto?> UpdateSectionAsync(Guid id, Guid sectionId, SectionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Remove a section (and its line items). Returns false if the BoQ or section is absent.</summary>
    Task<bool> RemoveSectionAsync(Guid id, Guid sectionId, CancellationToken cancellationToken = default);

    /// <summary>Add a priced line to a section. Returns null if the BoQ or section is absent.</summary>
    Task<BillOfQuantitiesDto?> AddLineItemAsync(Guid id, Guid sectionId, LineItemCommand command, CancellationToken cancellationToken = default);

    /// <summary>Revise a line item. Returns null if the BoQ, section, or line item is absent.</summary>
    Task<BillOfQuantitiesDto?> ReviseLineItemAsync(Guid id, Guid sectionId, Guid lineItemId, LineItemCommand command, CancellationToken cancellationToken = default);

    /// <summary>Remove a line item. Returns false if the BoQ, section, or line item is absent.</summary>
    Task<bool> RemoveLineItemAsync(Guid id, Guid sectionId, Guid lineItemId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
