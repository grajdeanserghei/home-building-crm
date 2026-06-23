namespace HomeProjectManagement.Application.Contractors;

/// <summary>
/// Read model returned to clients. <c>Contact</c> and <c>Address</c> are null when nothing was
/// captured; <c>CreatedAt</c> comes from the aggregate's audit fields.
/// </summary>
public sealed record ContractorDto(
    Guid Id,
    string Name,
    string? FiscalCode,
    string? RegistrationNumber,
    ContactInfoDto? Contact,
    AddressDto? Address,
    string? Notes,
    IReadOnlyCollection<Guid> TradeIds,
    DateTimeOffset CreatedAt);

/// <summary>Primary contact person (mirrors the <c>ContactInfo</c> value object).</summary>
public sealed record ContactInfoDto(string? PersonName, string? Email, string? Phone);

/// <summary>A postal address (mirrors the <c>Address</c> value object).</summary>
public sealed record AddressDto(
    string? Street,
    string? City,
    string? County,
    string? PostalCode,
    string? Country);

/// <summary>
/// Input for registering a contractor firm. Only the name is required. <c>TradeIds</c> are the
/// trades the firm performs (referenced by id; each must be an existing, active trade); null or
/// empty means none captured yet.
/// </summary>
public sealed record RegisterContractorCommand(
    string Name,
    string? FiscalCode,
    string? RegistrationNumber,
    ContactInfoDto? Contact,
    AddressDto? Address,
    string? Notes,
    IReadOnlyCollection<Guid>? TradeIds);

/// <summary>
/// Input for editing a contractor's master data. <c>TradeIds</c>, when non-null, replaces the whole
/// set of trades the firm performs (each must be an existing, active trade); a <c>null</c> leaves the
/// existing trades unchanged (they are managed incrementally via the add/remove-trade operations).
/// </summary>
public sealed record UpdateContractorCommand(
    string Name,
    string? FiscalCode,
    string? RegistrationNumber,
    ContactInfoDto? Contact,
    AddressDto? Address,
    string? Notes,
    IReadOnlyCollection<Guid>? TradeIds);
