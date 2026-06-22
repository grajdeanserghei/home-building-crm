using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.Domain.Bids;

/// <summary>
/// A timestamped entry in a <see cref="Bid"/>'s discussion log — a meeting, call, email, or
/// remark with the contractor about the work package.
/// </summary>
/// <remarks>
/// A <b>local entity inside the Bid aggregate</b>: it has identity within the bid but is never
/// referenced from outside it, so the bid root owns its whole lifecycle. Notes are created and
/// removed only through the <see cref="Bid"/> root; their content is immutable once logged.
/// </remarks>
public sealed class DiscussionNote : Entity<DiscussionNoteId>
{
    /// <summary>The kind of interaction this note records.</summary>
    public NoteType Type { get; private set; }

    /// <summary>When the interaction happened (not when it was logged).</summary>
    public DateTimeOffset OccurredOn { get; private set; }

    /// <summary>Which stakeholder logged it (a reference into the auth context).</summary>
    public UserId AuthorId { get; private set; }

    /// <summary>The note text.</summary>
    public string Content { get; private set; } = null!;

    // EF Core materialisation constructor.
    private DiscussionNote()
    {
    }

    // Created only by the Bid root (see Bid.LogNote).
    internal DiscussionNote(
        DiscussionNoteId id,
        NoteType type,
        DateTimeOffset occurredOn,
        UserId authorId,
        string content) : base(id)
    {
        Id = id;
        Type = type;
        OccurredOn = occurredOn;
        AuthorId = authorId;
        Content = NormalizeContent(content);
    }

    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Discussion note content is required.", nameof(content));
        }

        return content.Trim();
    }
}
