import Link from "next/link";
import { notFound } from "next/navigation";
import {
  getProjectBudget,
  WORK_PACKAGE_STATUS_LABELS,
  type CandidateRange,
  type CurrencyTotals,
  type Money,
} from "@/app/lib/api";
import { getDisplayCurrency, getDisplayRate } from "@/app/lib/display-currency";
import {
  convertMoney,
  displayMoney,
  formatNumber,
  type DisplayCurrency,
} from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Render a price band in the given display currency: a single amount when low === high, otherwise
// "low – high". Formatting follows the global toggle (decimals only in Original mode).
function bandIn(
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

function bidCountLabel(count: number): string {
  return count === 1
    ? t("budget.bidCountOne")
    : t("budget.bidCountMany", { count: String(count) });
}

// The candidate-bids cell content for one currency: the price band (in the display currency) plus
// the bid count.
function CandidateLine({
  range,
  pref,
  rate,
}: {
  range: CandidateRange;
  pref: DisplayCurrency;
  rate: number;
}) {
  return (
    <div>
      {bandIn(range.low, range.high, pref, rate)}
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
  const [budget, displayCurrency, rate] = await Promise.all([
    getProjectBudget(id),
    getDisplayCurrency(),
    getDisplayRate(),
  ]);

  if (!budget) {
    notFound();
  }

  // In RON/EUR mode every currency is converted with the app-wide rate and the totals collapse to a
  // single row; in Original mode each currency is shown natively (RON and EUR are never summed) with
  // the backend's EUR-equivalent row alongside.
  const converted = displayCurrency !== "Original";

  // Sum one figure across every currency, converted into the display currency. Used only in RON/EUR
  // mode (where a single combined row makes sense).
  const totalIn = (pick: (t: CurrencyTotals) => Money): Money => ({
    amount: budget.totalsByCurrency.reduce(
      (sum, tot) =>
        sum +
        (convertMoney(pick(tot), displayCurrency as Money["currency"], rate)
          ?.amount ?? 0),
      0,
    ),
    currency: displayCurrency as Money["currency"],
  });

  // Show the EUR-equivalent total (Original mode only) when it adds information — i.e. more than one
  // currency, or the single currency in play is not already EUR.
  const eur = budget.eurEquivalent ?? null;
  const showEur =
    !converted &&
    eur !== null &&
    (budget.totalsByCurrency.length > 1 ||
      budget.totalsByCurrency[0]?.currency !== "EUR");

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
                <th>{t("budget.col.eur")}</th>
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
                      ? displayMoney(line.committed, displayCurrency, rate)
                      : "—"}
                  </td>
                  <td>
                    {line.kind === "Bids" ? (
                      line.candidates.map((range) => (
                        <CandidateLine
                          key={range.currency}
                          range={range}
                          pref={displayCurrency}
                          rate={rate}
                        />
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
                  <td>
                    {line.eurEquivalent
                      ? bandIn(
                          line.eurEquivalent.low,
                          line.eurEquivalent.high,
                          "Original",
                          rate,
                        )
                      : "—"}
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
              {converted ? (
                // RON/EUR: one unified row summing every currency into the display currency.
                <tr>
                  <td>{displayCurrency}</td>
                  <td>{displayMoney(totalIn((x) => x.committed), displayCurrency, rate)}</td>
                  <td>
                    {bandIn(
                      totalIn((x) => x.estimatedLow),
                      totalIn((x) => x.estimatedHigh),
                      displayCurrency,
                      rate,
                    )}
                  </td>
                  <td>
                    <strong>
                      {bandIn(
                        totalIn((x) => x.projectedLow),
                        totalIn((x) => x.projectedHigh),
                        displayCurrency,
                        rate,
                      )}
                    </strong>
                  </td>
                </tr>
              ) : (
                <>
                  {budget.totalsByCurrency.map((totals) => (
                    <tr key={totals.currency}>
                      <td>{totals.currency}</td>
                      <td>{displayMoney(totals.committed, displayCurrency, rate)}</td>
                      <td>
                        {bandIn(
                          totals.estimatedLow,
                          totals.estimatedHigh,
                          displayCurrency,
                          rate,
                        )}
                      </td>
                      <td>
                        <strong>
                          {bandIn(
                            totals.projectedLow,
                            totals.projectedHigh,
                            displayCurrency,
                            rate,
                          )}
                        </strong>
                      </td>
                    </tr>
                  ))}
                  {showEur && eur && (
                    <tr>
                      <td>
                        <strong>{t("budget.eurEquivalent")}</strong>
                      </td>
                      <td>{displayMoney(eur.totals.committed, "Original", rate)}</td>
                      <td>
                        {bandIn(
                          eur.totals.estimatedLow,
                          eur.totals.estimatedHigh,
                          "Original",
                          rate,
                        )}
                      </td>
                      <td>
                        <strong>
                          {bandIn(
                            eur.totals.projectedLow,
                            eur.totals.projectedHigh,
                            "Original",
                            rate,
                          )}
                        </strong>
                      </td>
                    </tr>
                  )}
                </>
              )}
            </tbody>
          </table>
          {(converted || (showEur && eur)) && (
            <p className={styles.muted}>
              {t("budget.eurRate", {
                rate: formatNumber(converted ? rate : (eur?.ronPerEur ?? rate)),
              })}
            </p>
          )}
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
