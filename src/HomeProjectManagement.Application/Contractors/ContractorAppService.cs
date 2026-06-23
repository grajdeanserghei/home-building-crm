using HomeProjectManagement.Application.Trades;
using HomeProjectManagement.Domain.Common;
using HomeProjectManagement.Domain.Common.ValueObjects;
using HomeProjectManagement.Domain.Contractors;
using HomeProjectManagement.Domain.Trades;

namespace HomeProjectManagement.Application.Contractors;

/// <summary>
/// Thin orchestration over the <see cref="Contractor"/> aggregate: load via the repository
/// port, invoke domain behaviour, commit through the unit of work. Audit fields are stamped
/// inside the unit of work from the current user + clock. Maps the contact/address value
/// objects to and from their DTOs at this boundary.
/// </summary>
public sealed class ContractorAppService(
    IContractorRepository repository,
    ITradeRepository trades,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IContractorAppService
{
    public async Task<IReadOnlyList<ContractorDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var contractors = await repository.ListAsync(cancellationToken);
        return contractors.Select(ToDto).ToList();
    }

    public async Task<ContractorDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contractor = await repository.GetAsync(new ContractorId(id), cancellationToken);
        return contractor is null ? null : ToDto(contractor);
    }

    public async Task<ContractorDto> RegisterAsync(
        RegisterContractorCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate the referenced trades against the shared vocabulary before tagging the firm.
        var tradeIds = await TradeAssignment.ResolveAsync(trades, command.TradeIds, cancellationToken);

        var contractor = Contractor.Register(
            command.Name,
            timeProvider.GetUtcNow(),
            command.FiscalCode,
            command.RegistrationNumber,
            ToContact(command.Contact),
            ToAddress(command.Address),
            command.Notes);

        contractor.SetTrades(tradeIds);

        repository.Add(contractor);
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(contractor);
    }

    public async Task<ContractorDto?> UpdateAsync(
        Guid id,
        UpdateContractorCommand command,
        CancellationToken cancellationToken = default)
    {
        var contractor = await repository.GetAsync(new ContractorId(id), cancellationToken);
        if (contractor is null)
        {
            return null;
        }

        contractor.Rename(command.Name);
        contractor.SetFiscalIdentifiers(command.FiscalCode, command.RegistrationNumber);
        contractor.ChangeContact(ToContact(command.Contact));
        contractor.Relocate(ToAddress(command.Address));
        contractor.Annotate(command.Notes);

        // Trades are a set-replace, but only when supplied: a null TradeIds leaves the existing set
        // untouched (so the edit form, which manages trades incrementally elsewhere, doesn't clobber
        // them); a non-null list — including an empty one — replaces the whole set.
        if (command.TradeIds is not null)
        {
            contractor.SetTrades(await TradeAssignment.ResolveAsync(trades, command.TradeIds, cancellationToken));
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(contractor);
    }

    public async Task<ContractorDto?> AddTradeAsync(Guid id, Guid tradeId, CancellationToken cancellationToken = default)
    {
        var contractor = await repository.GetAsync(new ContractorId(id), cancellationToken);
        if (contractor is null)
        {
            return null;
        }

        // Validate the trade exists and is active before tagging the firm with it.
        var resolved = await TradeAssignment.ResolveAsync(trades, [tradeId], cancellationToken);
        contractor.AssignTrade(resolved[0]);

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(contractor);
    }

    public async Task<ContractorDto?> RemoveTradeAsync(Guid id, Guid tradeId, CancellationToken cancellationToken = default)
    {
        var contractor = await repository.GetAsync(new ContractorId(id), cancellationToken);
        if (contractor is null)
        {
            return null;
        }

        // Removal is idempotent and needs no vocabulary check — a now-retired trade can still be dropped.
        contractor.RemoveTrade(new TradeId(tradeId));

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(contractor);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contractor = await repository.GetAsync(new ContractorId(id), cancellationToken);
        if (contractor is null)
        {
            return false;
        }

        repository.Remove(contractor);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    // ----- mapping -----

    private static ContactInfo? ToContact(ContactInfoDto? dto) =>
        dto is null ? null : new ContactInfo(Trim(dto.PersonName), Trim(dto.Email), Trim(dto.Phone));

    private static Address? ToAddress(AddressDto? dto) =>
        dto is null
            ? null
            : new Address(Trim(dto.Street), Trim(dto.City), Trim(dto.County), Trim(dto.PostalCode), Trim(dto.Country));

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ContractorDto ToDto(Contractor contractor) => new(
        contractor.Id.Value,
        contractor.Name,
        contractor.FiscalCode,
        contractor.RegistrationNumber,
        contractor.Contact is null
            ? null
            : new ContactInfoDto(contractor.Contact.PersonName, contractor.Contact.Email, contractor.Contact.Phone),
        contractor.Address is null
            ? null
            : new AddressDto(
                contractor.Address.Street,
                contractor.Address.City,
                contractor.Address.County,
                contractor.Address.PostalCode,
                contractor.Address.Country),
        contractor.Notes,
        contractor.TradeIds.Select(t => t.Value).ToArray(),
        contractor.CreatedOn);
}
