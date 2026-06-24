namespace HomeProjectManagement.Application.BillsOfQuantities;

/// <summary>
/// Driven (secondary) port that renders a bill of quantities to a spreadsheet workbook. The
/// application service builds a fully-resolved <see cref="BoqExportModel"/> (unit codes and the
/// contractor/work-package names already looked up) and hands it here; the Infrastructure adapter
/// does pure rendering with no repository access, keeping the spreadsheet library out of the core.
/// </summary>
public interface IBoqSpreadsheetExporter
{
    BoqExportFile Export(BoqExportModel model);
}

/// <summary>
/// Everything the exporter needs, already resolved by the application service: the BoQ read model,
/// a unit-id → code lookup for the "U.M." column, the owning bid's contractor and work-package
/// names (for the file name), and the generation timestamp (for the file name's date).
/// </summary>
public sealed record BoqExportModel(
    BillOfQuantitiesDto Boq,
    string ContractorName,
    string WorkPackageName,
    IReadOnlyDictionary<Guid, string> UnitCodes,
    DateTimeOffset GeneratedAt);

/// <summary>A rendered workbook ready to stream back to the client.</summary>
public sealed record BoqExportFile(byte[] Content, string FileName, string ContentType);
