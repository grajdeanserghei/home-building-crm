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
    /// <summary>The single BoQ for a bid, or null if none has been drafted yet (at most one per bid).</summary>
    Task<BillOfQuantitiesDto?> GetByBidAsync(Guid bidId, CancellationToken cancellationToken = default);

    Task<BillOfQuantitiesDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Draft the BoQ for a bid. Returns null if the bid does not exist. There is at most one BoQ per
    /// bid: re-drafting with the same source content hash returns the existing BoQ (idempotent), and
    /// drafting a different quote for a bid that already has one is rejected (409) — use
    /// <see cref="ReplaceContentsAsync"/> to supersede it instead.
    /// </summary>
    Task<BillOfQuantitiesDto?> DraftAsync(Guid bidId, DraftBillOfQuantitiesCommand command, CancellationToken cancellationToken = default);

    Task<BillOfQuantitiesDto?> UpdateAsync(Guid id, UpdateBillOfQuantitiesCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replace a BoQ's contents in place when a revised <c>deviz</c> supersedes it: clears its
    /// sections/subsections/line items and re-points the header + provenance, ready to re-ingest the
    /// new document. Returns null if the BoQ does not exist; throws (409) if it is no longer editable.
    /// </summary>
    Task<BillOfQuantitiesDto?> ReplaceContentsAsync(Guid id, ReplaceBoqContentsCommand command, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Move a line item to a target container (a section's direct list, or a subsection) at a 0-based
    /// index, anywhere within the BoQ — reordering it or moving it between subcapitols. The affected
    /// containers are renumbered densely; the line keeps its id and data. Returns the updated BoQ, or
    /// null if the BoQ does not exist; an absent line/target or a locked BoQ is reported as a domain
    /// exception (400/409).
    /// </summary>
    Task<BillOfQuantitiesDto?> MoveLineItemAsync(Guid id, MoveBoqLineItemCommand command, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Render the BoQ to an Excel workbook (one worksheet per section, subsections as visually
    /// separated bands, a leading summary sheet, live <c>SUM</c> totals). Read-only and allowed in
    /// every status. Returns null if the BoQ does not exist. The file name is built from the owning
    /// bid's contractor and work-package names.
    /// </summary>
    Task<BoqExportFile?> ExportAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
