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

    /// <summary>
    /// Bulk-add priced lines to a section, normalising each line's free-text unit token onto an
    /// active canonical unit of measure. Resolvable lines are persisted; lines whose unit could not
    /// be matched are returned flagged (the batch is not failed by a single bad unit). The result's
    /// <c>Boq</c> is null only when the BoQ or section does not exist.
    /// </summary>
    Task<AddBoqLineItemsResult?> AddLineItemsAsync(Guid id, Guid sectionId, IReadOnlyList<BoqLineItemInput> items, CancellationToken cancellationToken = default);

    /// <summary>Revise a line item. Returns null if the BoQ, section, or line item is absent.</summary>
    Task<BillOfQuantitiesDto?> ReviseLineItemAsync(Guid id, Guid sectionId, Guid lineItemId, LineItemCommand command, CancellationToken cancellationToken = default);

    /// <summary>Remove a line item. Returns false if the BoQ, section, or line item is absent.</summary>
    Task<bool> RemoveLineItemAsync(Guid id, Guid sectionId, Guid lineItemId, CancellationToken cancellationToken = default);

    /// <summary>Add a subsection to a section. Returns null if the BoQ or section is absent.</summary>
    Task<BillOfQuantitiesDto?> AddSubsectionAsync(Guid id, Guid sectionId, SubsectionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Rename/reorder a subsection. Returns null if the BoQ, section, or subsection is absent.</summary>
    Task<BillOfQuantitiesDto?> UpdateSubsectionAsync(Guid id, Guid sectionId, Guid subsectionId, SubsectionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Remove a subsection (and its line items). Returns false if the BoQ, section, or subsection is absent.</summary>
    Task<bool> RemoveSubsectionAsync(Guid id, Guid sectionId, Guid subsectionId, CancellationToken cancellationToken = default);

    /// <summary>Add a priced line to a subsection. Returns null if the BoQ, section, or subsection is absent.</summary>
    Task<BillOfQuantitiesDto?> AddSubsectionLineItemAsync(Guid id, Guid sectionId, Guid subsectionId, LineItemCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-add priced lines to a subsection, normalising each line's free-text unit token onto an
    /// active canonical unit of measure (the subsection-level counterpart of <see cref="AddLineItemsAsync"/>).
    /// Resolvable lines persist; unmatched lines are returned flagged without failing the batch. The
    /// result's <c>Boq</c> is null only when the BoQ, section, or subsection does not exist.
    /// </summary>
    Task<AddBoqLineItemsResult?> AddSubsectionLineItemsAsync(Guid id, Guid sectionId, Guid subsectionId, IReadOnlyList<BoqLineItemInput> items, CancellationToken cancellationToken = default);

    /// <summary>Revise a line item inside a subsection. Returns null if the BoQ, section, subsection, or line item is absent.</summary>
    Task<BillOfQuantitiesDto?> ReviseSubsectionLineItemAsync(Guid id, Guid sectionId, Guid subsectionId, Guid lineItemId, LineItemCommand command, CancellationToken cancellationToken = default);

    /// <summary>Remove a line item from a subsection. Returns false if the BoQ, section, subsection, or line item is absent.</summary>
    Task<bool> RemoveSubsectionLineItemAsync(Guid id, Guid sectionId, Guid subsectionId, Guid lineItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a draft BoQ (Draft → Submitted, locking line edits) and record receipt on the owning
    /// bid (moving it to <c>BoqReceived</c> via <c>LinkBoq</c>). Returns null if the BoQ does not exist.
    /// </summary>
    Task<BillOfQuantitiesDto?> SubmitAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
