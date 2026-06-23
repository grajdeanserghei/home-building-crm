import Link from "next/link";
import { resolveCurrentProject } from "./lib/current-project";
import {
  formatMoney,
  getProjectBudget,
  getWorkPackages,
  PROJECT_STATUS_LABELS,
  WORK_PACKAGE_STATUS_LABELS,
  type CurrencyTotals,
  type Money,
  type Project,
  type ProjectBudget,
  type WorkPackage,
} from "./lib/api";
import { formatDate } from "./lib/format";
import { t } from "./lib/i18n";
import styles from "./page.module.css";

// Render a projected band: a single amount when low === high, otherwise "low – high".
function band(low: Money, high: Money): string {
  return low.amount === high.amount
    ? formatMoney(low)
    : `${formatMoney(low)} – ${formatMoney(high)}`;
}

// The single projected-cost figure to headline on the dashboard: the EUR-equivalent
// projection when available (comparable across currencies), otherwise the lone
// currency's projection. Null when there is nothing priced yet, or several
// currencies with no EUR rate to combine them.
function projectedHeadline(budget: ProjectBudget | null): string | null {
  if (!budget) return null;
  const eur = budget.eurEquivalent?.totals;
  if (eur) return band(eur.projectedLow, eur.projectedHigh);
  const single: CurrencyTotals | undefined =
    budget.totalsByCurrency.length === 1
      ? budget.totalsByCurrency[0]
      : undefined;
  return single ? band(single.projectedLow, single.projectedHigh) : null;
}

export default async function Home() {
  let current: Project | null = null;
  let error: string | null = null;

  try {
    const resolved = await resolveCurrentProject();
    current = resolved.current;
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  if (error) {
    return (
      <main className={styles.main}>
        <h1>{t("meta.title")}</h1>
        <p className={styles.error}>{t("common.apiError", { error })}</p>
      </main>
    );
  }

  // No projects exist yet: prompt the user to create the first one.
  if (!current) {
    return (
      <main className={styles.main}>
        <div className={styles.toolbar}>
          <div>
            <h1>{t("meta.title")}</h1>
            <p className={styles.subtitle}>{t("dashboard.subtitle")}</p>
          </div>
          <Link href="/projects/new" className={styles.primaryButton}>
            {t("projects.add")}
          </Link>
        </div>
        <section className={styles.card}>
          <p>{t("dashboard.empty")}</p>
        </section>
      </main>
    );
  }

  // Load the selected project's work packages and budget for the dashboard.
  let workPackages: WorkPackage[] = [];
  let budget: ProjectBudget | null = null;
  let dataError: string | null = null;
  try {
    [workPackages, budget] = await Promise.all([
      getWorkPackages(current.id),
      getProjectBudget(current.id),
    ]);
  } catch (e) {
    dataError = e instanceof Error ? e.message : t("common.unknownError");
  }

  const openForBids = workPackages.filter(
    (wp) => wp.status === "OpenForBids",
  ).length;
  const awarded = workPackages.filter((wp) => wp.status === "Awarded").length;
  const projected = projectedHeadline(budget);

  return (
    <main className={styles.main}>
      <div className={styles.toolbar}>
        <div>
          <h1>{current.name}</h1>
          <p className={styles.subtitle}>
            <span className={`${styles.badge} ${styles[`status${current.status}`]}`}>
              {PROJECT_STATUS_LABELS[current.status]}
            </span>
            {current.description ? ` · ${current.description}` : ""}
          </p>
        </div>
        <div className={styles.actions}>
          <Link href={`/projects/${current.id}/edit`} className={styles.edit}>
            {t("projects.edit")}
          </Link>
          <Link
            href={`/projects/${current.id}/budget`}
            className={styles.primaryButton}
          >
            {t("budget.link")}
          </Link>
        </div>
      </div>

      <section className={styles.stats}>
        <div className={styles.stat}>
          <span className={styles.statValue}>{workPackages.length}</span>
          <span className={styles.statLabel}>
            {t("dashboard.statWorkPackages")}
          </span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statValue}>{openForBids}</span>
          <span className={styles.statLabel}>
            {t("dashboard.statOpenForBids")}
          </span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statValue}>{awarded}</span>
          <span className={styles.statLabel}>{t("dashboard.statAwarded")}</span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statValue}>{projected ?? "—"}</span>
          <span className={styles.statLabel}>
            {t("dashboard.statProjected")}
          </span>
        </div>
      </section>

      <section className={styles.card}>
        <div className={styles.cardHeader}>
          <h2>{t("dashboard.workPackagesTitle")}</h2>
          <Link href={`/projects/${current.id}`} className={styles.edit}>
            {t("dashboard.workPackagesManage")}
          </Link>
        </div>
        {dataError ? (
          <p className={styles.error}>{t("common.apiError", { error: dataError })}</p>
        ) : workPackages.length === 0 ? (
          <p>{t("workPackages.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>#</th>
                <th>{t("common.name")}</th>
                <th>{t("common.status")}</th>
                <th>{t("workPackages.col.plannedStart")}</th>
                <th>{t("workPackages.col.plannedEnd")}</th>
              </tr>
            </thead>
            <tbody>
              {workPackages.map((wp) => (
                <tr key={wp.id}>
                  <td>{wp.sequence}</td>
                  <td>
                    <Link
                      href={`/work-packages/${wp.id}`}
                      className={styles.nameLink}
                    >
                      <strong>{wp.name}</strong>
                    </Link>
                    {wp.description ? (
                      <div className={styles.muted}>{wp.description}</div>
                    ) : null}
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${styles[`status${wp.status}`]}`}
                    >
                      {WORK_PACKAGE_STATUS_LABELS[wp.status]}
                    </span>
                  </td>
                  <td>{formatDate(wp.plannedStartDate)}</td>
                  <td>{formatDate(wp.plannedEndDate)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  );
}
