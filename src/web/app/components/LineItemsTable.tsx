import Link from "next/link";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import type { LineItem } from "@/app/lib/api";
import { formatMoney, formatNumber, formatPercent } from "@/app/lib/format";
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
}: LineItemsTableProps) {
  if (lineItems.length === 0) {
    return <p>{t("lineItems.empty")}</p>;
  }

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
            <td>{formatMoney(li.unitPrice)}</td>
            <td>{formatPercent(li.vatRatePercentage)}</td>
            <td>{formatMoney(li.lineTotal)}</td>
            <td>{formatMoney(li.lineTotalWithVat)}</td>
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
