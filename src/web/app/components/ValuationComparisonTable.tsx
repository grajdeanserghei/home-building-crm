import type { ValuationVsBoq } from "@/app/lib/api";
import { displayMoney, formatNumber, formatPercent, type DisplayCurrency } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface ValuationComparisonTableProps {
  comparison: ValuationVsBoq;
  displayCurrency: DisplayCurrency;
}

/**
 * The estimate-vs-real read model rendered as three cards: the per-item table, the per-currency totals
 * footer, and the coverage gaps. Shared by the standalone `/valuation/vs-boq` page (Decided basis) and the
 * cost-scenario detail page (Scenario basis) — the two differ only in which `comparison` they pass in, so
 * the presentation lives here once. Each page supplies its own heading above this fragment.
 *
 * The comparison is single-currency (the catalog currency); money is converted only for the header toggle,
 * using the read model's own pinned rate (`comparison.ronPerEur`).
 */
export function ValuationComparisonTable({
  comparison,
  displayCurrency,
}: ValuationComparisonTableProps) {
  const money = (m: Parameters<typeof displayMoney>[0]) =>
    displayMoney(m, displayCurrency, comparison.ronPerEur);
  const { totals } = comparison;

  return (
    <>
      <section className={styles.card}>
        {comparison.items.length === 0 ? (
          <p className={styles.muted}>{t("valuation.vsBoq.empty")}</p>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>{t("valuation.vsBoq.col.printedNumber")}</th>
                  <th>{t("valuation.vsBoq.col.name")}</th>
                  <th>{t("valuation.vsBoq.col.estimate")}</th>
                  <th>{t("valuation.vsBoq.col.actual")}</th>
                  <th>{t("valuation.vsBoq.col.variance")}</th>
                  <th>{t("valuation.vsBoq.col.variancePct")}</th>
                  <th>{t("valuation.vsBoq.col.coverage")}</th>
                </tr>
              </thead>
              <tbody>
                {comparison.items.map((line) => (
                  <tr key={line.valuationCatalogItemId}>
                    <td>{line.sequence}</td>
                    <td>
                      <strong>{line.name}</strong>
                    </td>
                    <td>{money(line.estimate)}</td>
                    <td>{line.isMapped && line.actual ? money(line.actual) : "—"}</td>
                    <td>
                      {line.isMapped && line.variance ? money(line.variance) : "—"}
                    </td>
                    <td>
                      {line.isMapped &&
                      line.variancePercentage !== null &&
                      line.variancePercentage !== undefined
                        ? formatPercent(line.variancePercentage)
                        : "—"}
                    </td>
                    <td className={styles.muted}>
                      {line.isMapped
                        ? t("valuation.vsBoq.covered")
                        : t("valuation.vsBoq.unmapped")}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className={styles.card}>
        <h2>{t("valuation.vsBoq.totalsTitle")}</h2>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>{t("valuation.vsBoq.totals.currency")}</th>
              <th>{t("valuation.vsBoq.totals.estimate")}</th>
              <th>{t("valuation.vsBoq.totals.actual")}</th>
              <th>{t("valuation.vsBoq.col.variance")}</th>
              <th>{t("valuation.vsBoq.col.variancePct")}</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>{comparison.currency}</td>
              <td>{money(totals.totalEstimate)}</td>
              <td>{money(totals.totalActual)}</td>
              <td>
                <strong>{money(totals.totalVariance)}</strong>
              </td>
              <td>
                {totals.totalVariancePercentage !== null &&
                totals.totalVariancePercentage !== undefined
                  ? formatPercent(totals.totalVariancePercentage)
                  : "—"}
              </td>
            </tr>
          </tbody>
        </table>
        <p className={styles.muted}>
          {t("valuation.vsBoq.mappedNote", {
            percent: formatNumber(totals.coveragePercentage),
          })}
        </p>
        {displayCurrency !== "Original" ? (
          <p className={styles.muted}>
            {t("valuation.vsBoq.eurRate", {
              rate: formatNumber(comparison.ronPerEur),
            })}
          </p>
        ) : null}
      </section>

      {comparison.coverageGaps.length > 0 ? (
        <section className={styles.card}>
          <h2>{t("valuation.vsBoq.gapsTitle")}</h2>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("valuation.vsBoq.gap.description")}</th>
                <th>{t("valuation.vsBoq.gap.amount")}</th>
              </tr>
            </thead>
            <tbody>
              {comparison.coverageGaps.map((gap, idx) => (
                <tr key={`${gap.kind}-${gap.sectionId ?? gap.valuationCatalogItemId ?? idx}`}>
                  <td>{gap.description}</td>
                  <td>{money(gap.amount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      ) : null}
    </>
  );
}
