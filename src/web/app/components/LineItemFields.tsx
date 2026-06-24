import {
  DEFAULT_VAT_RATE_PERCENTAGE,
  type Currency,
  type LineItem,
  type UnitOfMeasure,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export interface LineItemFieldsProps {
  // The owning BoQ and section, carried as hidden fields for routing/revalidate.
  boqId: string;
  sectionId: string;
  // Present when the line belongs to a subsection rather than the section directly;
  // carried as a hidden field so the action targets the subsection route.
  subsectionId?: string;
  // The BoQ's pricing currency — every line price is in it (carried as a hidden field,
  // not chosen per line, so the bill stays single-currency).
  currency: Currency;
  // The canonical units a line may reference (active ones when adding; when editing,
  // include the line's current unit too so a since-retired unit still renders).
  units: UnitOfMeasure[];
  // Suggested next order within the section (1-based). Used only when adding.
  defaultSequence?: number;
  // When present, the form edits this existing line: fields are seeded with its values
  // and its id is carried as a hidden field for the PUT route.
  lineItem?: LineItem;
}

/**
 * The inputs of the line-item form (hidden routing fields plus the visible priced-line
 * fields), without the surrounding `<form>` or submit control. Shared by both the
 * full-page form (`LineItemForm`) and the intercepting-route overlay (`ModalLineItemForm`)
 * so the two stay identical field-for-field.
 *
 * The unit price is a bare net (VAT-exclusive) amount in the BoQ's pricing currency; the VAT
 * rate defaults to 21% but can be overridden per line. The unit of measure must be one of the
 * supplied units (the backend rejects an inactive one). Pass `lineItem` to seed edit values.
 */
export function LineItemFields({
  boqId,
  sectionId,
  subsectionId,
  currency,
  units,
  defaultSequence,
  lineItem,
}: LineItemFieldsProps) {
  return (
    <>
      <input type="hidden" name="boqId" value={boqId} />
      <input type="hidden" name="sectionId" value={sectionId} />
      {subsectionId ? (
        <input type="hidden" name="subsectionId" value={subsectionId} />
      ) : null}
      <input type="hidden" name="currency" value={currency} />
      {lineItem ? (
        <input type="hidden" name="lineItemId" value={lineItem.id} />
      ) : null}

      <input
        name="description"
        placeholder={t("lineItems.descriptionPlaceholder")}
        defaultValue={lineItem?.description}
        required
      />
      <input
        name="quantity"
        type="number"
        min={0}
        step="any"
        placeholder={t("lineItems.quantityPlaceholder")}
        defaultValue={lineItem?.quantity}
        required
      />
      <label className={styles.fieldLabel}>
        {t("lineItems.col.unit")}
        <select
          name="unitOfMeasureId"
          required
          defaultValue={lineItem?.unitOfMeasureId ?? ""}
        >
          <option value="" disabled>
            {t("lineItems.selectUnit")}
          </option>
          {units.map((u) => (
            <option key={u.id} value={u.id}>
              {u.code} — {u.name}
            </option>
          ))}
        </select>
      </label>
      <label className={styles.fieldLabel}>
        {t("lineItems.unitPriceExclVatLabel", { currency })}
        <input
          name="unitPriceAmount"
          type="number"
          min={0}
          step="0.01"
          placeholder="0.00"
          defaultValue={lineItem?.unitPrice.amount}
          required
        />
      </label>
      <label className={styles.fieldLabel}>
        {t("lineItems.vatRateLabel")}
        <input
          name="vatRatePercentage"
          type="number"
          min={0}
          max={100}
          step="0.01"
          placeholder="21"
          defaultValue={lineItem?.vatRatePercentage ?? DEFAULT_VAT_RATE_PERCENTAGE}
          required
        />
      </label>
      <input
        name="sequence"
        type="number"
        min={1}
        step={1}
        placeholder={t("sections.orderPlaceholder")}
        defaultValue={lineItem?.sequence ?? defaultSequence ?? 1}
      />
      <input
        name="notes"
        placeholder={t("boq.notesPlaceholder")}
        defaultValue={lineItem?.notes ?? undefined}
      />
    </>
  );
}
