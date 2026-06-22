using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.BillsOfQuantities;
using HomeProjectManagement.Domain.Bids;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.UnitsOfMeasure;

namespace HomeProjectManagement.Application.BillsOfQuantities;

/// <summary>
/// Thin orchestration over the <see cref="BillOfQuantities"/> aggregate: load via the repository
/// port, invoke domain behaviour, commit through the unit of work. It assigns each BoQ's version
/// within its bid, checks the parent bid exists, and validates that every line item references an
/// <b>active</b> unit of measure before the line reaches the aggregate (the currency and
/// edit-while-open invariants are enforced by the aggregate itself). Audit fields are stamped
/// inside the unit of work.
/// </summary>
public sealed class BillOfQuantitiesAppService(
    IBillOfQuantitiesRepository repository,
    IBidRepository bids,
    IUnitOfMeasureRepository units,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IBillOfQuantitiesAppService
{
    public async Task<IReadOnlyList<BillOfQuantitiesDto>> ListByBidAsync(
        Guid bidId,
        CancellationToken cancellationToken = default)
    {
        var boqs = await repository.ListByBidAsync(new BidId(bidId), cancellationToken);
        return boqs.OrderBy(b => b.Version).Select(ToDto).ToList();
    }

    public async Task<BillOfQuantitiesDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        return boq is null ? null : ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> DraftAsync(
        Guid bidId,
        DraftBillOfQuantitiesCommand command,
        CancellationToken cancellationToken = default)
    {
        // The owning bid must exist before drafting a BoQ that references it by id.
        var bid = await bids.GetAsync(new BidId(bidId), cancellationToken);
        if (bid is null)
        {
            return null;
        }

        // Versions run 1, 2, … within the bid; assign the next one.
        var existing = await repository.ListByBidAsync(bid.Id, cancellationToken);
        var nextVersion = existing.Count == 0 ? 1 : existing.Max(b => b.Version) + 1;

        var boq = BillOfQuantities.Draft(
            bid.Id,
            nextVersion,
            command.PricingCurrency,
            timeProvider.GetUtcNow(),
            command.Reference,
            ToExchangeRate(command.ExchangeRate),
            command.SubmittedOn,
            command.ValidUntil);

        repository.Add(boq);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> UpdateAsync(
        Guid id,
        UpdateBillOfQuantitiesCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        boq.UpdateDetails(
            command.Reference,
            ToExchangeRate(command.ExchangeRate),
            command.SubmittedOn,
            command.ValidUntil);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> ChangeStatusAsync(
        Guid id,
        ChangeBoqStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        boq.ChangeStatus(command.Status, timeProvider.GetUtcNow());
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> AddSectionAsync(
        Guid id,
        SectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        boq.AddSection(command.Name, command.Sequence, command.Description);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> UpdateSectionAsync(
        Guid id,
        Guid sectionId,
        SectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null || !boq.UpdateSection(new SectionId(sectionId), command.Name, command.Sequence, command.Description))
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<bool> RemoveSectionAsync(Guid id, Guid sectionId, CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null || !boq.RemoveSection(new SectionId(sectionId)))
        {
            return false;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<BillOfQuantitiesDto?> AddLineItemAsync(
        Guid id,
        Guid sectionId,
        LineItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        await EnsureActiveUnitAsync(command.UnitOfMeasureId, cancellationToken);

        var added = boq.AddLineItem(
            new SectionId(sectionId),
            command.Description,
            command.Quantity,
            new UnitOfMeasureId(command.UnitOfMeasureId),
            ToMoney(command.UnitPrice),
            command.Sequence,
            command.Notes);

        if (added is null)
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<BillOfQuantitiesDto?> ReviseLineItemAsync(
        Guid id,
        Guid sectionId,
        Guid lineItemId,
        LineItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return null;
        }

        await EnsureActiveUnitAsync(command.UnitOfMeasureId, cancellationToken);

        var revised = boq.ReviseLineItem(
            new SectionId(sectionId),
            new LineItemId(lineItemId),
            command.Description,
            command.Quantity,
            new UnitOfMeasureId(command.UnitOfMeasureId),
            ToMoney(command.UnitPrice),
            command.Sequence,
            command.Notes);

        if (!revised)
        {
            return null;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(boq);
    }

    public async Task<bool> RemoveLineItemAsync(
        Guid id,
        Guid sectionId,
        Guid lineItemId,
        CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null || !boq.RemoveLineItem(new SectionId(sectionId), new LineItemId(lineItemId)))
        {
            return false;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var boq = await repository.GetAsync(new BoqId(id), cancellationToken);
        if (boq is null)
        {
            return false;
        }

        repository.Remove(boq);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    // Line items normalise onto the controlled vocabulary: only an active canonical unit may be used.
    private async Task EnsureActiveUnitAsync(Guid unitOfMeasureId, CancellationToken cancellationToken)
    {
        var unit = await units.GetAsync(new UnitOfMeasureId(unitOfMeasureId), cancellationToken);
        if (unit is null || !unit.IsActive)
        {
            throw new InvalidOperationException("The line item must reference an active unit of measure.");
        }
    }

    private static Money ToMoney(MoneyDto dto) => new(dto.Amount, dto.Currency);

    private static ExchangeRate? ToExchangeRate(ExchangeRateDto? dto) =>
        dto is null ? null : new ExchangeRate(dto.BaseCurrency, dto.QuoteCurrency, dto.Rate, dto.AsOf);

    private static BillOfQuantitiesDto ToDto(BillOfQuantities boq) => new(
        boq.Id.Value,
        boq.BidId.Value,
        boq.Reference,
        boq.Version,
        boq.Status,
        boq.PricingCurrency,
        ToDto(boq.ExchangeRate),
        boq.SubmittedOn,
        boq.ValidUntil,
        ToDto(boq.Total),
        boq.Sections
            .OrderBy(s => s.Sequence)
            .Select(ToDto)
            .ToList(),
        boq.CreatedOn);

    private static SectionDto ToDto(Section section) => new(
        section.Id.Value,
        section.Name,
        section.Sequence,
        section.Description,
        ToDto(section.Subtotal),
        section.LineItems
            .OrderBy(li => li.Sequence)
            .Select(ToDto)
            .ToList());

    private static LineItemDto ToDto(LineItem item) => new(
        item.Id.Value,
        item.Description,
        item.Quantity,
        item.UnitOfMeasureId.Value,
        ToDto(item.UnitPrice),
        ToDto(item.LineTotal),
        item.Sequence,
        item.Notes);

    private static MoneyDto ToDto(Money money) => new(money.Amount, money.Currency);

    private static ExchangeRateDto? ToDto(ExchangeRate? rate) =>
        rate is null ? null : new ExchangeRateDto(rate.BaseCurrency, rate.QuoteCurrency, rate.Rate, rate.AsOf);
}
