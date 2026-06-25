using HomeProjectManagement.Domain.Bids.Events;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.WorkPackages;

namespace HomeProjectManagement.Domain.Bids;

/// <summary>
/// A contractor's participation in one work package's selection process. Opened when
/// discussions begin (possibly before any quote), it carries a status, records the discussion
/// notes, and groups the BoQ version(s) the contractor submits (those live in the separate Bill
/// of Quantities aggregate and reference this bid by id).
/// </summary>
/// <remarks>
/// Aggregate root. It references its <see cref="WorkPackageId"/> and <see cref="ContractorId"/>
/// <b>by identity</b>, never by holding the other aggregate, and owns its
/// <see cref="DiscussionNote"/> log as internal entities. A contractor may hold several bids on
/// the same work package (e.g. a "Premium" and a "Buget" variant, told apart by their
/// <see cref="Label"/>); at most one bid per work package may be <see cref="BidStatus.Selected"/>,
/// guarded by the application service and a filtered unique database index, since it spans more
/// than this single aggregate instance. State changes go through intention-revealing methods that
/// enforce invariants; construction goes through the <see cref="Open"/> factory.
/// </remarks>
public sealed class Bid : AggregateRoot<BidId>
{
    private readonly List<DiscussionNote> _notes = [];

    /// <summary>The work package being procured (by id).</summary>
    public WorkPackageId WorkPackageId { get; private set; }

    /// <summary>The participating contractor (by id).</summary>
    public ContractorId ContractorId { get; private set; }

    public BidStatus Status { get; private set; }

    /// <summary>When discussions began. Optional.</summary>
    public DateTimeOffset? FirstContactedOn { get; private set; }

    /// <summary>
    /// When the contractor committed to send a BoQ (set together with <see cref="BidStatus.BoqExpected"/>
    /// via <see cref="ExpectBoqBy"/>). A relative promise ("by Monday next week") is resolved to an
    /// absolute date by the caller before it reaches the domain — there is no clock in the domain. Optional.
    /// </summary>
    public DateTimeOffset? ExpectedBoqDate { get; private set; }

    /// <summary>Short free-text summary/standing of the bid. Optional.</summary>
    public string? Summary { get; private set; }

    /// <summary>
    /// Short variant title that tells apart several bids from the same contractor on one work
    /// package (e.g. "Premium", "Buget"). Optional.
    /// </summary>
    public string? Label { get; private set; }

    /// <summary>
    /// The discussion log (internal entities). Mutated only through <see cref="LogNote"/> and
    /// <see cref="RemoveNote"/>; EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<DiscussionNote> Notes => _notes.AsReadOnly();

    // EF Core materialisation constructor.
    private Bid()
    {
    }

    private Bid(BidId id, WorkPackageId workPackageId, ContractorId contractorId) : base(id)
    {
        Id = id;
        WorkPackageId = workPackageId;
        ContractorId = contractorId;
    }

    /// <summary>
    /// Factory: open a contractor's bid on a work package. <paramref name="now"/> is supplied by
    /// the caller (from <c>TimeProvider</c>) rather than read inside the domain. A freshly opened
    /// bid starts <see cref="BidStatus.InDiscussion"/>.
    /// </summary>
    public static Bid Open(
        WorkPackageId workPackageId,
        ContractorId contractorId,
        DateTimeOffset now,
        DateTimeOffset? firstContactedOn = null,
        string? summary = null,
        string? label = null)
    {
        var bid = new Bid(BidId.New(), workPackageId, contractorId)
        {
            Status = BidStatus.InDiscussion,
            FirstContactedOn = firstContactedOn,
            Summary = Trim(summary),
            Label = Trim(label)
        };

        bid.Raise(new BidOpened(bid.Id, workPackageId, contractorId, now));
        return bid;
    }

    /// <summary>
    /// Factory: open a fresh bid copied from an existing one — same work package and contractor,
    /// carrying over its summary, first-contact date and label. The copy starts a new selection:
    /// status <see cref="BidStatus.InDiscussion"/>, an empty discussion log, no expected-BoQ date
    /// and no BoQ link. Callers adjust the <see cref="Label"/> afterwards (e.g. a "(copie)" suffix).
    /// </summary>
    public static Bid DuplicateFrom(Bid source, DateTimeOffset now) =>
        Open(source.WorkPackageId, source.ContractorId, now, source.FirstContactedOn, source.Summary, source.Label);

    /// <summary>Update the free-text summary/standing of the bid.</summary>
    public void Summarize(string? summary) => Summary = Trim(summary);

    /// <summary>Set or clear the bid's variant label.</summary>
    public void Relabel(string? label) => Label = Trim(label);

    /// <summary>Set or clear the date discussions began.</summary>
    public void SetFirstContact(DateTimeOffset? firstContactedOn) => FirstContactedOn = firstContactedOn;

    /// <summary>
    /// Append a note to the discussion log and return it. <paramref name="now"/> stamps the
    /// raised event; <paramref name="occurredOn"/> is when the interaction itself happened.
    /// </summary>
    public DiscussionNote LogNote(
        NoteType type,
        DateTimeOffset occurredOn,
        UserId authorId,
        string content,
        DateTimeOffset now)
    {
        var note = new DiscussionNote(DiscussionNoteId.New(), type, occurredOn, authorId, content);
        _notes.Add(note);
        Raise(new DiscussionNoteLogged(Id, note.Id, type, now));
        return note;
    }

    /// <summary>Remove a note from the discussion log. Returns false if no such note exists.</summary>
    public bool RemoveNote(DiscussionNoteId noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note is null)
        {
            return false;
        }

        _notes.Remove(note);
        return true;
    }

    /// <summary>
    /// Transition to a new status, raising an event if it changed. Reaching
    /// <see cref="BidStatus.Selected"/> is not allowed here — selection awards the work package
    /// and rejects the competing bids, so it must go through <see cref="Select"/> (coordinated
    /// by the application service).
    /// </summary>
    public void ChangeStatus(BidStatus status, DateTimeOffset now)
    {
        if (status == BidStatus.Selected)
        {
            throw new DomainConflictException(
                "A bid becomes Selected only via Select, which is coordinated with the competing bids.");
        }

        TransitionTo(status, now);
    }

    /// <summary>
    /// Record that the contractor committed to send a priced BoQ by <paramref name="expectedBoqDate"/>
    /// (already resolved to an absolute date by the caller), moving the bid to
    /// <see cref="BidStatus.BoqExpected"/> and raising <see cref="BidBoqExpected"/>. When the bid is
    /// already <see cref="BidStatus.BoqExpected"/> this just revises the committed date.
    /// </summary>
    public void ExpectBoqBy(DateTimeOffset? expectedBoqDate, DateTimeOffset now)
    {
        if (Status == BidStatus.BoqExpected)
        {
            ExpectedBoqDate = expectedBoqDate;
            return;
        }

        EnsureCanTransitionTo(BidStatus.BoqExpected);
        Status = BidStatus.BoqExpected;
        ExpectedBoqDate = expectedBoqDate;
        Raise(new BidBoqExpected(Id, expectedBoqDate, now));
    }

    /// <summary>
    /// Record receipt of a priced BoQ, moving the bid to <see cref="BidStatus.BoqReceived"/> and
    /// raising <see cref="BidBoqReceived"/>. The BoQ↔bid link is carried canonically by
    /// <c>BillOfQuantities.BidId</c> (a bid may hold several BoQ versions), so this records
    /// <i>receipt</i> via the status transition rather than storing a single BoQ pointer on the bid.
    /// </summary>
    public void LinkBoq(BoqId boqId, DateTimeOffset now)
    {
        if (Status == BidStatus.BoqReceived)
        {
            return;
        }

        EnsureCanTransitionTo(BidStatus.BoqReceived);
        Status = BidStatus.BoqReceived;
        Raise(new BidBoqReceived(Id, boqId, now));
    }

    /// <summary>Keep the bid in contention as a serious candidate.</summary>
    public void Shortlist(DateTimeOffset now) => TransitionTo(BidStatus.Shortlisted, now);

    /// <summary>Reject the bid (not chosen).</summary>
    public void Reject(DateTimeOffset now) => TransitionTo(BidStatus.Rejected, now);

    /// <summary>Record that the contractor pulled out of the selection.</summary>
    public void Withdraw(DateTimeOffset now) => TransitionTo(BidStatus.Withdrawn, now);

    /// <summary>
    /// Select this bid as the winner of its work package. The cross-aggregate award flow
    /// (rejecting the competing bids, creating the contract, transitioning the work package) is
    /// coordinated by an application service; this method is the bid's own part of it. Selection is
    /// reachable from any non-terminal status; a withdrawn, rejected, or already-selected bid cannot
    /// be selected.
    /// </summary>
    public void Select(DateTimeOffset now)
    {
        if (Status == BidStatus.Selected)
        {
            return;
        }

        EnsureCanTransitionTo(BidStatus.Selected);
        var previous = Status;
        Status = BidStatus.Selected;
        Raise(new BidStatusChanged(Id, previous, BidStatus.Selected, now));
    }

    private void TransitionTo(BidStatus status, DateTimeOffset now)
    {
        if (status == Status)
        {
            return;
        }

        EnsureCanTransitionTo(status);
        var previous = Status;
        Status = status;
        Raise(new BidStatusChanged(Id, previous, status, now));
    }

    /// <summary>
    /// Legal status transitions. <see cref="BidStatus.Selected"/> is included for every non-terminal
    /// source (it is reached via <see cref="Select"/>, never <see cref="ChangeStatus"/>);
    /// <see cref="BidStatus.Selected"/>, <see cref="BidStatus.Rejected"/> and
    /// <see cref="BidStatus.Withdrawn"/> are terminal and appear as no key.
    /// </summary>
    private static readonly IReadOnlyDictionary<BidStatus, BidStatus[]> AllowedTransitions =
        new Dictionary<BidStatus, BidStatus[]>
        {
            [BidStatus.InDiscussion] =
            [
                BidStatus.BoqExpected, BidStatus.BoqReceived, BidStatus.Shortlisted,
                BidStatus.Selected, BidStatus.Rejected, BidStatus.Withdrawn
            ],
            [BidStatus.BoqExpected] =
            [
                BidStatus.BoqReceived, BidStatus.InDiscussion,
                BidStatus.Selected, BidStatus.Rejected, BidStatus.Withdrawn
            ],
            [BidStatus.BoqReceived] =
            [
                BidStatus.Shortlisted, BidStatus.Selected, BidStatus.Rejected, BidStatus.Withdrawn
            ],
            [BidStatus.Shortlisted] =
            [
                BidStatus.Selected, BidStatus.Rejected, BidStatus.Withdrawn
            ],
        };

    private void EnsureCanTransitionTo(BidStatus target)
    {
        if (Status == target)
        {
            return;
        }

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(target))
        {
            throw new DomainConflictException(
                $"A bid cannot move from {Status} to {target}.",
                code: "BidInvalidStatusTransition",
                parameters: new Dictionary<string, object?>
                {
                    ["from"] = Status.ToString(),
                    ["to"] = target.ToString(),
                });
        }
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
