import {
  CURRENCIES,
  type BillOfQuantities,
  type Currency,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Turn an ISO timestamp (or null) into the `yyyy-MM-dd` value an <input type="date">
// expects, so an existing BoQ's dates pre-fill the editor.
function toDateInputValue(value?: string | null): string {
  if (!value) return "";
  return new Date(value).toISOString().slice(0, 10);
}

interface BillOfQuantitiesFormProps {
  // A server action taking the submitted FormData (draftBoq / updateBoq).
  action: (formData: FormData) => void | Promise<void>;
  // When drafting, the owning bid, carried as a hidden field for routing/revalidate.
  bidId?: string;
  // When editing, the BoQ whose fields seed the form. Omit to render a blank draft form.
  boq?: BillOfQuantities;
  // For drafting, the currency to pre-select.
  defaultCurrency?: Currency;
  submitLabel: string;
}

/**
 * The draft/edit form for a BoQ's header. The pricing currency is chosen only when
 * drafting — it is fixed for the life of the BoQ (every amount is stored in it), so the
 * edit flow shows it read-only instead. The pinned rate is modelled as "1 EUR = x RON".
 */
export function BillOfQuantitiesForm({
  action,
  bidId,
  boq,
  defaultCurrency,
  submitLabel,
}: BillOfQuantitiesFormProps) {
  const editing = Boolean(boq);

  return (
    <form action={action} className={styles.form}>
      {bidId ? <input type="hidden" name="bidId" value={bidId} /> : null}
      {boq ? <input type="hidden" name="id" value={boq.id} /> : null}

      <input
        name="reference"
        placeholder={t("boq.referencePlaceholder")}
        defaultValue={boq?.reference ?? ""}
      />

      {editing ? (
        // Pricing currency is immutable after drafting; show it, don't submit it.
        <label className={styles.fieldLabel}>
          {t("boq.pricingCurrency")}
          <input value={boq!.pricingCurrency} disabled />
        </label>
      ) : (
        <label className={styles.fieldLabel}>
          {t("boq.pricingCurrency")}
          <select name="pricingCurrency" defaultValue={defaultCurrency ?? "RON"}>
            {CURRENCIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </label>
      )}

      <label className={styles.fieldLabel}>
        {t("boq.pinnedRateField")}
        <input
          name="exchangeRate"
          type="number"
          min={0}
          step="0.0001"
          placeholder={t("boq.pinnedRatePlaceholder")}
          defaultValue={boq?.exchangeRate?.rate ?? ""}
        />
      </label>
      <label className={styles.fieldLabel}>
        {t("boq.rateAsOf")}
        <input
          name="exchangeRateAsOf"
          type="date"
          defaultValue={toDateInputValue(boq?.exchangeRate?.asOf)}
        />
      </label>

      <label className={styles.fieldLabel}>
        {t("boq.submittedOn")}
        <input
          name="submittedOn"
          type="date"
          defaultValue={toDateInputValue(boq?.submittedOn)}
        />
      </label>
      <label className={styles.fieldLabel}>
        {t("boq.validUntil")}
        <input
          name="validUntil"
          type="date"
          defaultValue={toDateInputValue(boq?.validUntil)}
        />
      </label>

      <button type="submit">{submitLabel}</button>
    </form>
  );
}
