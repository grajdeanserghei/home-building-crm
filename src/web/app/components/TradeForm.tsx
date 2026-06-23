import { type Trade } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface TradeFormProps {
  // A server action that takes the submitted FormData (defineTrade / updateTrade).
  action: (formData: FormData) => void | Promise<void>;
  // When editing, the trade whose fields seed the form. Omit to render a blank "create" form.
  trade?: Trade;
  submitLabel: string;
}

/**
 * The create/edit form for a trade. Field-for-field identical for both flows; only the
 * bound server action and the hidden `id` differ. A trade is just a canonical `name`
 * (unique, editable) and an optional short `code` — there is no immutable field, unlike
 * a unit of measure's code.
 */
export function TradeForm({ action, trade, submitLabel }: TradeFormProps) {
  return (
    <form action={action} className={styles.form}>
      {trade ? <input type="hidden" name="id" value={trade.id} /> : null}
      <input
        name="name"
        placeholder={t("trades.namePlaceholder")}
        defaultValue={trade?.name ?? ""}
        required
      />
      <input
        name="code"
        placeholder={t("trades.codePlaceholder")}
        defaultValue={trade?.code ?? ""}
      />
      <button type="submit">{submitLabel}</button>
    </form>
  );
}
