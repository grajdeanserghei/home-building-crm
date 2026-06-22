using HomeProjectManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeProjectManagement.Infrastructure.Persistence.Conversions;

/// <summary>
/// Applies <see cref="StronglyTypedIdValueConverter{TId}"/> by convention to every
/// <see cref="IStronglyTypedId"/> in the domain assembly, so new aggregates' id types map
/// to <c>Guid</c> columns automatically without per-id configuration.
/// </summary>
public static class StronglyTypedIdConventions
{
    public static void ApplyStronglyTypedIdConversions(this ModelConfigurationBuilder configurationBuilder)
    {
        var idTypes = typeof(IStronglyTypedId).Assembly
            .GetTypes()
            .Where(t => t is { IsValueType: true, IsAbstract: false }
                        && typeof(IStronglyTypedId).IsAssignableFrom(t));

        foreach (var idType in idTypes)
        {
            var converterType = typeof(StronglyTypedIdValueConverter<>).MakeGenericType(idType);
            configurationBuilder.Properties(idType).HaveConversion(converterType);
        }
    }
}
