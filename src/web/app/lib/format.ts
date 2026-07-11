// Shared ro-RO formatting helpers. Every human-facing date, number, currency and
// percentage goes through here, so formatting is consistent and locale-correct in one
// place — replacing the ~11 duplicated `formatDate` copies and the locale-less
// `formatMoney`. The backend keeps persisted/wire values invariant (dot decimal); ro-RO
// is applied only at render time. See docs/specifications/romanian-localization.md.
import type { Currency, Money } from "./api";

const LOCALE = "ro-RO";

// The placeholder shown for a null/absent value (em dash).
export const EMPTY = "—";

// A bare calendar date (DateOnly, e.g. a due date sent as "2026-06-22"). Formatted in UTC
// so a date-only value never shifts across the day boundary by the viewer's timezone.
const dateFormatter = new Intl.DateTimeFormat(LOCALE, { timeZone: "UTC" });

// A full timestamp (DateTimeOffset) → date + time in the viewer's local timezone.
const dateTimeFormatter = new Intl.DateTimeFormat(LOCALE, {
  dateStyle: "short",
  timeStyle: "short",
});

const numberFormatter = new Intl.NumberFormat(LOCALE, {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const percentFormatter = new Intl.NumberFormat(LOCALE, {
  style: "percent",
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
});

/** Format a bare calendar date in ro-RO, e.g. "22.06.2026". Null/empty → em dash. */
export function formatDate(value?: string | null): string {
  if (!value) return EMPTY;
  return dateFormatter.format(new Date(value));
}

/** Format a full timestamp in ro-RO, e.g. "22.06.2026, 14:30". Null/empty → em dash. */
export function formatDateTime(value?: string | null): string {
  if (!value) return EMPTY;
  return dateTimeFormatter.format(new Date(value));
}

/** Format a plain number with ro-RO grouping/decimals, e.g. "12.500,50". Null → em dash. */
export function formatNumber(value?: number | null): string {
  if (value === null || value === undefined) return EMPTY;
  return numberFormatter.format(value);
}

function formatMoneyWith(money: Money, fractionDigits: number): string {
  return new Intl.NumberFormat(LOCALE, {
    style: "currency",
    currency: money.currency,
    currencyDisplay: "code",
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(money.amount);
}

/**
 * Format a Money amount in ro-RO with the ISO currency code, e.g. "12.500,50 RON".
 * `currencyDisplay: "code"` keeps RON/EUR consistent (no lei/€ split) and matches the spec
 * example. Null → em dash.
 */
export function formatMoney(money?: Money | null): string {
  if (!money) return EMPTY;
  return formatMoneyWith(money, 2);
}

/**
 * Like {@link formatMoney} but rounded to whole units (no decimals), e.g. "12.501 RON". Used by the
 * cost simulator, where cents are noise against large, already-approximate build totals. Null → em dash.
 */
export function formatMoneyWhole(money?: Money | null): string {
  if (!money) return EMPTY;
  return formatMoneyWith(money, 0);
}

/**
 * Convert a Money amount to `target` using the app-wide display rate (`ronPerEur`, "1 EUR = N RON").
 * Same-currency is a no-op; RON→EUR divides by the rate, EUR→RON multiplies. A non-positive rate
 * falls back to the original amount (can't convert safely). Null in → null out. Approximate by design
 * — mirrors the backend's "EUR equivalent". Used by the simulator's RON/EUR display toggle.
 */
export function convertMoney(
  money: Money | null | undefined,
  target: Currency,
  ronPerEur: number,
): Money | null {
  if (!money) return null;
  if (money.currency === target || ronPerEur <= 0) {
    return money.currency === target ? money : { ...money };
  }
  if (money.currency === "RON" && target === "EUR") {
    return { amount: money.amount / ronPerEur, currency: "EUR" };
  }
  if (money.currency === "EUR" && target === "RON") {
    return { amount: money.amount * ronPerEur, currency: "RON" };
  }
  return money;
}

/**
 * Convert a Money to `target` (via {@link convertMoney}) and format it whole (no decimals) for the
 * cost simulator's RON/EUR toggle. Null → em dash.
 */
export function formatMoneyIn(
  money: Money | null | undefined,
  target: Currency,
  ronPerEur: number,
): string {
  return formatMoneyWhole(convertMoney(money, target, ronPerEur));
}

/** Format a whole-number VAT rate as a percentage (21 → "21%"). Null → em dash. */
export function formatPercent(percentage?: number | null): string {
  if (percentage === null || percentage === undefined) return EMPTY;
  return percentFormatter.format(percentage / 100);
}
