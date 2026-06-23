import Link from "next/link";
import { notFound } from "next/navigation";
import {
  getProjectBudget,
  WORK_PACKAGE_STATUS_LABELS,
  formatMoney,
  type CandidateRange,
  type Money,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Render a net price band: a single amount when low === high, otherwise "low – high".
function band(low: Money, high: Money): string {
  return low.amount === high.amount
    ? formatMoney(low)
    : `${formatMoney(low)} – ${formatMoney(high)}`;
}

function bidCountLabel(count: number): string {
  return count === 1
    ? t("budget.bidCountOne")
    : t("budget.bidCountMany", { count: String(count) });
}

// The candidate-bids cell content for one currency: the price band plus the bid count.
function CandidateLine({ range }: { range: CandidateRange }) {
  return (
    <div>
      {band(range.low, range.high)}
      <span className={styles.muted}> · {bidCountLabel(range.bidCount)}</span>
    </div>
  );
}

export default async function ProjectBudgetPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const budget = await getProjectBudget(id);

  if (!budget) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <Link href={`/projects/${id}`} className={styles.backLink}>
        {t("budget.backToProject")}
      </Link>
      <h1>{t("budget.title", { name: budget.projectName })}</h1>
      <p className={styles.subtitle}>{t("budget.subtitle")}</p>

      <section className={styles.card}>
        <h2>{t("budget.linesTitle")}</h2>
        {budget.lines.length === 0 ? (
          <p>{t("budget.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>#</th>
                <th>{t("common.name")}</th>
                <th>{t("common.status")}</th>
                <th>{t("budget.col.committed")}</th>
                <th>{t("budget.col.candidates")}</th>
              </tr>
            </thead>
            <tbody>
              {budget.lines.map((line) => (
                <tr key={line.workPackageId}>
                  <td>{line.sequence}</td>
                  <td>
                    <Link
                      href={`/work-packages/${line.workPackageId}`}
                      className={styles.nameLink}
                    >
                      <strong>{line.name}</strong>
                    </Link>
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${styles[`status${line.status}`]}`}
                    >
                      {WORK_PACKAGE_STATUS_LABELS[line.status]}
                    </span>
                  </td>
                  <td>
                    {line.kind === "Contract" && line.committed
                      ? formatMoney(line.committed)
                      : "—"}
                  </td>
                  <td>
                    {line.kind === "Bids" ? (
                      line.candidates.map((range) => (
                        <CandidateLine key={range.currency} range={range} />
                      ))
                    ) : line.kind === "Pending" ? (
                      <span className={styles.muted}>
                        {t("budget.kind.pending")}
                      </span>
                    ) : line.kind === "None" ? (
                      <span className={styles.muted}>
                        {t("budget.kind.none")}
                      </span>
                    ) : (
                      "—"
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {budget.totalsByCurrency.length > 0 && (
        <section className={styles.card}>
          <h2>{t("budget.totalsTitle")}</h2>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("budget.totals.currency")}</th>
                <th>{t("budget.totals.committed")}</th>
                <th>{t("budget.totals.estimated")}</th>
                <th>{t("budget.totals.projected")}</th>
              </tr>
            </thead>
            <tbody>
              {budget.totalsByCurrency.map((totals) => (
                <tr key={totals.currency}>
                  <td>{totals.currency}</td>
                  <td>{formatMoney(totals.committed)}</td>
                  <td>{band(totals.estimatedLow, totals.estimatedHigh)}</td>
                  <td>
                    <strong>
                      {band(totals.projectedLow, totals.projectedHigh)}
                    </strong>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {budget.unpricedWorkPackageCount > 0 && (
            <p className={styles.muted}>
              {budget.unpricedWorkPackageCount === 1
                ? t("budget.unpricedNoteOne")
                : t("budget.unpricedNote", {
                    count: String(budget.unpricedWorkPackageCount),
                  })}
            </p>
          )}
        </section>
      )}
    </main>
  );
}
