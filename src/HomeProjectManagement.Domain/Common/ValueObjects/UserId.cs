namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>
/// A reference into the authentication/identity context (one of the four stakeholders).
/// Not a domain entity — it appears only in audit fields (<c>createdBy</c>, etc.). Until
/// real sign-in lands, a stub <c>ICurrentUser</c> supplies a fixed value.
/// </summary>
public readonly record struct UserId(Guid Value) : IStronglyTypedId
{
    /// <summary>Placeholder used by the temporary current-user stub before auth exists.</summary>
    public static readonly UserId System = new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
