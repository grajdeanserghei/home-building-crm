namespace HomeProjectManagement.Domain.Bids;

/// <summary>
/// The kind of interaction a <see cref="DiscussionNote"/> records. Persisted as its string
/// name. See the domain model's <c>NoteType</c> enum.
/// </summary>
public enum NoteType
{
    /// <summary>A face-to-face or video meeting.</summary>
    Meeting,

    /// <summary>A phone call.</summary>
    Call,

    /// <summary>An email exchange.</summary>
    Email,

    /// <summary>A free-standing remark not tied to a specific interaction.</summary>
    Note
}
