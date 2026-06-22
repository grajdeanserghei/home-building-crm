import { type Currency, type UnitOfMeasure } from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface LineItemFormProps {
  // The server action taking the submitted FormData (addLineItem).
  action: (formData: FormData) => void | Promise<void>;
  // The owning BoQ and section, carried as hidden fields for routing/revalidate.
  boqId: string;
  sectionId: string;
  // The BoQ's pricing currency — every line price is in it (carried as a hidden field,
  // not chosen per line, so the bill stays single-currency).
  currency: Currency;
  // The active canonical units a line may reference.
  units: UnitOfMeasure[];
  // Suggested next order within the section (1-based).
  defaultSequence?: number;
}

/**
 * The add-a-line-item form for a section. The unit price is entered as a bare amount in
 * the BoQ's pricing currency; the unit of measure must be one of the active canonical
 * units (the backend rejects an inactive one).
 */
export function LineItemForm({
  action,
  boqId,
  sectionId,
  currency,
  units,
  defaultSequence,
}: LineItemFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="boqId" value={boqId} />
      <input type="hidden" name="sectionId" value={sectionId} />
      <input type="hidden" name="currency" value={currency} />

      <input
        name="description"
        placeholder="Line description (e.g. C25/30 concrete)"
        required
      />
      <input
        name="quantity"
        type="number"
        min={0}
        step="any"
        placeholder="Quantity"
        required
      />
      <label className={styles.fieldLabel}>
        Unit
        <select name="unitOfMeasureId" required defaultValue="">
          <option value="" disabled>
            Select a unit…
          </option>
          {units.map((u) => (
            <option key={u.id} value={u.id}>
              {u.code} — {u.name}
            </option>
          ))}
        </select>
      </label>
      <label className={styles.fieldLabel}>
        Unit price ({currency})
        <input
          name="unitPriceAmount"
          type="number"
          min={0}
          step="0.01"
          placeholder="0.00"
          required
        />
      </label>
      <input
        name="sequence"
        type="number"
        min={1}
        step={1}
        placeholder="Order"
        defaultValue={defaultSequence ?? 1}
      />
      <input name="notes" placeholder="Notes (optional)" />
      <button type="submit">Add line item</button>
    </form>
  );
}
