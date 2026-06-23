import { CURRENCIES, type Contract, type Currency } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Turn an ISO timestamp (or null) into the `yyyy-MM-dd` value an <input type="date">
// expects, so an existing contract's dates pre-fill the editor.
function toDateInputValue(value?: string | null): string {
  if (!value) return "";
  return new Date(value).toISOString().slice(0, 10);
}

interface ContractFormProps {
  // A server action taking the submitted FormData (updateContract).
  action: (formData: FormData) => void | Promise<void>;
  // The contract whose fields seed the form.
  contract: Contract;
  submitLabel: string;
}

/**
 * The edit form for a contract's header (reference number, agreed value, planned
 * dates, notes). The awarded work package and accepted BoQ are fixed for the life of
 * the contract, so they are not editable here. Signing / completion dates are recorded
 * through the status control, not this form.
 */
export function ContractForm({
  action,
  contract,
  submitLabel,
}: ContractFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="id" value={contract.id} />

      <input
        name="contractNumber"
        placeholder={t("contracts.contractNumberPlaceholder")}
        defaultValue={contract.contractNumber ?? ""}
      />

      <label className={styles.fieldLabel}>
        {t("contracts.agreedValue")}
        <input
          name="valueAmount"
          type="number"
          min={0}
          step="0.01"
          placeholder={t("contracts.valuePlaceholder")}
          defaultValue={contract.value.amount}
        />
      </label>
      <label className={styles.fieldLabel}>
        {t("contracts.currency")}
        <select
          name="valueCurrency"
          defaultValue={contract.value.currency as Currency}
        >
          {CURRENCIES.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
      </label>

      <label className={styles.fieldLabel}>
        {t("contracts.startDate")}
        <input
          name="startDate"
          type="date"
          defaultValue={toDateInputValue(contract.startDate)}
        />
      </label>
      <label className={styles.fieldLabel}>
        {t("contracts.plannedEndDate")}
        <input
          name="plannedEndDate"
          type="date"
          defaultValue={toDateInputValue(contract.plannedEndDate)}
        />
      </label>

      <input
        name="notes"
        placeholder={t("contracts.notesPlaceholder")}
        defaultValue={contract.notes ?? ""}
      />

      <button type="submit">{submitLabel}</button>
    </form>
  );
}
