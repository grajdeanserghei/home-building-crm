import Link from "next/link";
import { resolveCurrentProject } from "./lib/current-project";
import {
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
import { getDisplayCurrency, getDisplayRate } from "./lib/display-currency";
import {
  convertMoney,
  displayMoney,
  formatMoneyWhole,
  type DisplayCurrency,
} from "./lib/format";
import { ActivityFeed } from "./components/ActivityFeed";
import { WorkPackagesTable } from "./components/WorkPackagesTable";
import { t } from "./lib/i18n";
import styles from "./page.module.css";

// Render a projected band in the display currency: a single amount when low === high, otherwise
// "low – high". Formatting follows the global toggle (decimals only in Original mode).
function band(
  low: Money,
  high: Money,
  pref: DisplayCurrency,
  rate: number,
): string {
  const lo = displayMoney(low, pref, rate);
  return low.amount === high.amount
    ? lo
    : `${lo} – ${displayMoney(high, pref, rate)}`;
}

// The single projected-cost figure to headline on the dashboard, in the chosen display currency.
// In RON/EUR mode every currency's projection is converted with the app-wide rate and summed into
// one band. In Original mode there is no single "original" for a multi-currency blend, so it keeps
// the prior behaviour: the backend's EUR-equivalent projection when available, else the lone
// currency's projection. Null when there is nothing priced yet.
// `divisor` scales the figure to one apartment's share (whole-build total ÷ apartment count);
// divisor === 1 (the default) is the whole-building headline. Division is exact and commutes with
// the currency conversion and cross-currency summation done here.
function projectedHeadline(
  budget: ProjectBudget | null,
  pref: DisplayCurrency,
  rate: number,
  divisor = 1,
): string | null {
  if (!budget) return null;

  const perApt = (m: Money): Money =>
    divisor === 1 ? m : { amount: m.amount / divisor, currency: m.currency };

  if (pref !== "Original") {
    if (budget.totalsByCurrency.length === 0) return null;
    let low = 0;
    let high = 0;
    for (const tot of budget.totalsByCurrency) {
      const target = pref as Money["currency"];
      low += convertMoney(tot.projectedLow, target, rate)?.amount ?? 0;
      high += convertMoney(tot.projectedHigh, target, rate)?.amount ?? 0;
    }
    low /= divisor;
    high /= divisor;
    const lo = formatMoneyWhole({ amount: low, currency: pref });
    return low === high
      ? lo
      : `${lo} – ${formatMoneyWhole({ amount: high, currency: pref })}`;
  }

  const eur = budget.eurEquivalent?.totals;
  if (eur) return band(perApt(eur.projectedLow), perApt(eur.projectedHigh), pref, rate);
  const single: CurrencyTotals | undefined =
    budget.totalsByCurrency.length === 1
      ? budget.totalsByCurrency[0]
      : undefined;
  return single
    ? band(perApt(single.projectedLow), perApt(single.projectedHigh), pref, rate)
    : null;
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

  // The projected-cost headline honours the global display-currency toggle.
  const [displayCurrency, rate] = await Promise.all([
    getDisplayCurrency(),
    getDisplayRate(),
  ]);
  const projected = projectedHeadline(budget, displayCurrency, rate);
  // Each apartment's share of the projected total, shown as a sub-line only for a multi-unit build.
  const projectedPerApartment =
    current.apartmentUnits > 1
      ? projectedHeadline(budget, displayCurrency, rate, current.apartmentUnits)
      : null;

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
          {projectedPerApartment && (
            <span className={styles.muted}>
              {t("dashboard.statProjectedPerApartment", {
                value: projectedPerApartment,
              })}
            </span>
          )}
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
