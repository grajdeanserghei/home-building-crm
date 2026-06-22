using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Infrastructure.Identity;

/// <summary>
/// Temporary <see cref="ICurrentUser"/> until real sign-in for the four stakeholders is
/// designed. Supplies a fixed placeholder <see cref="UserId"/> so audit fields are valid.
/// Replace with an adapter that reads the authenticated principal.
/// </summary>
public sealed class StubCurrentUser : ICurrentUser
{
    // A stable, well-known id for the not-yet-authenticated "system" actor.
    private static readonly UserId Placeholder = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public UserId UserId => Placeholder;
}
