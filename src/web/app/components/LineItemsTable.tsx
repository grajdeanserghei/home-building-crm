import Link from "next/link";
import { BoqMappingSelect, type BoqMappingItem } from "@/app/components/BoqMappingSelect";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import type { LineItem, Money } from "@/app/lib/api";
import {
  displayMoney,
  formatMoney,
  formatNumber,
  formatPercent,
  type DisplayCurrency,
} from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface LineItemsTableProps {
  // The rows to render (already ordered as the API returned them).
  lineItems: LineItem[];
  // Resolves a unit-of-measure id to its short code (covers since-retired units).
  unitCode: Map<string, string>;
  // When true, render the per-row edit link and remove button.
  editable: boolean;
  // The owning BoQ and section, carried as hidden fields on the remove form.
  boqId: string;
  sectionId: string;
  // Present when the lines belong to a subsection rather than the section directly;
  // carried as a hidden field so the remove targets the subsection route.
  subsectionId?: string;
  // Route prefix for a line's edit page; the line id and `/edit` are appended per row.
  editHrefBase: string;
  // The server action that removes a line (removeLineItem / removeSubsectionLineItem).
  removeAction: (formData: FormData) => void | Promise<void>;
  // The server action that duplicates a line in place (keyed only by line id, so one action
  // serves both the section and subsection tables).
  duplicateAction: (formData: FormData) => void | Promise<void>;
  // The global display currency (the header toggle) and the "1 EUR = N RON" rate to convert with.
  // Optional: when omitted (e.g. arrange mode), prices render in their own currency, unconverted.
  displayCurrency?: DisplayCurrency;
  ronPerEur?: number;
  // When set, render a per-line valuation-catalog mapping select (the finest link granularity).
  // Omitted when the project has no catalog / no active items. `disabled` is true when a coarser
  // link (whole section or subsection) already covers these lines.
  mapping?: {
    projectId: string;
    catalogId: string;
    bidId: string;
    catalogItems: BoqMappingItem[];
    // Item currently linked to a line, keyed by line id (undefined ⇒ unmapped).
    linkedItemByLine: Record<string, string>;
    disabled: boolean;
  };
}

/**
 * The priced-lines table shared by a section and its subsections — identical columns and
 * row actions, differing only in which route a row edits and which action removes it.
 * Renders the empty-state message when there are no lines.
 */
export function LineItemsTable({
  lineItems,
  unitCode,
  editable,
  boqId,
  sectionId,
  subsectionId,
  editHrefBase,
  removeAction,
  duplicateAction,
  displayCurrency,
  ronPerEur,
  mapping,
}: LineItemsTableProps) {
  if (lineItems.length === 0) {
    return <p>{t("lineItems.empty")}</p>;
  }

  // Format for the chosen display currency when one is set (Original keeps decimals; RON/EUR convert
  // and drop them — see displayMoney); otherwise render as-is in the pricing currency.
  const money = (m: Money) =>
    displayCurrency && ronPerEur
      ? displayMoney(m, displayCurrency, ronPerEur)
      : formatMoney(m);

  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th>#</th>
          <th>{t("common.description")}</th>
          <th>{t("lineItems.col.unit")}</th>
          <th>{t("lineItems.col.qty")}</th>
          <th>{t("lineItems.col.unitPriceExclVat")}</th>
          <th>{t("lineItems.col.vat")}</th>
          <th>{t("lineItems.col.lineTotalExclVat")}</th>
          <th>{t("lineItems.col.lineTotalInclVat")}</th>
          {mapping ? <th>{t("lineItems.col.mapping")}</th> : null}
          {editable ? <th aria-label={t("common.actions")} /> : null}
        </tr>
      </thead>
      <tbody>
        {lineItems.map((li) => (
          <tr key={li.id}>
            <td>{li.sequence}</td>
            <td>
              <strong>{li.description}</strong>
              {li.notes ? <div className={styles.muted}>{li.notes}</div> : null}
            </td>
            <td>{unitCode.get(li.unitOfMeasureId) ?? "—"}</td>
            <td>{formatNumber(li.quantity)}</td>
            <td>{money(li.unitPrice)}</td>
            <td>{formatPercent(li.vatRatePercentage)}</td>
            <td>{money(li.lineTotal)}</td>
            <td>{money(li.lineTotalWithVat)}</td>
            {mapping ? (
              <td>
                <BoqMappingSelect
                  projectId={mapping.projectId}
                  catalogId={mapping.catalogId}
                  boqId={boqId}
                  bidId={mapping.bidId}
                  sectionId={sectionId}
                  subsectionId={subsectionId ?? null}
                  lineItemId={li.id}
                  catalogItems={mapping.catalogItems}
                  linkedItemId={mapping.linkedItemByLine[li.id]}
                  disabled={mapping.disabled}
                  compact
                />
              </td>
            ) : null}
            {editable ? (
              <td>
                <div className={styles.actions}>
                  <Link href={`${editHrefBase}/${li.id}/edit`} className={styles.edit}>
                    {t("common.edit")}
                  </Link>
                  <form action={duplicateAction}>
                    <input type="hidden" name="boqId" value={boqId} />
                    <input type="hidden" name="lineItemId" value={li.id} />
                    <button type="submit" className={styles.edit}>
                      {t("common.duplicate")}
                    </button>
                  </form>
                  <ConfirmDeleteButton
                    action={removeAction}
                    fields={{
                      boqId,
                      sectionId,
                      ...(subsectionId ? { subsectionId } : {}),
                      lineItemId: li.id,
                    }}
                    title={t("lineItems.removeTitle")}
                    bodyTemplate={t("lineItems.removeBody")}
                    name={li.description}
                    triggerLabel={t("common.remove")}
                    confirmLabel={t("common.remove")}
                  />
                </div>
              </td>
            ) : null}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
