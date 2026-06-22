namespace HomeProjectManagement.Application.Abstractions;

/// <summary>
/// Driven port for time. Lets application/domain code avoid <see cref="DateTimeOffset.UtcNow"/>
/// directly, so behaviour is testable. Implemented by <c>SystemClock</c> in Infrastructure.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
