"use client";

import { useRouter } from "next/navigation";
import type { Currency } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

const CURRENCIES: Currency[] = ["RON", "EUR"];

/**
 * Page-level RON/EUR display toggle for the BoQ. The chosen currency lives in the URL (`?currency=`)
 * so the whole server-rendered page — header totals, detail list, section subtotals and line items —
 * re-renders consistently in one currency. Each button navigates to the same page with the currency
 * param set (other params like `arrange` are preserved by the server-built `hrefs`); `scroll: false`
 * keeps the viewport in place so toggling feels in-place. Conversion is approximate (one display rate).
 */
export function BoqCurrencyToggle({
  current,
  hrefs,
}: {
  current: Currency;
  hrefs: Record<Currency, string>;
}) {
  const router = useRouter();

  return (
    <div className={styles.currencyToggle}>
      <span className={styles.label}>{t("boq.displayCurrency")}</span>
      <span
        className={styles.options}
        role="group"
        aria-label={t("boq.displayCurrency")}
      >
        {CURRENCIES.map((currency) => (
          <button
            key={currency}
            type="button"
            aria-pressed={current === currency}
            className={current === currency ? styles.active : undefined}
            onClick={() => router.replace(hrefs[currency], { scroll: false })}
          >
            {currency}
          </button>
        ))}
      </span>
    </div>
  );
}
