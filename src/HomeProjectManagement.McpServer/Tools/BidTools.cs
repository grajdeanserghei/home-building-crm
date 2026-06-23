using System.ComponentModel;
using HomeProjectManagement.Application.Bids;
using HomeProjectManagement.Domain.Bids;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HomeProjectManagement.McpServer.Tools;

/// <summary>
/// The conversational-CRM bid surface: open a bid linking a contractor to a work package, append
/// dated discussion notes, move the bid's status (including the expected-BoQ date), and read bids
/// back. A bid is the engagement — it carries the conversation, the status, and the expected dates.
/// Thin wrappers over <see cref="IBidAppService"/>.
/// </summary>
[McpServerToolType]
public static class BidTools
{
    [McpServerTool(Name = "open_bid"), Description(
        "Open a bid linking a contractor to a work package (one bid per contractor per work package). " +
        "A fresh bid starts InDiscussion. Optionally records an opening discussion note — use this for " +
        "the lead's provenance, e.g. \"Potential contractor, recommended by Luci\". Resolve the " +
        "workPackageId via list_work_packages and the contractorId via list_contractors/register_contractor " +
        "first. Returns the created bid including its bidId.")]
    public static async Task<BidDto> OpenBid(
        IBidAppService service,
        [Description("The work package the contractor is bidding on.")] Guid workPackageId,
        [Description("The participating contractor.")] Guid contractorId,
        [Description("Date discussions began (absolute ISO timestamp), if known.")] DateTimeOffset? firstContactedOn = null,
        [Description("Short standing/summary of the bid, if any.")] string? summary = null,
        [Description("Optional opening note text (e.g. how the lead surfaced).")] string? openingNote = null,
        [Description("Type of the opening note: Meeting, Call, Email, or Note. Defaults to Note.")] NoteType openingNoteType = NoteType.Note,
        [Description("When the opening note's interaction occurred (absolute ISO timestamp). Defaults to now if a note is given.")] DateTimeOffset? openingNoteOccurredOn = null,
        CancellationToken ct = default)
    {
        var opened = await service.OpenAsync(workPackageId, new OpenBidCommand(contractorId, firstContactedOn, summary), ct)
                     ?? throw new McpException(
                         $"Could not open the bid: work package {workPackageId} or contractor {contractorId} was not found.");

        if (string.IsNullOrWhiteSpace(openingNote))
        {
            return opened;
        }

        var noteCommand = new LogDiscussionNoteCommand(
            openingNoteType, openingNoteOccurredOn ?? DateTimeOffset.UtcNow, openingNote);
        return await service.LogNoteAsync(opened.Id, noteCommand, ct) ?? opened;
    }

    [McpServerTool(Name = "add_bid_note"), Description(
        "Append a dated discussion note (a call, meeting, email, or remark) to a bid's append-only log. " +
        "occurredOn is when the interaction actually happened — resolve any relative reference (\"yesterday\") " +
        "to an absolute ISO timestamp yourself before calling. Returns the updated bid with its full note history.")]
    public static async Task<BidDto> AddBidNote(
        IBidAppService service,
        [Description("The bid id.")] Guid bidId,
        [Description("Note type: Meeting, Call, Email, or Note.")] NoteType type,
        [Description("The note text.")] string text,
        [Description("When the interaction occurred (absolute ISO timestamp).")] DateTimeOffset occurredOn,
        CancellationToken ct = default)
        => await service.LogNoteAsync(bidId, new LogDiscussionNoteCommand(type, occurredOn, text), ct)
           ?? throw new McpException($"No bid exists with id {bidId}.");

    [McpServerTool(Name = "set_bid_status"), Description(
        "Move a bid's status. Legal values: InDiscussion, BoqExpected, BoqReceived, Shortlisted, Rejected, " +
        "Withdrawn. When moving to BoqExpected, pass expectedBoqDate — the absolute date the contractor " +
        "committed to send the BoQ (resolve \"Monday next week\" yourself). Selected is NOT set here; it " +
        "happens only when a BoQ is awarded. Returns the updated bid.")]
    public static async Task<BidDto> SetBidStatus(
        IBidAppService service,
        [Description("The bid id.")] Guid bidId,
        [Description("Target status (not Selected).")] BidStatus status,
        [Description("Absolute date the contractor committed to send the BoQ (set with BoqExpected).")] DateTimeOffset? expectedBoqDate = null,
        CancellationToken ct = default)
        => await service.ChangeStatusAsync(bidId, new ChangeBidStatusCommand(status, expectedBoqDate), ct)
           ?? throw new McpException($"No bid exists with id {bidId}.");

    [McpServerTool(Name = "list_bids"), Description(
        "List bids, filtered by EITHER a work package OR a contractor (provide exactly one). Use it to " +
        "resolve a bidId before adding notes or changing status.")]
    public static async Task<IReadOnlyList<BidDto>> ListBids(
        IBidAppService service,
        [Description("List the bids on this work package.")] Guid? workPackageId = null,
        [Description("List the bids submitted by this contractor.")] Guid? contractorId = null,
        CancellationToken ct = default)
    {
        if (workPackageId is { } wp && contractorId is null)
        {
            return await service.ListByWorkPackageAsync(wp, ct);
        }

        if (contractorId is { } c && workPackageId is null)
        {
            return await service.ListByContractorAsync(c, ct);
        }

        throw new McpException("Provide exactly one of workPackageId or contractorId.");
    }

    [McpServerTool(Name = "get_bid"), Description(
        "Read a single bid with its full discussion-note history and current status.")]
    public static async Task<BidDto> GetBid(
        IBidAppService service,
        [Description("The bid id.")] Guid bidId,
        CancellationToken ct = default)
        => await service.GetAsync(bidId, ct)
           ?? throw new McpException($"No bid exists with id {bidId}.");
}
