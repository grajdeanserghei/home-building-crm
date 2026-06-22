using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Application.Abstractions;

/// <summary>
/// Driven port: who is acting, for audit stamping. Until real sign-in for the four
/// stakeholders lands, a stub in Infrastructure returns a fixed <see cref="UserId"/>.
/// </summary>
public interface ICurrentUser
{
    UserId UserId { get; }
}
