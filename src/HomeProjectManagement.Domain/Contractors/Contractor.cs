using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contractors.Events;
using HomeProjectManagement.Domain.Trades;

namespace HomeProjectManagement.Domain.Contractors;

/// <summary>
/// A construction firm (master data): name, fiscal identifiers, contact and address. A
/// contractor exists independently of any project or work package, and may bid on — and be
/// selected for — several work packages. The <i>role</i> it plays per work package is captured
/// by Bid and Contract elsewhere, not by duplicating the firm here.
/// </summary>
/// <remarks>
/// Aggregate root with no internal entities and no references to other aggregates — it is
/// referenced (by id) from Bid. Encapsulated: no public setters. Construction goes through the
/// <see cref="Register"/> factory; edits go through intention-revealing methods. Its
/// <see cref="Contact"/> and <see cref="Address"/> are optional owned value objects, normalised
/// to <c>null</c> when entirely empty so "no contact captured" has a single representation.
/// </remarks>
public sealed class Contractor : AggregateRoot<ContractorId>
{
    private readonly List<ContractorTrade> _trades = [];

    /// <summary>Company name.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Romanian fiscal code (CUI). Optional.</summary>
    public string? FiscalCode { get; private set; }

    /// <summary>Trade register number (Nr. Reg. Com., the J-number). Optional.</summary>
    public string? RegistrationNumber { get; private set; }

    /// <summary>Primary contact person. Optional.</summary>
    public ContactInfo? Contact { get; private set; }

    /// <summary>The firm's address. Optional.</summary>
    public Address? Address { get; private set; }

    /// <summary>Free-text notes. Optional.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// The trades this firm performs, as owned links to the shared <see cref="Trade"/> vocabulary
    /// (by id; many-to-many via the <c>contractor_trades</c> join table). A distinct set that may be
    /// empty. Mutated only through <see cref="AssignTrade"/>/<see cref="RemoveTrade"/>/<see cref="SetTrades"/>;
    /// EF reaches the backing field directly. Drives "filter contractors by type of work".
    /// </summary>
    public IReadOnlyList<ContractorTrade> Trades => _trades.AsReadOnly();

    /// <summary>The ids of the trades this firm performs (convenience projection over <see cref="Trades"/>).</summary>
    public IEnumerable<TradeId> TradeIds => _trades.Select(t => t.TradeId);

    // EF Core materialisation constructor.
    private Contractor()
    {
    }

    private Contractor(ContractorId id, string name) : base(id)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Factory: register a new contractor firm, validating its invariants. <paramref name="now"/>
    /// is supplied by the caller (from <c>TimeProvider</c>) rather than read inside the domain.
    /// </summary>
    public static Contractor Register(
        string name,
        DateTimeOffset now,
        string? fiscalCode = null,
        string? registrationNumber = null,
        ContactInfo? contact = null,
        Address? address = null,
        string? notes = null)
    {
        var contractor = new Contractor(ContractorId.New(), NormalizeName(name))
        {
            FiscalCode = Trim(fiscalCode),
            RegistrationNumber = Trim(registrationNumber),
            Contact = NormalizeContact(contact),
            Address = NormalizeAddress(address),
            Notes = Trim(notes)
        };

        contractor.Raise(new ContractorRegistered(contractor.Id, contractor.Name, now));
        return contractor;
    }

    /// <summary>Rename the firm.</summary>
    public void Rename(string name) => Name = NormalizeName(name);

    /// <summary>Set or clear the Romanian fiscal identifiers (CUI and trade-register number).</summary>
    public void SetFiscalIdentifiers(string? fiscalCode, string? registrationNumber)
    {
        FiscalCode = Trim(fiscalCode);
        RegistrationNumber = Trim(registrationNumber);
    }

    /// <summary>Set or clear the primary contact person.</summary>
    public void ChangeContact(ContactInfo? contact) => Contact = NormalizeContact(contact);

    /// <summary>Set or clear the firm's address.</summary>
    public void Relocate(Address? address) => Address = NormalizeAddress(address);

    /// <summary>Update the free-text notes.</summary>
    public void Annotate(string? notes) => Notes = Trim(notes);

    /// <summary>
    /// Tag the firm with a trade it performs (referenced by id). No-op if it already performs that
    /// trade — the set stays distinct. Validating that the trade exists and is active is the
    /// application service's responsibility (against the shared vocabulary).
    /// </summary>
    public bool AssignTrade(TradeId tradeId)
    {
        if (_trades.Any(t => t.TradeId == tradeId))
        {
            return false;
        }

        _trades.Add(new ContractorTrade(tradeId));
        return true;
    }

    /// <summary>Remove a trade tag. No-op (returns false) if the firm did not perform that trade.</summary>
    public bool RemoveTrade(TradeId tradeId)
    {
        var existing = _trades.FirstOrDefault(t => t.TradeId == tradeId);
        if (existing is null)
        {
            return false;
        }

        _trades.Remove(existing);
        return true;
    }

    /// <summary>
    /// Replace the whole set of performed trades in one operation, keeping it distinct (duplicate
    /// ids collapse). Validating the ids against the shared vocabulary is the application service's job.
    /// </summary>
    public void SetTrades(IEnumerable<TradeId> tradeIds)
    {
        ArgumentNullException.ThrowIfNull(tradeIds);

        _trades.Clear();
        foreach (var tradeId in tradeIds.Distinct())
        {
            _trades.Add(new ContractorTrade(tradeId));
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Contractor name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // An all-empty contact/address carries no information; collapse it to null so the
    // aggregate has a single representation of "none captured".
    private static ContactInfo? NormalizeContact(ContactInfo? contact) =>
        contact is null || (contact.PersonName is null && contact.Email is null && contact.Phone is null)
            ? null
            : contact;

    private static Address? NormalizeAddress(Address? address) =>
        address is null ||
        (address.Street is null && address.City is null && address.County is null &&
         address.PostalCode is null && address.Country is null)
            ? null
            : address;
}
