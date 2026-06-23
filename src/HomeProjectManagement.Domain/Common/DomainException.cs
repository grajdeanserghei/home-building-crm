namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Base type for exceptions that represent a deliberately violated domain rule — a
/// signal raised by the domain core to reject an operation, not a programming defect.
/// The HTTP adapter maps subclasses to <c>ProblemDetails</c> responses; ordinary
/// BCL/framework exceptions are left to surface as 500s so genuine bugs stay visible.
/// </summary>
/// <remarks>
/// The English <see cref="Exception.Message"/> stays the developer-facing fallback. A stable
/// <see cref="Code"/> (plus an optional <see cref="Parameters"/> bag of interpolated values) lets
/// a presentation layer translate the rule violation — e.g. into Romanian — without dragging
/// localization into the pure domain. Interpolated values must be passed as parameters, not baked
/// into the message, so a translated template can reconstruct the sentence.
/// See <c>docs/specifications/romanian-localization.md</c>.
/// </remarks>
public abstract class DomainException : Exception
{
    /// <summary>
    /// A stable, language-neutral identifier for this rule violation (e.g.
    /// <c>ScopeItemNameDuplicate</c>), or <c>null</c> for the not-yet-coded long tail. Stable
    /// across releases so the frontend can key a localized message off it.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// The interpolated values referenced by the message (e.g. <c>{ ["name"] = "Foundation" }</c>),
    /// so a localized template can re-insert them. <c>null</c> when there are none.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; }

    protected DomainException(
        string message,
        string? code = null,
        IReadOnlyDictionary<string, object?>? parameters = null)
        : base(message)
    {
        Code = code;
        Parameters = parameters;
    }
}
