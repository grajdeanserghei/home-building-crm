import {
  CURRENCIES,
  DEFAULT_VAT_RATE_PERCENTAGE,
  type ValuationCatalog,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface ValuationCatalogFormProps {
  // The server action taking the submitted FormData (create or update).
  action: (formData: FormData) => void | Promise<void>;
  // The owning project, carried as a hidden field for routing/revalidate.
  projectId: string;
  // When present, the form edits this existing catalog: fields are seeded and its id is carried
  // as a hidden field. VAT and currency are edited elsewhere (VAT recompute is a separate action),
  // so on edit they are omitted from this form.
  catalog?: ValuationCatalog;
  submitLabel?: string;
}

/**
 * The valuation-catalog header form, used for both creating and editing. On create it seeds the
 * method (segregated-cost), currency (RON) and VAT so the appraiser's report header is captured
 * in one step; on edit only the reference, surfaces and own-regie adjustment are editable (VAT
 * has its own recompute control on the hub, and the method/currency are fixed).
 */
export function ValuationCatalogForm({
  action,
  projectId,
  catalog,
  submitLabel,
}: ValuationCatalogFormProps) {
  const editing = catalog !== undefined;

  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="projectId" value={projectId} />
      {catalog ? (
        <input type="hidden" name="catalogId" value={catalog.id} />
      ) : null}

      <input
        name="catalogReference"
        placeholder={t("valuation.form.catalogReference")}
        defaultValue={catalog?.catalogReference}
        required
      />
      <input
        name="builtArea"
        type="number"
        min={0}
        step="0.01"
        placeholder={t("valuation.form.builtArea")}
        defaultValue={catalog?.builtArea}
      />
      <input
        name="grossFloorArea"
        type="number"
        min={0}
        step="0.01"
        placeholder={t("valuation.form.grossFloorArea")}
        defaultValue={catalog?.grossFloorArea}
      />
      <input
        name="usableArea"
        type="number"
        min={0}
        step="0.01"
        placeholder={t("valuation.form.usableArea")}
        defaultValue={catalog?.usableArea}
      />
      <input
        name="ownRegieAdjustment"
        type="number"
        min={0}
        step="0.01"
        placeholder={t("valuation.form.ownRegieAdjustment")}
        defaultValue={catalog?.ownRegieAdjustment ?? 0.2}
      />

      {/* VAT and currency are set at creation only; on edit VAT changes go through the hub's
          dedicated recompute action and the currency is fixed. */}
      {editing ? null : (
        <>
          <input
            name="vatRatePercentage"
            type="number"
            min={0}
            step="0.01"
            placeholder={t("valuation.form.vatRate")}
            defaultValue={DEFAULT_VAT_RATE_PERCENTAGE}
          />
          <select name="currency" defaultValue="RON">
            {CURRENCIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </>
      )}

      <button type="submit">{submitLabel ?? t("valuation.form.save")}</button>
    </form>
  );
}
