using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Application.UnitsOfMeasure;

/// <summary>
/// Thin orchestration over the <see cref="UnitOfMeasure"/> aggregate: load via the repository
/// port, invoke domain behaviour, commit through the unit of work. Audit fields are stamped
/// inside the unit of work from the current user + clock.
/// </summary>
public sealed class UnitOfMeasureAppService(
    IUnitOfMeasureRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IUnitOfMeasureAppService
{
    public async Task<IReadOnlyList<UnitOfMeasureDto>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var units = await repository.ListAsync(includeInactive, cancellationToken);
        return units.Select(ToDto).ToList();
    }

    public async Task<UnitOfMeasureDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await repository.GetAsync(new UnitOfMeasureId(id), cancellationToken);
        return unit is null ? null : ToDto(unit);
    }

    public async Task<DefineUnitOfMeasureResult> DefineAsync(
        DefineUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default)
    {
        // Enforce the controlled-vocabulary invariant: the canonical code is unique. The unique
        // DB index is the backstop; this check turns a race-free duplicate into a clean conflict.
        var existing = await repository.FindByCodeAsync(command.Code, cancellationToken);
        if (existing is not null)
        {
            return DefineUnitOfMeasureResult.Conflict(existing.Code);
        }

        var unit = UnitOfMeasure.Define(
            command.Code,
            command.Name,
            command.Category,
            timeProvider.GetUtcNow(),
            command.Aliases);

        repository.Add(unit);
        await unitOfWork.CommitAsync(cancellationToken);
        return DefineUnitOfMeasureResult.Success(ToDto(unit));
    }

    public async Task<UnitOfMeasureDto?> UpdateAsync(
        Guid id,
        UpdateUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default)
    {
        var unit = await repository.GetAsync(new UnitOfMeasureId(id), cancellationToken);
        if (unit is null)
        {
            return null;
        }

        unit.Rename(command.Name);
        unit.Recategorize(command.Category);
        unit.SetAliases(command.Aliases ?? []);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(unit);
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var unit = await repository.GetAsync(new UnitOfMeasureId(id), cancellationToken);
        if (unit is null)
        {
            return false;
        }

        if (isActive)
        {
            unit.Activate(timeProvider.GetUtcNow());
        }
        else
        {
            unit.Deactivate(timeProvider.GetUtcNow());
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static UnitOfMeasureDto ToDto(UnitOfMeasure unit) => new(
        unit.Id.Value,
        unit.Code,
        unit.Name,
        unit.Category,
        unit.Aliases.ToArray(),
        unit.IsActive,
        unit.CreatedOn);
}
