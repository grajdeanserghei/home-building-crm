namespace HomeProjectManagement.Domain.Common;

/// <summary>
/// Marker for the strongly-typed identifier convention. Every aggregate root has its
/// own id type (e.g. <c>ProjectId</c>) wrapping a <see cref="Guid"/>, so the compiler
/// prevents mixing identities across aggregates.
/// </summary>
/// <remarks>
/// Implementers are expected to be <c>readonly record struct Xxx(Guid Value)</c>:
/// the <see cref="Value"/> getter plus a single public constructor taking a
/// <see cref="Guid"/> is the contract the EF Core value-converter convention relies on
/// (see Infrastructure/Persistence/Conversions).
/// </remarks>
public interface IStronglyTypedId
{
    Guid Value { get; }
}
