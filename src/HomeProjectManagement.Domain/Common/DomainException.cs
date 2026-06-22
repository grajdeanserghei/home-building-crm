namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Base type for exceptions that represent a deliberately violated domain rule — a
/// signal raised by the domain core to reject an operation, not a programming defect.
/// The HTTP adapter maps subclasses to <c>ProblemDetails</c> responses; ordinary
/// BCL/framework exceptions are left to surface as 500s so genuine bugs stay visible.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }
}
