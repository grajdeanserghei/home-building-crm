using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Contracts;

/// <summary>
/// Strongly-typed identity for the Contract aggregate root.
/// </summary>
/// <remarks>
/// The Contract aggregate itself is not yet implemented. This id is declared ahead of it
/// because the <see cref="WorkPackages.WorkPackage"/> root references the awarded contract
/// <b>by identity</b> (<c>AwardedContractId</c>) — the model's rule that aggregates only
/// reference each other by id. When Contract lands it will own this type's folder.
/// </remarks>
public readonly record struct ContractId(Guid Value) : IStronglyTypedId
{
    public static ContractId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
