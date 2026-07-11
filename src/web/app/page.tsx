import Link from "next/link";
import { resolveCurrentProject } from "./lib/current-project";
import {
  formatMoney,
  getProjectActivity,
  getProjectBudget,
  getWorkPackages,
  PROJECT_STATUS_LABELS,
  type ActivityItem,
  type CurrencyTotals,
  type Money,
  type Project,
  type ProjectBudget,
  type WorkPackage,
} from "./lib/api";
import { ActivityFeed } from "./components/ActivityFeed";
import { WorkPackagesTable } from "./components/WorkPackagesTable";
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

  // Load the selected project's work packages, budget and recent activity for the dashboard.
  let workPackages: WorkPackage[] = [];
  let budget: ProjectBudget | null = null;
  let activity: ActivityItem[] = [];
  let dataError: string | null = null;
  try {
    [workPackages, budget, activity] = await Promise.all([
      getWorkPackages(current.id),
      getProjectBudget(current.id),
      getProjectActivity(current.id),
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

      <p className={styles.muted}>
        {t("projects.apartmentUnitsSummary", {
          count: String(current.apartmentUnits),
        })}
      </p>

      <div className={styles.linkRow}>
        <Link href={`/projects/${current.id}/bids`} className={styles.edit}>
          {t("projectBids.link")} →
        </Link>
        <Link
          href={`/projects/${current.id}/cost-scenarios`}
          className={styles.edit}
        >
          {t("costScenario.link")} →
        </Link>
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
        <h2>{t("feed.title")}</h2>
        {dataError ? (
          <p className={styles.error}>{t("common.apiError", { error: dataError })}</p>
        ) : (
          <ActivityFeed items={activity} />
        )}
      </section>

      <section className={styles.card}>
        <h2>{t("dashboard.workPackagesTitle")}</h2>
        <WorkPackagesTable
          workPackages={workPackages}
          projectId={current.id}
          error={dataError}
        />
      </section>

      <p>
        <Link
          href={`/projects/${current.id}/work-packages/new`}
          className={styles.primaryButton}
        >
          {t("workPackages.add")}
        </Link>
      </p>
    </main>
  );
}
