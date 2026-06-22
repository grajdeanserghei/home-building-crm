using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Projects;

/// <summary>Strongly-typed identity for the <see cref="Project"/> aggregate root.</summary>
public readonly record struct ProjectId(Guid Value) : IStronglyTypedId
{
    public static ProjectId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
