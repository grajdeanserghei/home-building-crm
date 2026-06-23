namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Thrown when input violates an invariant of an aggregate or value object — a required
/// field is blank, a number is out of its allowed range, a date range is inverted. Maps
/// to HTTP 400 Bad Request.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    /// <summary>The offending parameter or field name, when known.</summary>
    public string? ParameterName { get; }

    public DomainValidationException(
        string message,
        string? parameterName = null,
        string? code = null,
        IReadOnlyDictionary<string, object?>? parameters = null)
        : base(message, code, parameters) => ParameterName = parameterName;
}
