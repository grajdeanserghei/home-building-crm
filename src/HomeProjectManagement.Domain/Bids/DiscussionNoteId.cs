using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Bids;

/// <summary>
/// Local identity for a <see cref="DiscussionNote"/> within the <see cref="Bid"/> aggregate.
/// Unique inside its bid; notes are never referenced from outside the aggregate.
/// </summary>
public readonly record struct DiscussionNoteId(Guid Value) : IStronglyTypedId
{
    public static DiscussionNoteId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
