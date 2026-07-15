import Link from "next/link";
import { getProject, getValuationCatalog, getValuationProgress } from "@/app/lib/api";
import {
  formatDate,
  formatMoneyWhole,
  formatNumber,
  formatPercent,
} from "@/app/lib/format";
import { snapshotCompletion } from "@/app/lib/valuation";
import {
  ValuationProgressChart,
  type ValuationProgressPoint,
} from "@/app/components/ValuationProgressChart";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function ValuationProgressPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [project, catalog] = await Promise.all([
    getProject(id),
    getValuationCatalog(id),
  ]);
  // Progress is catalog-scoped; resolve the catalog id first (null ⇒ no catalog for the project).
  const progress = catalog ? await getValuationProgress(catalog.id) : null;
  const projectName = project?.name ?? "";

  if (!catalog || !progress) {
    return (
      <main className={styles.main}>
        <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
          {t("valuation.backToProject")}
        </Link>
        <h1>{t("valuation.subnav.progress")}</h1>
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

  // Derive one headline completion figure per snapshot from its per-currency totals.
  const rows = progress.snapshots.map((s) => ({
    snapshot: s,
    completion: snapshotCompletion(s.totals),
  }));

  const points: ValuationProgressPoint[] = rows.map(({ snapshot, completion }) => ({
    key: snapshot.id,
    label: formatDate(snapshot.assessedOn),
    percentage: completion.percentage,
    percentageLabel: formatPercent(completion.percentage),
    valueLabel: formatMoneyWhole(completion.completedNet),
  }));

  return (
    <main className={styles.main}>
      <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
        {t("valuation.backToProject")}
      </Link>
      <h1>{t("valuation.progress.title", { name: projectName })}</h1>
      <p className={styles.subtitle}>{t("valuation.progress.subtitle")}</p>

      {rows.length === 0 ? (
        <section className={styles.card}>
          <p className={styles.muted}>{t("valuation.progress.empty")}</p>
        </section>
      ) : (
        <>
          <section className={styles.card}>
            <h2>{t("valuation.progress.chartTitle")}</h2>
            <ValuationProgressChart points={points} />
            <p className={styles.muted}>{t("valuation.progress.rateNote")}</p>
          </section>

          <section className={styles.card}>
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>{t("valuation.progress.col.assessedOn")}</th>
                    <th>{t("valuation.progress.col.appraiser")}</th>
                    <th>{t("valuation.progress.col.completed")}</th>
                    <th>{t("valuation.progress.col.remaining")}</th>
                    <th>{t("valuation.progress.col.completion")}</th>
                    <th>{t("valuation.progress.col.rate")}</th>
                    <th>{t("valuation.progress.col.eur")}</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map(({ snapshot, completion }) => (
                    <tr key={snapshot.id}>
                      <td>
                        <Link
                          href={`/projects/${id}/valuation/snapshots/${snapshot.id}`}
                          className={styles.nameLink}
                        >
                          {formatDate(snapshot.assessedOn)}
                        </Link>
                      </td>
                      <td>{snapshot.appraiser || "—"}</td>
                      <td>{formatMoneyWhole(completion.completedNet)}</td>
                      <td>{formatMoneyWhole(completion.remainingNet)}</td>
                      <td>{formatPercent(completion.percentage)}</td>
                      <td>{formatNumber(snapshot.ronPerEur)}</td>
                      <td>
                        {snapshot.totals.eurEquivalent
                          ? formatMoneyWhole(
                              snapshot.totals.eurEquivalent.completedWithVat,
                            )
                          : "—"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </main>
  );
}
