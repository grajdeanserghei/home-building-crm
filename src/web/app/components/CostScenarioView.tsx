import Link from "next/link";
import type {
  CostScenario,
  Money,
  ScenarioCandidateWorkPackage,
} from "@/app/lib/api";
import { ScenarioSelectionSelect } from "@/app/components/ScenarioSelectionSelect";
import {
  convertMoney,
  displayMoney,
  formatMoney,
  formatMoneyWhole,
  formatNumber,
  type DisplayCurrency,
} from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Each apartment's share of a whole-build figure: the amount divided by the dwelling-unit count.
// Every scenario figure is already gross-or-net whole-build, so this exact division is all "per
// apartment" needs. units === 1 returns the figure unchanged.
function perApartment(m: Money, units: number): Money {
  return units === 1 ? m : { amount: m.amount / units, currency: m.currency };
}

interface CostScenarioViewProps {
  scenario: CostScenario;
  candidates: ScenarioCandidateWorkPackage[];
  projectId: string;
  // The global display currency (the header toggle), read from the cookie by the server page.
  displayCurrency: DisplayCurrency;
}

/**
 * Body of a cost scenario (the "simulator"): the choose-offers table, the breakdown, and the totals.
 * Every price renders in the global display currency (the header toggle). In "Original" mode each
 * figure shows in its own currency (with decimals) and the totals are listed per currency — RON and
 * EUR are never summed. In RON/EUR mode every figure is converted with the app-wide `ronPerEur` rate
 * carried on the scenario and summed into one total (whole numbers, no decimals). Conversion is
 * approximate (one display rate, not per-BoQ pinned rates) — the same basis as the former
 * "EUR equivalent".
 */
export function CostScenarioView({
  scenario,
  candidates,
  projectId,
  displayCurrency,
}: CostScenarioViewProps) {
  const { ronPerEur } = scenario;

  // The chosen bid per work package, for seeding the per-row selects.
  const selectedBidByWp = new Map(
    scenario.lines.map((line) => [line.workPackageId, line.bidId]),
  );

  // In RON/EUR mode, every per-currency total is converted into the display currency and summed.
  const converted = displayCurrency !== "Original";
  const target = displayCurrency as Money["currency"];
  const totalNet = scenario.totalsByCurrency.reduce(
    (sum, tot) =>
      sum +
      (converted ? (convertMoney(tot.net, target, ronPerEur)?.amount ?? 0) : 0),
    0,
  );
  const totalGross = scenario.totalsByCurrency.reduce(
    (sum, tot) =>
      sum +
      (converted ? (convertMoney(tot.gross, target, ronPerEur)?.amount ?? 0) : 0),
    0,
  );

  // A conversion is in effect (and the rate worth noting) whenever a native currency differs.
  const converting =
    converted &&
    scenario.totalsByCurrency.some((tot) => tot.currency !== displayCurrency);

  // Format a summed amount in the (converted) display currency — whole numbers, matching displayMoney.
  const summed = (amount: number): string =>
    formatMoneyWhole({ amount, currency: displayCurrency as Money["currency"] });

  return (
    <>
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
                <th>{t("costScenario.col.grossBuilding")}</th>
                <th>{t("costScenario.col.grossPerApartment")}</th>
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
                      <td>{displayMoney(line.gross, displayCurrency, ronPerEur)}</td>
                      <td>
                        {displayMoney(
                          perApartment(line.gross, scenario.apartmentUnits),
                          displayCurrency,
                          ronPerEur,
                        )}
                      </td>
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
              {converted ? (
                // RON/EUR: one unified row summing every currency into the display currency.
                <tr>
                  <td>
                    <strong>{t("costScenario.total")}</strong>
                  </td>
                  <td>{summed(totalNet)}</td>
                  <td>
                    <strong>{summed(totalGross)}</strong>
                  </td>
                </tr>
              ) : (
                // Original: a row per currency (RON and EUR are never summed), with decimals.
                scenario.totalsByCurrency.map((tot) => (
                  <tr key={tot.currency}>
                    <td>
                      <strong>
                        {t("costScenario.total")} · {tot.currency}
                      </strong>
                    </td>
                    <td>{formatMoney(tot.net)}</td>
                    <td>
                      <strong>{formatMoney(tot.gross)}</strong>
                    </td>
                  </tr>
                ))
              )}
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
