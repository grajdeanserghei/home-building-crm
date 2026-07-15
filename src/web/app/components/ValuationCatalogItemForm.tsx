import type { Currency, ValuationCatalogItem } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface ValuationCatalogItemFormProps {
  // The server action taking the submitted FormData (add or revise).
  action: (formData: FormData) => void | Promise<void>;
  projectId: string;
  catalogId: string;
  // The catalog's pricing currency (RON), carried hidden so the action can build Money values.
  currency: Currency;
  // When present, the form revises this existing item; else it adds a fresh one.
  item?: ValuationCatalogItem;
  // Suggested next order (one past the existing items). Used only when adding.
  defaultSequence?: number;
  submitLabel?: string;
}

/**
 * The valuation catalog-item form, used for both adding and revising. Fields follow the
 * appraiser's sheet columns; `unit` is free text (mp/mc/%/lei — deliberately not the
 * UnitOfMeasure vocabulary). The gross total (Cost cu TVA) is derived server-side from the
 * catalog's VAT rate, so it is shown as a hint rather than entered.
 */
export function ValuationCatalogItemForm({
  action,
  projectId,
  catalogId,
  currency,
  item,
  defaultSequence,
  submitLabel,
}: ValuationCatalogItemFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="projectId" value={projectId} />
      <input type="hidden" name="catalogId" value={catalogId} />
      <input type="hidden" name="currency" value={currency} />
      {item ? <input type="hidden" name="itemId" value={item.id} /> : null}

      <input
        name="printedNumber"
        placeholder={t("valuation.item.field.printedNumber")}
        defaultValue={item?.printedNumber}
        required
      />
      <input
        name="sequence"
        type="number"
        min={1}
        step={1}
        placeholder={t("valuation.item.field.sequence")}
        defaultValue={item?.sequence ?? defaultSequence ?? 1}
      />
      <input
        name="name"
        placeholder={t("valuation.item.field.name")}
        defaultValue={item?.name}
        required
      />
      <input
        name="catalogSource"
        placeholder={t("valuation.item.field.source")}
        defaultValue={item?.catalogSource}
        required
      />
      <input
        name="unit"
        placeholder={t("valuation.item.field.unit")}
        defaultValue={item?.unit}
        required
      />
      <input
        name="unitCostAmount"
        type="number"
        min={0}
        step="0.01"
        placeholder={t("valuation.item.field.unitCost")}
        defaultValue={item?.unitCostPerBuiltArea.amount}
      />
      <input
        name="costWeight"
        type="number"
        min={0}
        step="0.0001"
        placeholder={t("valuation.item.field.weight")}
        defaultValue={item?.costWeight}
      />
      <input
        name="totalCostAmount"
        type="number"
        min={0}
        step="0.01"
        placeholder={t("valuation.item.field.costNet")}
        defaultValue={item?.totalCostWithoutVat.amount}
      />

      <p className={styles.muted}>{t("valuation.item.field.grossHint")}</p>

      <button type="submit">{submitLabel ?? t("valuation.item.save")}</button>
    </form>
  );
}
