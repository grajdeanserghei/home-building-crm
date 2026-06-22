using HomeProjectManagement.Domain.Bids.Events;
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
/// <see cref="DiscussionNote"/> log as internal entities. There is at most one bid per
/// (work package, contractor) pair, and at most one bid per work package may be
/// <see cref="BidStatus.Selected"/> — both guarded by the application service and unique
/// database indexes, since they span more than this single aggregate instance. State changes go
/// through intention-revealing methods that enforce invariants; construction goes through the
/// <see cref="Open"/> factory.
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

    /// <summary>Short free-text summary/standing of the bid. Optional.</summary>
    public string? Summary { get; private set; }

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
    /// Factory: open a contractor's bid on a work package, validating its invariants.
    /// <paramref name="now"/> is supplied by the caller (from <c>TimeProvider</c>) rather than
    /// read inside the domain. A freshly opened bid starts <see cref="BidStatus.InDiscussion"/>.
    /// The "one bid per work-package/contractor pair" rule spans the set of bids and is enforced
    /// by the application service plus a unique index, not here.
    /// </summary>
    public static Bid Open(
        WorkPackageId workPackageId,
        ContractorId contractorId,
        DateTimeOffset now,
        DateTimeOffset? firstContactedOn = null,
        string? summary = null)
    {
        var bid = new Bid(BidId.New(), workPackageId, contractorId)
        {
            Status = BidStatus.InDiscussion,
            FirstContactedOn = firstContactedOn,
            Summary = Trim(summary)
        };

        bid.Raise(new BidOpened(bid.Id, workPackageId, contractorId, now));
        return bid;
    }

    /// <summary>Update the free-text summary/standing of the bid.</summary>
    public void Summarize(string? summary) => Summary = Trim(summary);

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

    /// <summary>Record that the contractor has submitted a priced quote.</summary>
    public void MarkQuoted(DateTimeOffset now) => TransitionTo(BidStatus.Quoted, now);

    /// <summary>Keep the bid in contention as a serious candidate.</summary>
    public void Shortlist(DateTimeOffset now) => TransitionTo(BidStatus.Shortlisted, now);

    /// <summary>Reject the bid (not chosen).</summary>
    public void Reject(DateTimeOffset now) => TransitionTo(BidStatus.Rejected, now);

    /// <summary>Record that the contractor pulled out of the selection.</summary>
    public void Withdraw(DateTimeOffset now) => TransitionTo(BidStatus.Withdrawn, now);

    /// <summary>
    /// Select this bid as the winner of its work package. The cross-aggregate award flow
    /// (rejecting the competing bids, creating the contract, transitioning the work package) is
    /// coordinated by an application service; this method is the bid's own part of it. A
    /// withdrawn or rejected bid cannot be selected.
    /// </summary>
    public void Select(DateTimeOffset now)
    {
        if (Status is BidStatus.Withdrawn or BidStatus.Rejected)
        {
            throw new DomainConflictException($"A {Status} bid cannot be selected.");
        }

        TransitionTo(BidStatus.Selected, now);
    }

    private void TransitionTo(BidStatus status, DateTimeOffset now)
    {
        if (status == Status)
        {
            return;
        }

        if (Status == BidStatus.Withdrawn)
        {
            throw new DomainConflictException("A withdrawn bid is closed and cannot change status.");
        }

        var previous = Status;
        Status = status;
        Raise(new BidStatusChanged(Id, previous, status, now));
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
