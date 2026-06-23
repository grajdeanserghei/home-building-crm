import {
  DEFAULT_VAT_RATE_PERCENTAGE,
  type Currency,
  type LineItem,
  type UnitOfMeasure,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface LineItemFormProps {
  // The server action taking the submitted FormData (addLineItem / reviseLineItem).
  action: (formData: FormData) => void | Promise<void>;
  // The owning BoQ and section, carried as hidden fields for routing/revalidate.
  boqId: string;
  sectionId: string;
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
  // Submit-button caption — defaults to the "add" label.
  submitLabel?: string;
}

/**
 * The line-item form for a section, used for both adding and editing. The unit price is
 * entered as a bare net (VAT-exclusive) amount in the BoQ's pricing currency; the VAT
 * rate defaults to 21% but can be overridden per line. The unit of measure must be one
 * of the active canonical units (the backend rejects an inactive one).
 *
 * Pass `lineItem` to edit an existing row — its values seed the inputs and its id is
 * submitted as a hidden field. Omit it to add a fresh row.
 */
export function LineItemForm({
  action,
  boqId,
  sectionId,
  currency,
  units,
  defaultSequence,
  lineItem,
  submitLabel,
}: LineItemFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="boqId" value={boqId} />
      <input type="hidden" name="sectionId" value={sectionId} />
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
      <button type="submit">{submitLabel ?? t("lineItems.add")}</button>
    </form>
  );
}
