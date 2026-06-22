namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>
/// An inclusive date range. Optional building block that may replace planned start/end
/// pairs on Work Package / Contract.
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("DateRange end must not be before start.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
