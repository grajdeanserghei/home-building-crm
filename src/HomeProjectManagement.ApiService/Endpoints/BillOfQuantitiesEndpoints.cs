using HomeProjectManagement.Application.BillsOfQuantities;

namespace HomeProjectManagement.ApiService.Endpoints;

/// <summary>
/// The driving (primary) adapter for bills of quantities: thin minimal-API endpoints that call
/// <see cref="IBillOfQuantitiesAppService"/> and return DTOs. A bid has at most one BoQ, reachable
/// under the bid; an individual BoQ is a root, addressable by its own id, with sub-resources for its
/// sections and line items. Domain rule violations (inactive unit, currency mismatch, editing a
/// closed BoQ, illegal status transition, a second BoQ for a bid) are raised as domain exceptions and
/// turned into ProblemDetails (validation → 400, conflict → 409) by the global exception handler, so
/// the endpoints stay thin.
/// </summary>
public static class BillOfQuantitiesEndpoints
{
    public static IEndpointRouteBuilder MapBillOfQuantitiesEndpoints(this IEndpointRouteBuilder app)
    {
        // Bid-scoped resource: get or draft the single BoQ for a bid.
        var byBid = app.MapGroup("/api/bids/{bidId:guid}/bills-of-quantities");

        byBid.MapGet("/",
            async (Guid bidId, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.GetByBidAsync(bidId, ct) is { } boq
                    ? Results.Ok(boq)
                    : Results.NotFound());

        byBid.MapPost("/",
            async (Guid bidId, DraftBillOfQuantitiesCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.DraftAsync(bidId, command, ct) is { } created
                    ? Results.Created($"/api/bills-of-quantities/{created.Id}", created)
                    : Results.NotFound());

        // Root-level item: a BoQ is an aggregate root, addressable by its own id.
        var boqs = app.MapGroup("/api/bills-of-quantities");

        boqs.MapGet("/{id:guid}",
            async (Guid id, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } boq
                    ? Results.Ok(boq)
                    : Results.NotFound());

        boqs.MapPut("/{id:guid}",
            async (Guid id, UpdateBillOfQuantitiesCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.UpdateAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        // Replace a BoQ's contents in place when a revised deviz supersedes it (clears sections,
        // re-points provenance), ready to re-ingest the new document onto the same BoQ.
        boqs.MapPost("/{id:guid}/replace-contents",
            async (Guid id, ReplaceBoqContentsCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.ReplaceContentsAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapPost("/{id:guid}/status",
            async (Guid id, ChangeBoqStatusCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.ChangeStatusAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapDelete("/{id:guid}",
            async (Guid id, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.DeleteAsync(id, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Export the whole BoQ to an Excel workbook (read-only; allowed in any status).
        boqs.MapGet("/{id:guid}/export",
            async (Guid id, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.ExportAsync(id, ct) is { } file
                    ? Results.File(file.Content, file.ContentType, file.FileName)
                    : Results.NotFound());

        // Sections: internal entities of the BoQ, addressed as sub-resources.
        boqs.MapPost("/{id:guid}/sections",
            async (Guid id, SectionCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.AddSectionAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapPut("/{id:guid}/sections/{sectionId:guid}",
            async (Guid id, Guid sectionId, SectionCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.UpdateSectionAsync(id, sectionId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapDelete("/{id:guid}/sections/{sectionId:guid}",
            async (Guid id, Guid sectionId, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.RemoveSectionAsync(id, sectionId, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Line items: priced rows within a section.
        boqs.MapPost("/{id:guid}/sections/{sectionId:guid}/line-items",
            async (Guid id, Guid sectionId, LineItemCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.AddLineItemAsync(id, sectionId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapPut("/{id:guid}/sections/{sectionId:guid}/line-items/{lineItemId:guid}",
            async (Guid id, Guid sectionId, Guid lineItemId, LineItemCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.ReviseLineItemAsync(id, sectionId, lineItemId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapDelete("/{id:guid}/sections/{sectionId:guid}/line-items/{lineItemId:guid}",
            async (Guid id, Guid sectionId, Guid lineItemId, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.RemoveLineItemAsync(id, sectionId, lineItemId, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Duplicate any line (section-direct or subsection) — keyed only by line id, which is unique within the BoQ.
        boqs.MapPost("/{id:guid}/line-items/{lineItemId:guid}/duplicate",
            async (Guid id, Guid lineItemId, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.DuplicateLineItemAsync(id, lineItemId, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        // Reorder a line, or move it between containers (a section's direct list or any subsection),
        // anywhere within the BoQ. One call per drop; returns the BoQ with its containers renumbered.
        boqs.MapPost("/{id:guid}/move-line-item",
            async (Guid id, MoveBoqLineItemCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.MoveLineItemAsync(id, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        // Subsections: an optional second level of grouping within a section.
        boqs.MapPost("/{id:guid}/sections/{sectionId:guid}/subsections",
            async (Guid id, Guid sectionId, SubsectionCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.AddSubsectionAsync(id, sectionId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapPut("/{id:guid}/sections/{sectionId:guid}/subsections/{subsectionId:guid}",
            async (Guid id, Guid sectionId, Guid subsectionId, SubsectionCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.UpdateSubsectionAsync(id, sectionId, subsectionId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapDelete("/{id:guid}/sections/{sectionId:guid}/subsections/{subsectionId:guid}",
            async (Guid id, Guid sectionId, Guid subsectionId, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.RemoveSubsectionAsync(id, sectionId, subsectionId, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        // Line items within a subsection.
        boqs.MapPost("/{id:guid}/sections/{sectionId:guid}/subsections/{subsectionId:guid}/line-items",
            async (Guid id, Guid sectionId, Guid subsectionId, LineItemCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.AddSubsectionLineItemAsync(id, sectionId, subsectionId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapPut("/{id:guid}/sections/{sectionId:guid}/subsections/{subsectionId:guid}/line-items/{lineItemId:guid}",
            async (Guid id, Guid sectionId, Guid subsectionId, Guid lineItemId, LineItemCommand command, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.ReviseSubsectionLineItemAsync(id, sectionId, subsectionId, lineItemId, command, ct) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound());

        boqs.MapDelete("/{id:guid}/sections/{sectionId:guid}/subsections/{subsectionId:guid}/line-items/{lineItemId:guid}",
            async (Guid id, Guid sectionId, Guid subsectionId, Guid lineItemId, IBillOfQuantitiesAppService service, CancellationToken ct) =>
                await service.RemoveSubsectionLineItemAsync(id, sectionId, subsectionId, lineItemId, ct)
                    ? Results.NoContent()
                    : Results.NotFound());

        return app;
    }
}
