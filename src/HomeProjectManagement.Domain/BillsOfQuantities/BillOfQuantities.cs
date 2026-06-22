using HomeProjectManagement.Domain.BillsOfQuantities.Events;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A contractor's priced, itemized cost estimate (<c>deviz</c>) submitted within a bid. It is
/// organised into <see cref="Section"/>s of <see cref="LineItem"/>s, priced in a single
/// <see cref="PricingCurrency"/>, and a bid may hold several BoQ versions over negotiation.
/// </summary>
/// <remarks>
/// Aggregate root. It references its owning <see cref="BidId"/> <b>by identity</b> (the work
/// package and contractor are reached through the bid) and owns its sections and line items as
/// internal entities. Line items reference their <see cref="UnitOfMeasure"/> by id. All
/// <see cref="Money"/> amounts share the <see cref="PricingCurrency"/>; the <see cref="Total"/>
/// (and section subtotals and line totals) are <b>derived</b>, never stored. Construction goes
/// through the <see cref="Draft"/> factory; structural edits are allowed only while the BoQ is
/// <see cref="BoqStatus.Draft"/> or <see cref="BoqStatus.Submitted"/>.
/// </remarks>
public sealed class BillOfQuantities : AggregateRoot<BoqId>
{
    private readonly List<Section> _sections = [];

    /// <summary>The bid this BoQ belongs to (by id). Work package &amp; contractor are reached through it.</summary>
    public BidId BidId { get; private set; }

    /// <summary>The contractor's own <c>deviz</c> number/label. Optional.</summary>
    public string? Reference { get; private set; }

    /// <summary>BoQ revision within the bid (1, 2, …).</summary>
    public int Version { get; private set; }

    public BoqStatus Status { get; private set; }

    /// <summary>The currency every <see cref="Money"/> amount in this BoQ is stored in (RON or EUR).</summary>
    public Currency PricingCurrency { get; private set; }

    /// <summary>
    /// Pinned EUR↔RON rate so the other currency is derivable for side-by-side comparison. Optional
    /// until a rate is captured. When set, it must involve the <see cref="PricingCurrency"/>.
    /// </summary>
    public ExchangeRate? ExchangeRate { get; private set; }

    /// <summary>When the quote was received. Optional.</summary>
    public DateTimeOffset? SubmittedOn { get; private set; }

    /// <summary>Offer expiry. Optional.</summary>
    public DateTimeOffset? ValidUntil { get; private set; }

    /// <summary>
    /// The sections of this BoQ (internal entities). Mutated only through the aggregate's methods;
    /// EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<Section> Sections => _sections.AsReadOnly();

    /// <summary>Derived total: the sum of the section subtotals, in the pricing currency.</summary>
    public Money Total =>
        _sections.Aggregate(Money.Zero(PricingCurrency), (sum, section) => sum.Add(section.Subtotal));

    // EF Core materialisation constructor.
    private BillOfQuantities()
    {
    }

    private BillOfQuantities(BoqId id, BidId bidId, int version, Currency pricingCurrency) : base(id)
    {
        Id = id;
        BidId = bidId;
        Version = version;
        PricingCurrency = pricingCurrency;
    }

    /// <summary>
    /// Factory: start a new BoQ version for a bid, validating its invariants. <paramref name="now"/>
    /// is supplied by the caller (from <c>TimeProvider</c>) rather than read inside the domain. A
    /// freshly drafted BoQ starts <see cref="BoqStatus.Draft"/> with no sections. The
    /// <paramref name="version"/> sequence within the bid is assigned by the application service.
    /// </summary>
    public static BillOfQuantities Draft(
        BidId bidId,
        int version,
        Currency pricingCurrency,
        DateTimeOffset now,
        string? reference = null,
        ExchangeRate? exchangeRate = null,
        DateTimeOffset? submittedOn = null,
        DateTimeOffset? validUntil = null)
    {
        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Bill of Quantities version must be 1 or greater.");
        }

        EnsureRateMatchesCurrency(exchangeRate, pricingCurrency);

        var boq = new BillOfQuantities(BoqId.New(), bidId, version, pricingCurrency)
        {
            Status = BoqStatus.Draft,
            Reference = Trim(reference),
            ExchangeRate = exchangeRate,
            SubmittedOn = submittedOn,
            ValidUntil = validUntil
        };

        boq.Raise(new BillOfQuantitiesDrafted(boq.Id, bidId, version, now));
        return boq;
    }

    /// <summary>Update the BoQ's header details (reference, pinned rate, dates). The pricing currency is fixed.</summary>
    public void UpdateDetails(
        string? reference,
        ExchangeRate? exchangeRate,
        DateTimeOffset? submittedOn,
        DateTimeOffset? validUntil)
    {
        EnsureMutable();
        EnsureRateMatchesCurrency(exchangeRate, PricingCurrency);

        Reference = Trim(reference);
        ExchangeRate = exchangeRate;
        SubmittedOn = submittedOn;
        ValidUntil = validUntil;
    }

    /// <summary>Add a section to the BoQ and return it. The section inherits the pricing currency.</summary>
    public Section AddSection(string name, int sequence, string? description = null)
    {
        EnsureMutable();
        var section = new Section(SectionId.New(), name, sequence, description, PricingCurrency);
        _sections.Add(section);
        return section;
    }

    /// <summary>Rename/reorder a section. Returns false if no such section exists.</summary>
    public bool UpdateSection(SectionId sectionId, string name, int sequence, string? description)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        if (section is null)
        {
            return false;
        }

        section.Update(name, sequence, description);
        return true;
    }

    /// <summary>Remove a section (and its line items). Returns false if no such section exists.</summary>
    public bool RemoveSection(SectionId sectionId)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        if (section is null)
        {
            return false;
        }

        _sections.Remove(section);
        return true;
    }

    /// <summary>
    /// Add a priced line to a section and return it. Returns null if the section does not exist.
    /// The <paramref name="unitPrice"/> must be in the BoQ's pricing currency.
    /// </summary>
    public LineItem? AddLineItem(
        SectionId sectionId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        int sequence,
        string? notes = null)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section?.AddLineItem(description, quantity, unitOfMeasureId, unitPrice, sequence, notes);
    }

    /// <summary>Revise a line item in place. Returns false if the section or line item is absent.</summary>
    public bool ReviseLineItem(
        SectionId sectionId,
        LineItemId lineItemId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        int sequence,
        string? notes)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section is not null
            && section.ReviseLineItem(lineItemId, description, quantity, unitOfMeasureId, unitPrice, sequence, notes);
    }

    /// <summary>Remove a line item from a section. Returns false if the section or line item is absent.</summary>
    public bool RemoveLineItem(SectionId sectionId, LineItemId lineItemId)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section is not null && section.RemoveLineItem(lineItemId);
    }

    /// <summary>Mark the BoQ as submitted (a firm quote handed over by the contractor).</summary>
    public void Submit(DateTimeOffset now) => TransitionTo(BoqStatus.Submitted, now);

    /// <summary>
    /// Accept the BoQ — the basis for a contract. Locks it against further structural edits.
    /// (The cross-aggregate award flow is coordinated by an application service.)
    /// </summary>
    public void Accept(DateTimeOffset now) => TransitionTo(BoqStatus.Accepted, now);

    /// <summary>Reject the BoQ (not chosen).</summary>
    public void Reject(DateTimeOffset now) => TransitionTo(BoqStatus.Rejected, now);

    /// <summary>Withdraw the BoQ (pulled back or superseded by a later version).</summary>
    public void Withdraw(DateTimeOffset now) => TransitionTo(BoqStatus.Withdrawn, now);

    /// <summary>Transition the BoQ to a new status, raising an event if it changed.</summary>
    public void ChangeStatus(BoqStatus status, DateTimeOffset now) => TransitionTo(status, now);

    private void TransitionTo(BoqStatus status, DateTimeOffset now)
    {
        if (status == Status)
        {
            return;
        }

        if (Status is BoqStatus.Rejected or BoqStatus.Withdrawn)
        {
            throw new InvalidOperationException(
                $"A {Status} bill of quantities is closed and cannot change status.");
        }

        var previous = Status;
        Status = status;
        Raise(new BillOfQuantitiesStatusChanged(Id, previous, status, now));
    }

    private void EnsureMutable()
    {
        if (Status is not (BoqStatus.Draft or BoqStatus.Submitted))
        {
            throw new InvalidOperationException(
                $"A {Status} bill of quantities can no longer be edited.");
        }
    }

    private Section? FindSection(SectionId sectionId) =>
        _sections.FirstOrDefault(s => s.Id == sectionId);

    private static void EnsureRateMatchesCurrency(ExchangeRate? rate, Currency pricingCurrency)
    {
        if (rate is null)
        {
            return;
        }

        if (rate.BaseCurrency != pricingCurrency && rate.QuoteCurrency != pricingCurrency)
        {
            throw new InvalidOperationException(
                $"The pinned exchange rate must involve the pricing currency ({pricingCurrency}).");
        }
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
