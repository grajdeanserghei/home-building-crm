using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Trades;

namespace HomeProjectManagement.Application.Trades;

/// <summary>
/// Thin orchestration over the <see cref="Trade"/> aggregate: load via the repository port, invoke
/// domain behaviour, commit through the unit of work. Audit fields are stamped inside the unit of
/// work from the current user + clock.
/// </summary>
public sealed class TradeAppService(
    ITradeRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ITradeAppService
{
    public async Task<IReadOnlyList<TradeDto>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var trades = await repository.ListAsync(includeInactive, cancellationToken);
        return trades.Select(ToDto).ToList();
    }

    public async Task<TradeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trade = await repository.GetAsync(new TradeId(id), cancellationToken);
        return trade is null ? null : ToDto(trade);
    }

    public async Task<DefineTradeResult> DefineAsync(
        DefineTradeCommand command,
        CancellationToken cancellationToken = default)
    {
        // Enforce the controlled-vocabulary invariant: the canonical name is unique. The unique
        // DB index is the backstop; this check turns a duplicate into a clean conflict.
        var existing = await repository.FindByNameAsync(command.Name, cancellationToken);
        if (existing is not null)
        {
            return DefineTradeResult.Conflict(existing.Name);
        }

        var trade = Trade.Define(command.Name, timeProvider.GetUtcNow(), command.Code);

        repository.Add(trade);
        await unitOfWork.CommitAsync(cancellationToken);
        return DefineTradeResult.Success(ToDto(trade));
    }

    public async Task<TradeDto?> UpdateAsync(
        Guid id,
        UpdateTradeCommand command,
        CancellationToken cancellationToken = default)
    {
        var trade = await repository.GetAsync(new TradeId(id), cancellationToken);
        if (trade is null)
        {
            return null;
        }

        // Keep the unique-name invariant: another trade may not already carry the new name.
        var clash = await repository.FindByNameAsync(command.Name, cancellationToken);
        if (clash is not null && clash.Id != trade.Id)
        {
            throw new DomainConflictException(
                $"A trade named '{clash.Name}' already exists.",
                code: "TradeNameDuplicate",
                parameters: new Dictionary<string, object?> { ["name"] = clash.Name });
        }

        trade.Rename(command.Name);
        trade.SetCode(command.Code);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(trade);
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var trade = await repository.GetAsync(new TradeId(id), cancellationToken);
        if (trade is null)
        {
            return false;
        }

        if (isActive)
        {
            trade.Activate(timeProvider.GetUtcNow());
        }
        else
        {
            trade.Deactivate(timeProvider.GetUtcNow());
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static TradeDto ToDto(Trade trade) => new(
        trade.Id.Value,
        trade.Name,
        trade.Code,
        trade.IsActive,
        trade.CreatedOn);
}
