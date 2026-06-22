using System.Linq.Expressions;
using HomeProjectManagement.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeProjectManagement.Infrastructure.Persistence.Conversions;

/// <summary>
/// EF Core value converter mapping any strongly-typed id (<see cref="IStronglyTypedId"/>)
/// to/from a <see cref="Guid"/> column. Relies on the convention that an id is a
/// <c>record struct Xxx(Guid Value)</c> — i.e. it exposes <c>Value</c> and has a single
/// <see cref="Guid"/> constructor.
/// </summary>
public sealed class StronglyTypedIdValueConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct, IStronglyTypedId
{
    public StronglyTypedIdValueConverter()
        : base(id => id.Value, value => Rehydrate(value))
    {
    }

    private static readonly Func<Guid, TId> Factory = BuildFactory();

    private static TId Rehydrate(Guid value) => Factory(value);

    private static Func<Guid, TId> BuildFactory()
    {
        var ctor = typeof(TId).GetConstructor([typeof(Guid)])
            ?? throw new InvalidOperationException(
                $"{typeof(TId)} must have a public constructor taking a single Guid.");

        var parameter = Expression.Parameter(typeof(Guid), "value");
        var body = Expression.New(ctor, parameter);
        return Expression.Lambda<Func<Guid, TId>>(body, parameter).Compile();
    }
}
