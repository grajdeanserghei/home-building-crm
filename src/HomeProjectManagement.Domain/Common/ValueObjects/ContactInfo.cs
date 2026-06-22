namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>A contractor's primary contact person. All parts optional.</summary>
public sealed class ContactInfo : ValueObject
{
    public string? PersonName { get; }
    public string? Email { get; }
    public string? Phone { get; }

    public ContactInfo(string? personName, string? email, string? phone)
    {
        PersonName = personName;
        Email = email;
        Phone = phone;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PersonName;
        yield return Email;
        yield return Phone;
    }
}
