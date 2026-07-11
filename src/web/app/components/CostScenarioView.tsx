"use client";

import { useState } from "react";
import Link from "next/link";
import type {
  CostScenario,
  Currency,
  ScenarioCandidateWorkPackage,
} from "@/app/lib/api";
import { ScenarioSelectionSelect } from "@/app/components/ScenarioSelectionSelect";
import { convertMoney, formatMoney, formatMoneyIn, formatNumber } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

const CURRENCIES: Currency[] = ["RON", "EUR"];

interface CostScenarioViewProps {
  scenario: CostScenario;
  candidates: ScenarioCandidateWorkPackage[];
  projectId: string;
}

/**
 * Interactive body of a cost scenario (the "simulator"): the choose-offers table, the breakdown,
 * and the totals. A single RON/EUR toggle re-renders every price in the chosen currency, converting
 * client-side with the app-wide `ronPerEur` rate carried on the scenario. Conversion is approximate
 * (one display rate, not per-BoQ pinned rates) — the same basis as the former "EUR equivalent".
 */
export function CostScenarioView({
  scenario,
  candidates,
  projectId,
}: CostScenarioViewProps) {
  const [displayCurrency, setDisplayCurrency] = useState<Currency>("RON");
  const { ronPerEur } = scenario;

  // The chosen bid per work package, for seeding the per-row selects.
  const selectedBidByWp = new Map(
    scenario.lines.map((line) => [line.workPackageId, line.bidId]),
  );

  // Unified totals: every per-currency total converted into the display currency and summed.
  const totalNet = scenario.totalsByCurrency.reduce(
    (sum, tot) => sum + (convertMoney(tot.net, displayCurrency, ronPerEur)?.amount ?? 0),
    0,
  );
  const totalGross = scenario.totalsByCurrency.reduce(
    (sum, tot) => sum + (convertMoney(tot.gross, displayCurrency, ronPerEur)?.amount ?? 0),
    0,
  );

  // A conversion is in effect (and the rate worth noting) whenever a native currency differs.
  const converting = scenario.totalsByCurrency.some(
    (tot) => tot.currency !== displayCurrency,
  );

  return (
    <>
      <div className={styles.currencyToggle}>
        <span className={styles.label}>{t("costScenario.displayCurrency")}</span>
        <span className={styles.options} role="group" aria-label={t("costScenario.displayCurrency")}>
          {CURRENCIES.map((currency) => (
            <button
              key={currency}
              type="button"
              aria-pressed={displayCurrency === currency}
              className={displayCurrency === currency ? styles.active : undefined}
              onClick={() => setDisplayCurrency(currency)}
            >
              {currency}
            </button>
          ))}
        </span>
      </div>

      <section className={styles.card}>
        <h2>{t("costScenario.editorTitle")}</h2>
        <p className={styles.muted}>{t("costScenario.editorHint")}</p>
        {candidates.length === 0 ? (
          <p>{t("costScenario.noWorkPackages")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>#</th>
                <th>{t("common.name")}</th>
                <th>{t("costScenario.col.chosenBid")}</th>
              </tr>
            </thead>
            <tbody>
              {candidates.map((wp) => (
                <tr key={wp.workPackageId}>
                  <td>{wp.sequence}</td>
                  <td>
                    <Link
                      href={`/work-packages/${wp.workPackageId}`}
                      className={styles.nameLink}
                    >
                      <strong>{wp.name}</strong>
                    </Link>
                  </td>
                  <td>
                    <ScenarioSelectionSelect
                      scenarioId={scenario.id}
                      projectId={projectId}
                      workPackageId={wp.workPackageId}
                      candidates={wp.bids}
                      selectedBidId={selectedBidByWp.get(wp.workPackageId) ?? ""}
                      displayCurrency={displayCurrency}
                      ronPerEur={ronPerEur}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className={styles.card}>
        <h2>{t("costScenario.breakdownTitle")}</h2>
        {scenario.lines.length === 0 ? (
          <p>{t("costScenario.breakdownEmpty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>#</th>
                <th>{t("common.name")}</th>
                <th>{t("costScenario.col.contractor")}</th>
                <th>{t("costScenario.col.net")}</th>
                <th>{t("costScenario.col.gross")}</th>
              </tr>
            </thead>
            <tbody>
              {scenario.lines.map((line) => (
                <tr key={line.workPackageId}>
                  <td>{line.sequence}</td>
                  <td>
                    <Link
                      href={`/work-packages/${line.workPackageId}`}
                      className={styles.nameLink}
                    >
                      <strong>{line.workPackageName}</strong>
                    </Link>
                    {line.priced && line.multiplier > 1 ? (
                      <div className={styles.muted}>
                        {t("costScenario.perApartment", {
                          count: String(line.multiplier),
                        })}
                      </div>
                    ) : null}
                  </td>
                  <td>{line.contractorName}</td>
                  {line.priced ? (
                    <>
                      <td>{formatMoneyIn(line.net, displayCurrency, ronPerEur)}</td>
                      <td>{formatMoneyIn(line.gross, displayCurrency, ronPerEur)}</td>
                    </>
                  ) : (
                    <td colSpan={2} className={styles.muted}>
                      {t("costScenario.notPriced")}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {scenario.totalsByCurrency.length > 0 && (
        <section className={styles.card}>
          <h2>{t("costScenario.totalsTitle")}</h2>
          <table className={styles.table}>
            <thead>
              <tr>
                <th></th>
                <th>{t("costScenario.col.net")}</th>
                <th>{t("costScenario.col.gross")}</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>
                  <strong>{t("costScenario.total")}</strong>
                </td>
                <td>{formatMoney({ amount: totalNet, currency: displayCurrency })}</td>
                <td>
                  <strong>
                    {formatMoney({ amount: totalGross, currency: displayCurrency })}
                  </strong>
                </td>
              </tr>
            </tbody>
          </table>
          {converting && (
            <p className={styles.muted}>
              {t("costScenario.eurRate", { rate: formatNumber(ronPerEur) })}
            </p>
          )}
        </section>
      )}
    </>
  );
}
