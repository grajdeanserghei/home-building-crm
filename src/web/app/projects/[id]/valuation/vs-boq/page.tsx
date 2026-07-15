import Link from "next/link";
import { getProject, getValuationComparison } from "@/app/lib/api";
import { getDisplayCurrency } from "@/app/lib/display-currency";
import { displayMoney, formatNumber, formatPercent } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function ValuationVsBoqPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [project, comparison, displayCurrency] = await Promise.all([
    getProject(id),
    getValuationComparison(id),
    getDisplayCurrency(),
  ]);

  const projectName = project?.name ?? "";

  // No catalog for this project yet — invite creating one, mirroring the hub's empty state.
  if (!comparison) {
    return (
      <main className={styles.main}>
        <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
          {t("valuation.backToProject")}
        </Link>
        <h1>{t("valuation.subnav.vsBoq")}</h1>
        <section className={styles.card}>
          <p className={styles.muted}>{t("valuation.empty.body")}</p>
          <div className={styles.actions}>
            <Link
              href={`/projects/${id}/valuation`}
              className={styles.primaryButton}
            >
              {t("valuation.link")} →
            </Link>
          </div>
        </section>
      </main>
    );
  }

  // The comparison is single-currency (the catalog currency). Convert only for the header toggle,
  // using the read model's own pinned rate.
  const money = (m: Parameters<typeof displayMoney>[0]) =>
    displayMoney(m, displayCurrency, comparison.ronPerEur);
  const { totals } = comparison;

  return (
    <main className={styles.main}>
      <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
        {t("valuation.backToProject")}
      </Link>
      <h1>{t("valuation.vsBoq.title", { name: projectName })}</h1>
      <p className={styles.subtitle}>{t("valuation.vsBoq.subtitle")}</p>

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
    </main>
  );
}
