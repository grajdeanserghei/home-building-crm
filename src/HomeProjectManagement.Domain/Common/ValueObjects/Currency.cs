namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>
/// The currencies the tool deals in. Contractors price in one of these; both figures are
/// shown side by side via a pinned <see cref="ExchangeRate"/>. Persisted as the ISO 4217
/// string code (RON / EUR).
/// </summary>
public enum Currency
{
    RON,
    EUR
}
