using HomeProjectManagement.Domain.BillsOfQuantities.Events;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A contractor's priced, itemized cost estimate (<c>deviz</c>) submitted within a bid. It is
/// organised into <see cref="Section"/>s of <see cref="LineItem"/>s, priced in a single
/// <see cref="PricingCurrency"/>. There is at most one BoQ per bid; a revised <c>deviz</c> replaces
/// its contents in place (see <see cref="ReplaceContents"/>) rather than creating another version.
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
    /// Provenance: a reference to the source <c>deviz</c> document the line items were extracted from
    /// (by the agent — the server never parses PDFs). Optional.
    /// </summary>
    public DocumentReference? SourceDocument { get; private set; }

    /// <summary>
    /// SHA-256 of the source document, computed by the agent. Used by the application service to make
    /// ingestion idempotent (re-running an interrupted ingestion returns the existing BoQ rather than
    /// duplicating) and kept for audit. Optional.
    /// </summary>
    public string? SourceContentHash { get; private set; }

    /// <summary>
    /// The sections of this BoQ (internal entities). Mutated only through the aggregate's methods;
    /// EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<Section> Sections => _sections.AsReadOnly();

    /// <summary>Derived net total (VAT-exclusive): the sum of the section subtotals, in the pricing currency.</summary>
    public Money Total =>
        _sections.Aggregate(Money.Zero(PricingCurrency), (sum, section) => sum.Add(section.Subtotal));

    /// <summary>Derived gross total (VAT-inclusive): the sum of the section VAT-inclusive subtotals.</summary>
    public Money TotalWithVat =>
        _sections.Aggregate(Money.Zero(PricingCurrency), (sum, section) => sum.Add(section.SubtotalWithVat));

    // EF Core materialisation constructor.
    private BillOfQuantities()
    {
    }

    private BillOfQuantities(BoqId id, BidId bidId, Currency pricingCurrency) : base(id)
    {
        Id = id;
        BidId = bidId;
        PricingCurrency = pricingCurrency;
    }

    /// <summary>
    /// Factory: start the BoQ for a bid, validating its invariants. <paramref name="now"/> is
    /// supplied by the caller (from <c>TimeProvider</c>) rather than read inside the domain. A freshly
    /// drafted BoQ starts <see cref="BoqStatus.Draft"/> with no sections. There is at most one BoQ per
    /// bid (the application service enforces this); a revised <c>deviz</c> uses <see cref="ReplaceContents"/>.
    /// </summary>
    public static BillOfQuantities Draft(
        BidId bidId,
        Currency pricingCurrency,
        DateTimeOffset now,
        string? reference = null,
        ExchangeRate? exchangeRate = null,
        DateTimeOffset? submittedOn = null,
        DateTimeOffset? validUntil = null,
        DocumentReference? sourceDocument = null,
        string? sourceContentHash = null)
    {
        EnsureRateMatchesCurrency(exchangeRate, pricingCurrency);

        var boq = new BillOfQuantities(BoqId.New(), bidId, pricingCurrency)
        {
            Status = BoqStatus.Draft,
            Reference = Trim(reference),
            ExchangeRate = exchangeRate,
            SubmittedOn = submittedOn,
            ValidUntil = validUntil,
            SourceDocument = sourceDocument,
            SourceContentHash = NormalizeHash(sourceContentHash)
        };

        boq.Raise(new BillOfQuantitiesDrafted(boq.Id, bidId, now));
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

    /// <summary>
    /// Replace the quote's contents in place when a revised <c>deviz</c> supersedes it: drop every
    /// section (cascading to its subsections and line items) and re-point the provenance to the new
    /// source document, so the new <c>deviz</c> can be re-ingested onto the same BoQ. The pricing
    /// currency is fixed; header details are updated separately via <see cref="UpdateDetails"/>.
    /// Allowed only while the BoQ is still editable (<see cref="BoqStatus.Draft"/> or
    /// <see cref="BoqStatus.Submitted"/>).
    /// </summary>
    public void ReplaceContents(DocumentReference? sourceDocument, string? sourceContentHash, DateTimeOffset now)
    {
        EnsureMutable();

        _sections.Clear();
        SourceDocument = sourceDocument;
        SourceContentHash = NormalizeHash(sourceContentHash);

        Raise(new BillOfQuantitiesContentsReplaced(Id, BidId, now));
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
    /// The <paramref name="unitPrice"/> (net, VAT-exclusive) must be in the BoQ's pricing currency;
    /// the <paramref name="vatRate"/> applied to it is 21% by default.
    /// </summary>
    public LineItem? AddLineItem(
        SectionId sectionId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes = null)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section?.AddLineItem(description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
    }

    /// <summary>Revise a line item in place. Returns false if the section or line item is absent.</summary>
    public bool ReviseLineItem(
        SectionId sectionId,
        LineItemId lineItemId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section is not null
            && section.ReviseLineItem(lineItemId, description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
    }

    /// <summary>Remove a line item from a section. Returns false if the section or line item is absent.</summary>
    public bool RemoveLineItem(SectionId sectionId, LineItemId lineItemId)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section is not null && section.RemoveLineItem(lineItemId);
    }

    /// <summary>Add a subsection to a section and return it. Returns null if the section does not exist.</summary>
    public Subsection? AddSubsection(SectionId sectionId, string name, int sequence, string? description = null)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section?.AddSubsection(name, sequence, description);
    }

    /// <summary>Rename/reorder a subsection. Returns false if the section or subsection is absent.</summary>
    public bool UpdateSubsection(SectionId sectionId, SubsectionId subsectionId, string name, int sequence, string? description)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section is not null && section.UpdateSubsection(subsectionId, name, sequence, description);
    }

    /// <summary>Remove a subsection (and its line items). Returns false if the section or subsection is absent.</summary>
    public bool RemoveSubsection(SectionId sectionId, SubsectionId subsectionId)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section is not null && section.RemoveSubsection(subsectionId);
    }

    /// <summary>
    /// Add a priced line to a subsection and return it. Returns null if the section or subsection is
    /// absent. The same currency and VAT rules as a section-level line apply.
    /// </summary>
    public LineItem? AddSubsectionLineItem(
        SectionId sectionId,
        SubsectionId subsectionId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes = null)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section?.AddSubsectionLineItem(subsectionId, description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
    }

    /// <summary>Revise a line item inside a subsection. Returns false if the section, subsection, or line item is absent.</summary>
    public bool ReviseSubsectionLineItem(
        SectionId sectionId,
        SubsectionId subsectionId,
        LineItemId lineItemId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section is not null
            && section.ReviseSubsectionLineItem(subsectionId, lineItemId, description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
    }

    /// <summary>Remove a line item from a subsection. Returns false if the section, subsection, or line item is absent.</summary>
    public bool RemoveSubsectionLineItem(SectionId sectionId, SubsectionId subsectionId, LineItemId lineItemId)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        return section is not null && section.RemoveSubsectionLineItem(subsectionId, lineItemId);
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

    /// <summary>Withdraw the BoQ (the contractor pulled the quote).</summary>
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
            throw new DomainConflictException(
                $"A {Status} bill of quantities is closed and cannot change status.",
                code: "BoqClosed",
                parameters: new Dictionary<string, object?> { ["status"] = Status.ToString() });
        }

        var previous = Status;
        Status = status;
        Raise(new BillOfQuantitiesStatusChanged(Id, previous, status, now));
    }

    private void EnsureMutable()
    {
        if (Status is not (BoqStatus.Draft or BoqStatus.Submitted))
        {
            throw new DomainConflictException(
                $"A {Status} bill of quantities can no longer be edited.",
                code: "BoqNotEditable",
                parameters: new Dictionary<string, object?> { ["status"] = Status.ToString() });
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
            throw new DomainValidationException(
                $"The pinned exchange rate must involve the pricing currency ({pricingCurrency}).",
                code: "BoqExchangeRateCurrencyMismatch",
                parameters: new Dictionary<string, object?> { ["pricingCurrency"] = pricingCurrency.ToString() });
        }
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // The content hash is a hex SHA-256 digest; store it normalised so idempotency comparison is stable.
    private static string? NormalizeHash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
