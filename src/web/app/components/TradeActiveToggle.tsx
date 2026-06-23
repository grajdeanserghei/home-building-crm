import { type Trade } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface TradeActiveToggleProps {
  // The setTradeActive server action, passed from the server component.
  action: (formData: FormData) => void | Promise<void>;
  trade: Trade;
}

/**
 * Retire/restore control for a trade. Deactivation is reversible (the trade is hidden
 * from new assignments, not deleted) so — unlike the destructive delete buttons
 * elsewhere — it submits directly without a confirmation modal. The hidden `isActive`
 * carries the target state: the opposite of the current one.
 */
export function TradeActiveToggle({ action, trade }: TradeActiveToggleProps) {
  return (
    <form action={action}>
      <input type="hidden" name="id" value={trade.id} />
      <input type="hidden" name="isActive" value={String(!trade.isActive)} />
      <button
        type="submit"
        className={trade.isActive ? styles.delete : styles.edit}
      >
        {trade.isActive ? t("trades.deactivate") : t("trades.activate")}
      </button>
    </form>
  );
}
