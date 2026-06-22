using HomeProjectManagement.Application.Abstractions;

namespace HomeProjectManagement.Infrastructure.Time;

/// <summary>Real wall-clock adapter for <see cref="IClock"/>.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
