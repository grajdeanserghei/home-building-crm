namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>
/// A postal address. Used for a Project's site and a Contractor's address. All parts are
/// optional so a partial address can still be captured.
/// </summary>
public sealed class Address : ValueObject
{
    public string? Street { get; }
    public string? City { get; }

    /// <summary>The Romanian județ.</summary>
    public string? County { get; }

    public string? PostalCode { get; }
    public string? Country { get; }

    public Address(string? street, string? city, string? county, string? postalCode, string? country)
    {
        Street = street;
        City = city;
        County = county;
        PostalCode = postalCode;
        Country = country;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return County;
        yield return PostalCode;
        yield return Country;
    }
}
