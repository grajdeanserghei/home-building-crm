import Link from "next/link";
import { notFound } from "next/navigation";
import { getConstructionValuation } from "@/app/lib/api";
import { formatDate, formatMoneyWhole, formatNumber, formatPercent } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function ValuationSnapshotDetailPage({
  params,
}: {
  params: Promise<{ id: string; snapshotId: string }>;
}) {
  const { id, snapshotId } = await params;
  const snapshot = await getConstructionValuation(snapshotId);

  if (!snapshot) {
    notFound();
  }

  const source =
    snapshot.sourceDocument?.fileName ||
    snapshot.sourceDocument?.url ||
    "—";

  return (
    <main className={styles.main}>
      <Link
        href={`/projects/${id}/valuation/snapshots`}
        className={styles.backLink}
      >
        {t("valuation.snapshot.backToList")}
      </Link>
      <h1>
        {t("valuation.snapshot.detailTitle", {
          date: formatDate(snapshot.assessedOn),
        })}
      </h1>

      {/* Resolves a spec Open Question: a snapshot reflects the catalog as it was at capture. */}
      <p className={styles.muted}>
        {t("valuation.snapshot.banner", { date: formatDate(snapshot.assessedOn) })}
      </p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>{t("valuation.snapshot.header.assessedOn")}</dt>
          <dd>{formatDate(snapshot.assessedOn)}</dd>
          <dt>{t("valuation.snapshot.header.appraiser")}</dt>
          <dd>{snapshot.appraiser || "—"}</dd>
          <dt>{t("valuation.snapshot.header.rate")}</dt>
          <dd>
            1 {snapshot.exchangeRate.baseCurrency} ={" "}
            {formatNumber(snapshot.exchangeRate.rate)}{" "}
            {snapshot.exchangeRate.quoteCurrency}
          </dd>
          <dt>{t("valuation.snapshot.header.source")}</dt>
          <dd>{source}</dd>
        </dl>
      </section>

      <section className={styles.card}>
        <div className={styles.tableWrap}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("valuation.snapshot.item.name")}</th>
                <th>{t("valuation.snapshot.item.estimate")}</th>
                <th>{t("valuation.snapshot.item.completionPct")}</th>
                <th>{t("valuation.snapshot.item.completed")}</th>
                <th>{t("valuation.snapshot.item.remainingPct")}</th>
                <th>{t("valuation.snapshot.item.remaining")}</th>
              </tr>
            </thead>
            <tbody>
              {snapshot.items.map((item) => (
                <tr key={item.id}>
                  <td>
                    <strong>{item.name}</strong>
                  </td>
                  <td>{formatMoneyWhole(item.estimatedValueWithoutVat)}</td>
                  <td>{formatPercent(item.completionPercentage)}</td>
                  <td>{formatMoneyWhole(item.completedValueWithoutVat)}</td>
                  <td>{formatPercent(item.remainingPercentage)}</td>
                  <td>{formatMoneyWhole(item.remainingValueWithoutVat)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}
