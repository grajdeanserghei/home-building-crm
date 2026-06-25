using HomeProjectManagement.Domain.BillsOfQuantities.Events;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Domain.BillsOfQuantities;

/// <summary>
/// A contractor's priced, itemized cost estimate (<c>deviz</c>) submitted within a bid. It is
/// organised into <see cref="Section"/>s (and optional <see cref="Subsection"/>s) of
/// <see cref="LineItem"/>s, priced in a single <see cref="PricingCurrency"/>. There is at most one BoQ
/// per bid; a revised <c>deviz</c> replaces its contents in place (see <see cref="ReplaceContents"/>)
/// rather than creating another version.
/// </summary>
/// <remarks>
/// Aggregate root. It references its owning <see cref="BidId"/> <b>by identity</b> (the work package
/// and contractor are reached through the bid). It owns its <see cref="Section"/> headings (which in
/// turn own their <see cref="Subsection"/> headings) and, in <b>one flat collection</b>, every
/// <see cref="LineItem"/> — each line carrying the <see cref="Section"/> (and optional
/// <see cref="Subsection"/>) it is grouped under by id. Holding the lines flat makes moving a line
/// between containers a plain field update rather than a delete+insert across owned tables. Line items
/// reference their <see cref="UnitOfMeasure"/> by id. All <see cref="Money"/> amounts share the
/// <see cref="PricingCurrency"/>; the <see cref="Total"/> (and section/subsection subtotals and line
/// totals) are <b>derived</b>, never stored. Construction goes through the <see cref="Draft"/>
/// factory; structural edits are allowed only while the BoQ is <see cref="BoqStatus.Draft"/> or
/// <see cref="BoqStatus.Submitted"/>.
/// </remarks>
public sealed class BillOfQuantities : AggregateRoot<BoqId>
{
    private readonly List<Section> _sections = [];
    private readonly List<LineItem> _lineItems = [];

    /// <summary>The bid this BoQ belongs to (by id). Work package &amp; contractor are reached through it.</summary>
    public BidId BidId { get; private set; }

    /// <summary>The contractor's own <c>deviz</c> number/label. Optional.</summary>
    public string? Reference { get; private set; }

    /// <summary>
    /// What this quote is priced against, as the supplier provided it: the entire building, or a
    /// single apartment. Defaults to the whole building. When <see cref="BudgetScopeKind.PerApartment"/>,
    /// the cost for the whole build is the total times the project's apartment-unit count (see
    /// <see cref="EffectiveTotal"/>) — so a single per-apartment <c>deviz</c> covers every apartment
    /// without being duplicated.
    /// </summary>
    public BudgetScopeKind Scope { get; private set; }

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
    /// The sections of this BoQ (internal heading entities). Mutated only through the aggregate's
    /// methods; EF reaches the backing field directly.
    /// </summary>
    public IReadOnlyList<Section> Sections => _sections.AsReadOnly();

    /// <summary>
    /// Every line item in this BoQ, held flat (each tagged with its <see cref="Section"/> and optional
    /// <see cref="Subsection"/> by id). Mutated only through the aggregate's methods; EF reaches the
    /// backing field directly. Order is not significant here — read it grouped via
    /// <see cref="DirectLineItemsOf"/> / <see cref="LineItemsOf"/>, which sort by <c>Sequence</c>.
    /// </summary>
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>Derived net total (VAT-exclusive): the sum of every line total, in the pricing currency.</summary>
    public Money Total => Sum(_lineItems, li => li.LineTotal);

    /// <summary>Derived gross total (VAT-inclusive): the sum of every VAT-inclusive line total.</summary>
    public Money TotalWithVat => Sum(_lineItems, li => li.LineTotalWithVat);

    /// <summary>
    /// The cost multiplier for the whole build given the project's <paramref name="apartmentUnits"/>:
    /// the unit count for a <see cref="BudgetScopeKind.PerApartment"/> quote, otherwise 1. The count is
    /// supplied by the caller (it lives on the project) rather than read inside the domain.
    /// </summary>
    public int Multiplier(int apartmentUnits) =>
        Scope == BudgetScopeKind.PerApartment ? apartmentUnits : 1;

    /// <summary>The net (VAT-exclusive) total scaled to the whole build (<see cref="Total"/> × <see cref="Multiplier"/>).</summary>
    public Money EffectiveTotal(int apartmentUnits) => Total.Multiply(Multiplier(apartmentUnits));

    /// <summary>The gross (VAT-inclusive) total scaled to the whole build.</summary>
    public Money EffectiveTotalWithVat(int apartmentUnits) => TotalWithVat.Multiply(Multiplier(apartmentUnits));

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
        string? sourceContentHash = null,
        BudgetScopeKind scope = BudgetScopeKind.EntireBuilding)
    {
        EnsureRateMatchesCurrency(exchangeRate, pricingCurrency);

        var boq = new BillOfQuantities(BoqId.New(), bidId, pricingCurrency)
        {
            Status = BoqStatus.Draft,
            Reference = Trim(reference),
            Scope = scope,
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
    /// Set what this quote is priced against (the entire building or per apartment). Allowed only
    /// while the BoQ is still editable.
    /// </summary>
    public void AssignScope(BudgetScopeKind scope)
    {
        EnsureMutable();
        Scope = scope;
    }

    /// <summary>
    /// Replace the quote's contents in place when a revised <c>deviz</c> supersedes it: drop every
    /// section (and subsection) and every line item, and re-point the provenance to the new source
    /// document, so the new <c>deviz</c> can be re-ingested onto the same BoQ. The pricing currency is
    /// fixed; header details are updated separately via <see cref="UpdateDetails"/>. Allowed only while
    /// the BoQ is still editable (<see cref="BoqStatus.Draft"/> or <see cref="BoqStatus.Submitted"/>).
    /// </summary>
    public void ReplaceContents(DocumentReference? sourceDocument, string? sourceContentHash, DateTimeOffset now)
    {
        EnsureMutable();

        _lineItems.Clear();
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

    /// <summary>Remove a section, its subsections, and all of its line items. Returns false if no such section exists.</summary>
    public bool RemoveSection(SectionId sectionId)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        if (section is null)
        {
            return false;
        }

        // Every line under the section (direct or in one of its subsections) carries this SectionId.
        _lineItems.RemoveAll(li => li.SectionId == sectionId);
        _sections.Remove(section);
        return true;
    }

    /// <summary>
    /// Add a priced line directly to a section and return it. Returns null if the section does not
    /// exist. The <paramref name="unitPrice"/> (net, VAT-exclusive) must be in the BoQ's pricing
    /// currency; the <paramref name="vatRate"/> applied to it is 21% by default.
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
        if (FindSection(sectionId) is null)
        {
            return null;
        }

        return AddLine(sectionId, subsectionId: null, description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
    }

    /// <summary>Revise a line held directly in a section. Returns false if the section or line item is absent.</summary>
    public bool ReviseLineItem(
        SectionId sectionId,
        LineItemId lineItemId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes) =>
        Revise(
            _lineItems.FirstOrDefault(li => li.Id == lineItemId && li.SectionId == sectionId && li.SubsectionId is null),
            description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);

    /// <summary>Remove a line held directly in a section. Returns false if the section or line item is absent.</summary>
    public bool RemoveLineItem(SectionId sectionId, LineItemId lineItemId) =>
        Remove(_lineItems.FirstOrDefault(li => li.Id == lineItemId && li.SectionId == sectionId && li.SubsectionId is null));

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

    /// <summary>Remove a subsection and all of its line items. Returns false if the section or subsection is absent.</summary>
    public bool RemoveSubsection(SectionId sectionId, SubsectionId subsectionId)
    {
        EnsureMutable();
        var section = FindSection(sectionId);
        if (section is null || !section.RemoveSubsection(subsectionId))
        {
            return false;
        }

        _lineItems.RemoveAll(li => li.SubsectionId == subsectionId);
        return true;
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
        if (FindSection(sectionId)?.FindSubsection(subsectionId) is null)
        {
            return null;
        }

        return AddLine(sectionId, subsectionId, description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
    }

    /// <summary>Revise a line inside a subsection. Returns false if the section, subsection, or line item is absent.</summary>
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
        string? notes) =>
        Revise(
            _lineItems.FirstOrDefault(li => li.Id == lineItemId && li.SectionId == sectionId && li.SubsectionId == subsectionId),
            description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);

    /// <summary>Remove a line from a subsection. Returns false if the section, subsection, or line item is absent.</summary>
    public bool RemoveSubsectionLineItem(SectionId sectionId, SubsectionId subsectionId, LineItemId lineItemId) =>
        Remove(_lineItems.FirstOrDefault(li => li.Id == lineItemId && li.SectionId == sectionId && li.SubsectionId == subsectionId));

    /// <summary>
    /// Duplicate a line item: create an identical copy (new id, same priced data) and place it
    /// directly below the source within the same container, renumbering that container dense 1..N.
    /// Returns the new line, or null if no line with that id exists. Allowed only while editable.
    /// </summary>
    public LineItem? DuplicateLineItem(LineItemId lineItemId)
    {
        EnsureMutable();

        var source = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (source is null)
        {
            return null;
        }

        // Clone the owned value objects into fresh instances — an EF owned entity (UnitPrice, VatRate)
        // cannot be shared between two owners, so the copy must not reuse the source's instances.
        var copy = AddLine(
            source.SectionId, source.SubsectionId, source.Description, source.Quantity,
            source.UnitOfMeasureId,
            new Money(source.UnitPrice.Amount, source.UnitPrice.Currency),
            new VatRate(source.VatRate.Percentage),
            source.Sequence, source.Notes);

        var container = _lineItems
            .Where(li => li.SectionId == source.SectionId && li.SubsectionId == source.SubsectionId && li.Id != copy.Id)
            .OrderBy(li => li.Sequence)
            .ToList();
        var sourceIndex = container.FindIndex(li => li.Id == source.Id);
        PlaceInContainer(copy, source.SectionId, source.SubsectionId, sourceIndex + 1);

        return copy;
    }

    /// <summary>
    /// Move a line item to a target container — a section's direct list when
    /// <paramref name="targetSubsectionId"/> is null, otherwise the named subsection — and place it at
    /// <paramref name="targetIndex"/> (0-based, clamped to the container's size). The line keeps its id
    /// and priced data; the source and target containers are renumbered to a dense 1..N
    /// <see cref="LineItem.Sequence"/>. A move within the same container is a pure reorder. Allowed only
    /// while the BoQ is editable. Throws if the line, target section, or target subsection is absent.
    /// </summary>
    public void MoveLineItem(
        LineItemId lineItemId,
        SectionId targetSectionId,
        SubsectionId? targetSubsectionId,
        int targetIndex,
        DateTimeOffset now)
    {
        EnsureMutable();

        var line = _lineItems.FirstOrDefault(li => li.Id == lineItemId)
            ?? throw new DomainValidationException(
                "The line item does not exist in this bill of quantities.",
                code: "BoqLineItemNotFound",
                parameters: new Dictionary<string, object?> { ["lineItemId"] = lineItemId.Value });

        var targetSection = FindSection(targetSectionId)
            ?? throw new DomainValidationException(
                "The target section does not exist in this bill of quantities.",
                code: "BoqTargetSectionNotFound",
                parameters: new Dictionary<string, object?> { ["sectionId"] = targetSectionId.Value });

        if (targetSubsectionId is { } subId && targetSection.FindSubsection(subId) is null)
        {
            throw new DomainValidationException(
                "The target subsection does not exist in the target section.",
                code: "BoqTargetSubsectionNotFound",
                parameters: new Dictionary<string, object?> { ["subsectionId"] = subId.Value });
        }

        var sourceSectionId = line.SectionId;
        var sourceSubsectionId = line.SubsectionId;

        // Re-group the line, place it at the target index, and keep both containers' sequences dense.
        line.Reassign(targetSectionId, targetSubsectionId);
        PlaceInContainer(line, targetSectionId, targetSubsectionId, targetIndex);

        if (sourceSectionId != targetSectionId || sourceSubsectionId != targetSubsectionId)
        {
            RenumberContainer(sourceSectionId, sourceSubsectionId);
        }

        Raise(new BoqLineItemMoved(Id, BidId, lineItemId, now));
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

    // ----- Grouped reads (the line collection is flat; these project it per container, Sequence-sorted) -----

    /// <summary>The lines held directly in a section (not in a subsection), ordered by sequence.</summary>
    public IReadOnlyList<LineItem> DirectLineItemsOf(SectionId sectionId) =>
        _lineItems
            .Where(li => li.SectionId == sectionId && li.SubsectionId is null)
            .OrderBy(li => li.Sequence)
            .ToList();

    /// <summary>The lines grouped under a subsection, ordered by sequence.</summary>
    public IReadOnlyList<LineItem> LineItemsOf(SubsectionId subsectionId) =>
        _lineItems
            .Where(li => li.SubsectionId == subsectionId)
            .OrderBy(li => li.Sequence)
            .ToList();

    /// <summary>Derived net subtotal of a section (its direct lines plus every subsection's lines).</summary>
    public Money SubtotalOf(SectionId sectionId) =>
        Sum(_lineItems.Where(li => li.SectionId == sectionId), li => li.LineTotal);

    /// <summary>Derived gross (VAT-inclusive) subtotal of a section.</summary>
    public Money SubtotalWithVatOf(SectionId sectionId) =>
        Sum(_lineItems.Where(li => li.SectionId == sectionId), li => li.LineTotalWithVat);

    /// <summary>Derived net subtotal of a subsection.</summary>
    public Money SubtotalOf(SubsectionId subsectionId) =>
        Sum(_lineItems.Where(li => li.SubsectionId == subsectionId), li => li.LineTotal);

    /// <summary>Derived gross (VAT-inclusive) subtotal of a subsection.</summary>
    public Money SubtotalWithVatOf(SubsectionId subsectionId) =>
        Sum(_lineItems.Where(li => li.SubsectionId == subsectionId), li => li.LineTotalWithVat);

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

    // Create a line under a container (subsectionId null = section's direct list) and track it flat.
    private LineItem AddLine(
        SectionId sectionId,
        SubsectionId? subsectionId,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes)
    {
        EnsureSharedCurrency(unitPrice);
        var item = new LineItem(
            LineItemId.New(), sectionId, subsectionId, description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
        _lineItems.Add(item);
        return item;
    }

    private bool Revise(
        LineItem? line,
        string description,
        decimal quantity,
        UnitOfMeasureId unitOfMeasureId,
        Money unitPrice,
        VatRate vatRate,
        int sequence,
        string? notes)
    {
        EnsureMutable();
        if (line is null)
        {
            return false;
        }

        EnsureSharedCurrency(unitPrice);
        line.Revise(description, quantity, unitOfMeasureId, unitPrice, vatRate, sequence, notes);
        return true;
    }

    private bool Remove(LineItem? line)
    {
        EnsureMutable();
        if (line is null)
        {
            return false;
        }

        _lineItems.Remove(line);
        RenumberContainer(line.SectionId, line.SubsectionId);
        return true;
    }

    // Insert a line at an index within its (already-assigned) container, against the container's
    // Sequence-sorted order — the order the reader shows — then renumber the container dense 1..N.
    private void PlaceInContainer(LineItem line, SectionId sectionId, SubsectionId? subsectionId, int index)
    {
        var container = _lineItems
            .Where(li => li.Id != line.Id && li.SectionId == sectionId && li.SubsectionId == subsectionId)
            .OrderBy(li => li.Sequence)
            .ToList();

        container.Insert(Math.Clamp(index, 0, container.Count), line);
        Renumber(container);
    }

    // Renumber a container's lines dense 1..N following their current Sequence order.
    private void RenumberContainer(SectionId sectionId, SubsectionId? subsectionId) =>
        Renumber(_lineItems
            .Where(li => li.SectionId == sectionId && li.SubsectionId == subsectionId)
            .OrderBy(li => li.Sequence)
            .ToList());

    private static void Renumber(List<LineItem> ordered)
    {
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Resequence(i + 1);
        }
    }

    private Money Sum(IEnumerable<LineItem> lines, Func<LineItem, Money> amount) =>
        lines.Aggregate(Money.Zero(PricingCurrency), (acc, li) => acc.Add(amount(li)));

    private void EnsureSharedCurrency(Money unitPrice)
    {
        if (unitPrice.Currency != PricingCurrency)
        {
            throw new DomainValidationException(
                $"Line item price currency ({unitPrice.Currency}) must match the bill's pricing currency ({PricingCurrency}).",
                code: "LineItemCurrencyMismatch",
                parameters: new Dictionary<string, object?>
                {
                    ["lineCurrency"] = unitPrice.Currency.ToString(),
                    ["billCurrency"] = PricingCurrency.ToString(),
                });
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
