using ClosedXML.Excel;
using HomeProjectManagement.Application.BillsOfQuantities;

namespace HomeProjectManagement.Infrastructure.Export;

/// <summary>
/// Driven adapter that renders a <see cref="BoqExportModel"/> to an <c>.xlsx</c> workbook with
/// ClosedXML — the only type that touches the spreadsheet library. Layout: a leading "Rezumat"
/// (summary) sheet, then one worksheet per section, with each subsection rendered as a visually
/// separated band. Money values are <b>live Excel formulas</b> (<c>=qty*price</c>, <c>SUM</c>s) so the
/// workbook recalculates if edited and reconciles with the domain figures on open. Romanian labels
/// throughout, matching the web UI's glossary.
/// </summary>
public sealed class ClosedXmlBoqExporter : IBoqSpreadsheetExporter
{
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    // Column layout, shared by every section sheet (and the summary's value columns).
    private const int ColIndex = 1;       // Nr.
    private const int ColDescription = 2; // Descriere
    private const int ColUnit = 3;        // U.M.
    private const int ColQuantity = 4;    // Cantitate
    private const int ColUnitPrice = 5;   // Preț unitar (fără TVA)
    private const int ColVat = 6;         // TVA
    private const int ColExcl = 7;        // Valoare (fără TVA)
    private const int ColIncl = 8;        // Valoare (cu TVA)
    private const int ColumnCount = 8;

    private const string MoneyFormat = "#,##0.00";
    private const string QuantityFormat = "#,##0.####";
    private const string VatFormat = "0.##\"%\"";

    private static readonly XLColor HeaderFill = XLColor.FromArgb(0x33, 0x33, 0x33);
    private static readonly XLColor BandFill = XLColor.FromArgb(0xE8, 0xE8, 0xE8);
    private static readonly XLColor TotalFill = XLColor.FromArgb(0xF2, 0xF2, 0xF2);

    public BoqExportFile Export(BoqExportModel model)
    {
        var boq = model.Boq;
        var currency = boq.PricingCurrency.ToString();

        using var workbook = new XLWorkbook();

        // The summary is added first so it is the leading tab; it is populated last, once each
        // section sheet exists and its total-row address is known (the summary references them).
        var summary = workbook.AddWorksheet("Rezumat");

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sectionRefs = new List<SectionRef>();

        foreach (var section in boq.Sections)
        {
            var sheetName = SafeSheetName($"{section.Sequence:00} {section.Name}", usedNames);
            var sheet = workbook.AddWorksheet(sheetName);
            var totalRow = BuildSectionSheet(sheet, section, model);
            sectionRefs.Add(new SectionRef(sheetName, section.Sequence, section.Name, totalRow));
        }

        BuildSummarySheet(summary, model, currency, sectionRefs);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new BoqExportFile(stream.ToArray(), BuildFileName(model), XlsxContentType);
    }

    // Renders one section onto its sheet and returns the row holding the section total.
    private static int BuildSectionSheet(IXLWorksheet ws, SectionDto section, BoqExportModel model)
    {
        var row = 1;

        // Title band.
        ws.Cell(row, ColIndex).Value = $"{section.Sequence}. {section.Name}";
        var title = ws.Range(row, ColIndex, row, ColumnCount).Merge();
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;
        row++;

        if (!string.IsNullOrWhiteSpace(section.Description))
        {
            ws.Cell(row, ColIndex).Value = section.Description;
            var desc = ws.Range(row, ColIndex, row, ColumnCount).Merge();
            desc.Style.Font.Italic = true;
            desc.Style.Font.FontColor = XLColor.Gray;
            row++;
        }

        row++; // spacer

        var headerRow = row;
        WriteHeader(ws, headerRow);
        ws.SheetView.FreezeRows(headerRow); // keep the header visible while scrolling
        row++;

        // Direct (non-subsection) line items sit between the header and the first band.
        var directRows = new List<int>();
        var lineNumber = 1;
        foreach (var line in section.LineItems)
        {
            WriteLineItem(ws, row, lineNumber++, line, model);
            directRows.Add(row);
            row++;
        }

        // Each subsection: a spacer, a shaded title band, its lines, then a bold subtotal row.
        var subtotalRows = new List<int>();
        foreach (var subsection in section.Subsections)
        {
            row++; // spacer separating this band from what precedes it

            ws.Cell(row, ColIndex).Value = $"{section.Sequence}.{subsection.Sequence} {subsection.Name}";
            var band = ws.Range(row, ColIndex, row, ColumnCount).Merge();
            band.Style.Font.Bold = true;
            band.Style.Fill.BackgroundColor = BandFill;
            band.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            band.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            row++;

            var subLineRows = new List<int>();
            var subLineNumber = 1;
            foreach (var line in subsection.LineItems)
            {
                WriteLineItem(ws, row, subLineNumber++, line, model);
                subLineRows.Add(row);
                row++;
            }

            // Subtotal of just this subsection.
            ws.Cell(row, ColDescription).Value = $"Subtotal {subsection.Name}";
            SetSumFormula(ws, row, subLineRows);
            StyleTotalRow(ws, row, bold: true, doubleTop: false);
            subtotalRows.Add(row);
            row++;
        }

        row++; // spacer before the grand section total

        var totalRow = row;
        ws.Cell(totalRow, ColDescription).Value = $"TOTAL {section.Name}";
        // The section total = the contiguous direct lines + each subsection subtotal cell.
        ws.Cell(totalRow, ColExcl).FormulaA1 = SectionTotalFormula(ColExcl, directRows, subtotalRows);
        ws.Cell(totalRow, ColIncl).FormulaA1 = SectionTotalFormula(ColIncl, directRows, subtotalRows);
        StyleTotalRow(ws, totalRow, bold: true, doubleTop: true);

        ApplyColumnFormatting(ws);
        return totalRow;
    }

    private static void BuildSummarySheet(
        IXLWorksheet ws,
        BoqExportModel model,
        string currency,
        IReadOnlyList<SectionRef> sections)
    {
        var boq = model.Boq;
        var row = 1;

        ws.Cell(row, 1).Value = $"Deviz: {model.WorkPackageName}";
        var heading = ws.Range(row, 1, row, 3).Merge();
        heading.Style.Font.Bold = true;
        heading.Style.Font.FontSize = 14;
        row += 2;

        // Header block: label/value pairs.
        WriteMeta(ws, ref row, "Contractor", model.ContractorName);
        WriteMeta(ws, ref row, "Referință", string.IsNullOrWhiteSpace(boq.Reference) ? "—" : boq.Reference!);
        WriteMeta(ws, ref row, "Status", StatusLabel(boq.Status));
        WriteMeta(ws, ref row, "Monedă", currency);
        WriteMeta(ws, ref row, "Depus", FormatDate(boq.SubmittedOn));
        WriteMeta(ws, ref row, "Valabil până", FormatDate(boq.ValidUntil));
        row++;

        // Totals table: one row per section, then a grand total.
        var tableHeader = row;
        ws.Cell(row, 1).Value = "Secțiune";
        ws.Cell(row, 2).Value = $"Valoare (fără TVA) [{currency}]";
        ws.Cell(row, 3).Value = $"Valoare (cu TVA) [{currency}]";
        var head = ws.Range(row, 1, row, 3);
        head.Style.Font.Bold = true;
        head.Style.Font.FontColor = XLColor.White;
        head.Style.Fill.BackgroundColor = HeaderFill;
        row++;

        var firstDataRow = row;
        foreach (var section in sections)
        {
            ws.Cell(row, 1).Value = $"{section.Sequence:00} {section.Name}";
            var sheetRef = SheetReference(section.SheetName);
            // Pull each section's total straight from its sheet, so the summary always agrees with it.
            ws.Cell(row, 2).FormulaA1 = $"{sheetRef}!{ColumnLetter(ColExcl)}{section.TotalRow}";
            ws.Cell(row, 3).FormulaA1 = $"{sheetRef}!{ColumnLetter(ColIncl)}{section.TotalRow}";
            row++;
        }

        var lastDataRow = row - 1;

        ws.Cell(row, 1).Value = "TOTAL";
        if (sections.Count > 0)
        {
            ws.Cell(row, 2).FormulaA1 = $"SUM(B{firstDataRow}:B{lastDataRow})";
            ws.Cell(row, 3).FormulaA1 = $"SUM(C{firstDataRow}:C{lastDataRow})";
        }
        else
        {
            ws.Cell(row, 2).Value = 0;
            ws.Cell(row, 3).Value = 0;
        }

        var totalBand = ws.Range(row, 1, row, 3);
        totalBand.Style.Font.Bold = true;
        totalBand.Style.Fill.BackgroundColor = TotalFill;
        totalBand.Style.Border.TopBorder = XLBorderStyleValues.Double;

        // Money formatting on the two value columns of the table (header → total).
        ws.Range(tableHeader + 1, 2, row, 3).Style.NumberFormat.Format = MoneyFormat;

        ws.Column(1).Width = 36;
        ws.Column(2).Width = 22;
        ws.Column(3).Width = 22;
    }

    private static void WriteHeader(IXLWorksheet ws, int row)
    {
        ws.Cell(row, ColIndex).Value = "Nr.";
        ws.Cell(row, ColDescription).Value = "Descriere";
        ws.Cell(row, ColUnit).Value = "U.M.";
        ws.Cell(row, ColQuantity).Value = "Cantitate";
        ws.Cell(row, ColUnitPrice).Value = "Preț unitar (fără TVA)";
        ws.Cell(row, ColVat).Value = "TVA";
        ws.Cell(row, ColExcl).Value = "Valoare (fără TVA)";
        ws.Cell(row, ColIncl).Value = "Valoare (cu TVA)";

        var header = ws.Range(row, ColIndex, row, ColumnCount);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = HeaderFill;
        header.Style.Alignment.WrapText = true;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
    }

    private static void WriteLineItem(IXLWorksheet ws, int row, int number, LineItemDto line, BoqExportModel model)
    {
        ws.Cell(row, ColIndex).Value = number;
        ws.Cell(row, ColDescription).Value = line.Description;
        ws.Cell(row, ColUnit).Value = model.UnitCodes.TryGetValue(line.UnitOfMeasureId, out var code) ? code : string.Empty;
        ws.Cell(row, ColQuantity).Value = line.Quantity;
        ws.Cell(row, ColUnitPrice).Value = line.UnitPrice.Amount;
        ws.Cell(row, ColVat).Value = line.VatRatePercentage;

        // Live math: net line total = qty × unit price; gross = net × (1 + vat%/100).
        // ClosedXML formulas are written without a leading "=".
        ws.Cell(row, ColExcl).FormulaA1 = $"{ColumnLetter(ColQuantity)}{row}*{ColumnLetter(ColUnitPrice)}{row}";
        ws.Cell(row, ColIncl).FormulaA1 = $"{ColumnLetter(ColExcl)}{row}*(1+{ColumnLetter(ColVat)}{row}/100)";
    }

    // A subtotal over a contiguous block of line rows (or zero when the block is empty).
    private static void SetSumFormula(IXLWorksheet ws, int row, IReadOnlyList<int> lineRows)
    {
        if (lineRows.Count == 0)
        {
            ws.Cell(row, ColExcl).Value = 0;
            ws.Cell(row, ColIncl).Value = 0;
            return;
        }

        var first = lineRows[0];
        var last = lineRows[^1];
        ws.Cell(row, ColExcl).FormulaA1 = $"SUM({ColumnLetter(ColExcl)}{first}:{ColumnLetter(ColExcl)}{last})";
        ws.Cell(row, ColIncl).FormulaA1 = $"SUM({ColumnLetter(ColIncl)}{first}:{ColumnLetter(ColIncl)}{last})";
    }

    // Section total = SUM over the contiguous direct lines + each subsection's subtotal cell.
    private static string SectionTotalFormula(int column, IReadOnlyList<int> directRows, IReadOnlyList<int> subtotalRows)
    {
        var letter = ColumnLetter(column);
        var parts = new List<string>();
        if (directRows.Count > 0)
        {
            parts.Add($"SUM({letter}{directRows[0]}:{letter}{directRows[^1]})");
        }

        parts.AddRange(subtotalRows.Select(r => $"{letter}{r}"));
        return parts.Count == 0 ? "0" : string.Join("+", parts);
    }

    private static void StyleTotalRow(IXLWorksheet ws, int row, bool bold, bool doubleTop)
    {
        var range = ws.Range(row, ColIndex, row, ColumnCount);
        range.Style.Font.Bold = bold;
        range.Style.Fill.BackgroundColor = TotalFill;
        range.Style.Border.TopBorder = doubleTop ? XLBorderStyleValues.Double : XLBorderStyleValues.Thin;
        ws.Cell(row, ColExcl).Style.NumberFormat.Format = MoneyFormat;
        ws.Cell(row, ColIncl).Style.NumberFormat.Format = MoneyFormat;
    }

    private static void ApplyColumnFormatting(IXLWorksheet ws)
    {
        ws.Column(ColIndex).Width = 5;
        ws.Column(ColDescription).Width = 44;
        ws.Column(ColUnit).Width = 8;
        ws.Column(ColQuantity).Width = 12;
        ws.Column(ColUnitPrice).Width = 16;
        ws.Column(ColVat).Width = 8;
        ws.Column(ColExcl).Width = 16;
        ws.Column(ColIncl).Width = 16;

        ws.Column(ColQuantity).Style.NumberFormat.Format = QuantityFormat;
        ws.Column(ColUnitPrice).Style.NumberFormat.Format = MoneyFormat;
        ws.Column(ColVat).Style.NumberFormat.Format = VatFormat;
        ws.Column(ColExcl).Style.NumberFormat.Format = MoneyFormat;
        ws.Column(ColIncl).Style.NumberFormat.Format = MoneyFormat;
    }

    private static void WriteMeta(IXLWorksheet ws, ref int row, string label, string value)
    {
        ws.Cell(row, 1).Value = $"{label}:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = value;
        row++;
    }

    // Excel sheet names: ≤ 31 chars, none of : \ / ? * [ ], non-blank, unique (case-insensitive).
    private static string SafeSheetName(string raw, HashSet<string> used)
    {
        var cleaned = new string(raw.Select(c => "\\/?*[]:".Contains(c) ? ' ' : c).ToArray()).Trim();
        if (cleaned.Length == 0)
        {
            cleaned = "Secțiune";
        }

        if (cleaned.Length > 31)
        {
            cleaned = cleaned[..31].Trim();
        }

        var candidate = cleaned;
        var suffix = 2;
        while (!used.Add(candidate))
        {
            var tag = $" ({suffix++})";
            var keep = Math.Min(cleaned.Length, 31 - tag.Length);
            candidate = cleaned[..keep].Trim() + tag;
        }

        return candidate;
    }

    // A worksheet reference for use in a formula, quoted and with embedded apostrophes doubled.
    private static string SheetReference(string sheetName) => $"'{sheetName.Replace("'", "''")}'";

    private static string ColumnLetter(int column) => XLHelper.GetColumnLetterFromNumber(column);

    private static string StatusLabel(Domain.BillsOfQuantities.BoqStatus status) => status switch
    {
        Domain.BillsOfQuantities.BoqStatus.Draft => "Ciornă",
        Domain.BillsOfQuantities.BoqStatus.Submitted => "Trimis",
        Domain.BillsOfQuantities.BoqStatus.Accepted => "Acceptat",
        Domain.BillsOfQuantities.BoqStatus.Rejected => "Respins",
        Domain.BillsOfQuantities.BoqStatus.Withdrawn => "Retras",
        _ => status.ToString(),
    };

    private static string FormatDate(DateTimeOffset? value) =>
        value is { } v ? v.ToString("dd.MM.yyyy") : "—";

    // {Contractor}_{WorkPackage}_{yyyy-MM-dd}.xlsx, with characters illegal in file names removed.
    private static string BuildFileName(BoqExportModel model)
    {
        var date = model.GeneratedAt.ToString("yyyy-MM-dd");
        var stem = $"{Clean(model.ContractorName)}_{Clean(model.WorkPackageName)}_{date}";
        return stem + ".xlsx";

        static string Clean(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(value.Select(c => invalid.Contains(c) ? ' ' : c).ToArray());
            // Collapse runs of whitespace and trim, so the name stays tidy.
            sanitized = string.Join(' ', sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(sanitized) ? "deviz" : sanitized;
        }
    }

    private readonly record struct SectionRef(string SheetName, int Sequence, string Name, int TotalRow);
}
