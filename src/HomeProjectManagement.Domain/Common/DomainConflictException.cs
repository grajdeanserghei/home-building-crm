namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Thrown when an operation is rejected because the aggregate's current state does not
/// permit it — an illegal status transition, modifying a submitted bid, awarding a work
/// package that is already under contract. Maps to HTTP 409 Conflict.
/// </summary>
public sealed class DomainConflictException : DomainException
{
    public DomainConflictException(
        string message,
        string? code = null,
        IReadOnlyDictionary<string, object?>? parameters = null)
        : base(message, code, parameters)
    {
    }
}
