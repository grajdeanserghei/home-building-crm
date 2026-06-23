using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Trades.Events;

namespace HomeProjectManagement.Domain.Trades;

/// <summary>
/// A category of specialized construction work (e.g. Zidărie/masonry, Instalații Electrice/
/// electrical, Interioare/interior finishing). A <b>controlled reference vocabulary</b> — not free
/// text — so the same trade never fragments into "Electrice", "Instalații electrice" and "Electric"
/// across contractors and work packages, which would break filtering and matching.
/// </summary>
/// <remarks>
/// Project-independent aggregate root referenced <b>by id</b> from Contractor ("trades performed")
/// and Work Package ("trades required"), never held directly. Seeded with the common construction
/// trades and only extended by an admin. A trade is <b>retired via <see cref="Deactivate"/></b>
/// rather than deleted, since contractors and work packages may still reference it. State changes go
/// through intention-revealing methods; construction goes through the <see cref="Define"/> factory.
/// </remarks>
public sealed class Trade : AggregateRoot<TradeId>
{
    /// <summary>Canonical trade name, unique across the vocabulary (e.g. "Zidărie"). Displayed in Romanian.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Optional short code (e.g. "ELE"). Free-form; not required to be unique.</summary>
    public string? Code { get; private set; }

    /// <summary>Whether the trade may be assigned to contractors/work packages. Retire via <see cref="Deactivate"/>.</summary>
    public bool IsActive { get; private set; }

    // EF Core materialisation constructor.
    private Trade()
    {
    }

    private Trade(TradeId id, string name) : base(id)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Factory: define a new canonical trade, validating its invariants. <paramref name="now"/>
    /// is supplied by the caller (from <c>TimeProvider</c>) rather than read inside the domain. A
    /// freshly defined trade is active. Global name uniqueness is the caller's responsibility
    /// (enforced by the application service + a unique DB index).
    /// </summary>
    public static Trade Define(string name, DateTimeOffset now, string? code = null)
    {
        var trade = new Trade(TradeId.New(), NormalizeName(name))
        {
            Code = Trim(code),
            IsActive = true
        };

        trade.Raise(new TradeDefined(trade.Id, trade.Name, now));
        return trade;
    }

    /// <summary>Rename the trade (its canonical name).</summary>
    public void Rename(string name) => Name = NormalizeName(name);

    /// <summary>Set or clear the optional short code.</summary>
    public void SetCode(string? code) => Code = Trim(code);

    /// <summary>Retire the trade so it is no longer offered for new assignments, without deleting it.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Raise(new TradeActivationChanged(Id, false, now));
    }

    /// <summary>Bring a retired trade back into use.</summary>
    public void Activate(DateTimeOffset now)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Raise(new TradeActivationChanged(Id, true, now));
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Trade name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
