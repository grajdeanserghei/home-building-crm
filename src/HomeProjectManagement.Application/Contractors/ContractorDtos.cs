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

/// <summary>Input for registering a contractor firm. Only the name is required.</summary>
public sealed record RegisterContractorCommand(
    string Name,
    string? FiscalCode,
    string? RegistrationNumber,
    ContactInfoDto? Contact,
    AddressDto? Address,
    string? Notes);

/// <summary>Input for editing a contractor's master data.</summary>
public sealed record UpdateContractorCommand(
    string Name,
    string? FiscalCode,
    string? RegistrationNumber,
    ContactInfoDto? Contact,
    AddressDto? Address,
    string? Notes);
