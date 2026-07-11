import { cookies } from "next/headers";
import { apiBaseUrl } from "./api";
import type { DisplayCurrency } from "./format";

// Name of the cookie holding the global display-currency preference (the header toggle). Read on the
// server wherever money is rendered; written by the `setDisplayCurrency` server action. Absent means
// the default, "Original" (each amount in its own currency — no approximate conversion).
export const DISPLAY_CURRENCY_COOKIE = "displayCurrency";

// Fallback "1 EUR = N RON" rate if the backend's /api/exchange-rate is unreachable. Mirrors
// ManualExchangeRateProvider.DefaultRonPerEur so the two never silently disagree on the default.
export const DEFAULT_RON_PER_EUR = 5.24;

/**
 * The display currency chosen in the header, from the request's cookie. Defaults to "Original" when
 * the cookie is missing (or holds an unknown value), so a fresh visitor sees each BoQ in the currency
 * its contractor priced it in — no rate applied until they opt into RON/EUR.
 */
export async function getDisplayCurrency(): Promise<DisplayCurrency> {
  const cookieStore = await cookies();
  const raw = cookieStore.get(DISPLAY_CURRENCY_COOKIE)?.value;
  return raw === "RON" || raw === "EUR" ? raw : "Original";
}

/**
 * The single app-wide EUR↔RON display rate ("1 EUR = N RON"), used to convert figures on pages whose
 * own DTOs carry no rate (dashboard, work packages, contracts). Fetched from the backend so it tracks
 * the configured value; falls back to {@link DEFAULT_RON_PER_EUR} if the call fails. Approximate by
 * design — a BoQ's own pinned rate stays the source of truth for that quote, so BoQ figures keep
 * using `boq.ronPerEur` rather than this.
 */
export async function getDisplayRate(): Promise<number> {
  try {
    const res = await fetch(`${apiBaseUrl()}/api/exchange-rate`, {
      cache: "no-store",
    });
    if (!res.ok) return DEFAULT_RON_PER_EUR;
    const data = (await res.json()) as { ronPerEur?: number };
    return typeof data.ronPerEur === "number" && data.ronPerEur > 0
      ? data.ronPerEur
      : DEFAULT_RON_PER_EUR;
  } catch {
    return DEFAULT_RON_PER_EUR;
  }
}
