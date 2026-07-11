"use client";

import { DISPLAY_CURRENCIES, type DisplayCurrency } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "./Nav.module.css";

interface CurrencyToggleProps {
  // The active display currency, read from the cookie server-side by Nav.
  current: DisplayCurrency;
  // The setDisplayCurrency server action, passed down from the server Nav.
  action: (formData: FormData) => void | Promise<void>;
}

/**
 * Global EUR / RON / Original currency switch in the header. Each option is a submit button that
 * posts its own value to the `setDisplayCurrency` server action (progressive enhancement: the
 * clicked button's `name=value` is what submits), which persists the choice in a cookie and
 * revalidates the layout so every server-rendered price re-formats in place. Rendered as a client
 * component only so the buttons submit on click; the preference itself lives server-side, so there is
 * no hydration mismatch. Mirrors ProjectSwitcher's action-bound-form pattern.
 */
export function CurrencyToggle({ current, action }: CurrencyToggleProps) {
  return (
    <form
      action={action}
      className={styles.currencyToggle}
      role="group"
      aria-label={t("currency.toggleLabel")}
    >
      {DISPLAY_CURRENCIES.map((currency) => (
        <button
          key={currency}
          type="submit"
          name="displayCurrency"
          value={currency}
          aria-pressed={current === currency}
          className={current === currency ? styles.currencyActive : undefined}
        >
          {t(`currency.${currency}`)}
        </button>
      ))}
    </form>
  );
}
