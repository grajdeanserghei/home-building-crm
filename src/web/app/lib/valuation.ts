// Small shared derivations for the construction-valuation read models. The backend reports a
// snapshot's totals per currency (money never sums across currencies); the catalog is priced in a
// single currency (RON), so for a headline completion figure we aggregate the net amounts and keep
// the first row's currency. Kept out of api.ts (pure data) and the pages (pure view).
import type { Money, ValuationProgressTotals } from "./api";

export interface SnapshotCompletion {
  percentage: number; // 0..100 (completed ÷ estimated, net)
  completedNet: Money;
  remainingNet: Money;
  estimatedNet: Money;
}

// The overall completion of one snapshot from its per-currency totals.
export function snapshotCompletion(
  totals: ValuationProgressTotals,
): SnapshotCompletion {
  const rows = totals.byCurrency;
  const currency = rows[0]?.estimatedWithoutVat.currency ?? "RON";
  const sum = (pick: (r: (typeof rows)[number]) => Money): number =>
    rows.reduce((acc, r) => acc + pick(r).amount, 0);

  const estimated = sum((r) => r.estimatedWithoutVat);
  const completed = sum((r) => r.completedWithoutVat);
  const remaining = sum((r) => r.remainingWithoutVat);

  return {
    percentage: estimated > 0 ? (completed / estimated) * 100 : 0,
    completedNet: { amount: completed, currency },
    remainingNet: { amount: remaining, currency },
    estimatedNet: { amount: estimated, currency },
  };
}
