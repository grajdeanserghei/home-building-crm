import Link from "next/link";
import { notFound } from "next/navigation";
import {
  getCostScenario,
  getScenarioCandidates,
  type ScenarioCandidateWorkPackage,
} from "@/app/lib/api";
import { deleteCostScenario } from "@/app/cost-scenarios/actions";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { ScenarioSelectionSelect } from "@/app/components/ScenarioSelectionSelect";
import { formatMoney, formatNumber } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function CostScenarioPage({
  params,
}: {
  params: Promise<{ id: string; scenarioId: string }>;
}) {
  const { id, scenarioId } = await params;
  const [scenario, candidatesData] = await Promise.all([
    getCostScenario(scenarioId),
    getScenarioCandidates(id),
  ]);

  if (!scenario) {
    notFound();
  }

  const candidates: ScenarioCandidateWorkPackage[] = candidatesData ?? [];

  // The chosen bid per work package, for seeding the per-row selects.
  const selectedBidByWp = new Map(
    scenario.lines.map((line) => [line.workPackageId, line.bidId]),
  );

  // Show the EUR-equivalent total only when it adds information — i.e. there is more than one
  // currency, or the single currency in play is not already EUR.
  const eur = scenario.eurEquivalent ?? null;
  const showEur =
    eur !== null &&
    (scenario.totalsByCurrency.length > 1 ||
      scenario.totalsByCurrency[0]?.currency !== "EUR");

  return (
    <main className={styles.main}>
      <Link href={`/projects/${id}/cost-scenarios`} className={styles.backLink}>
        {t("costScenario.backToList")}
      </Link>

      <h1>{scenario.name}</h1>
      {scenario.description ? (
        <p className={styles.subtitle}>{scenario.description}</p>
      ) : null}

      <div className={styles.actions}>
        <Link
          href={`/projects/${id}/cost-scenarios/${scenarioId}/edit`}
          className={styles.edit}
        >
          {t("common.edit")}
        </Link>
        <ConfirmDeleteButton
          action={deleteCostScenario}
          fields={{ id: scenario.id, projectId: id }}
          title={t("costScenario.deleteTitle")}
          bodyTemplate={t("costScenario.deleteBody")}
          name={scenario.name}
        />
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
                      projectId={id}
                      workPackageId={wp.workPackageId}
                      candidates={wp.bids}
                      selectedBidId={selectedBidByWp.get(wp.workPackageId) ?? ""}
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
                <th>{t("costScenario.col.eur")}</th>
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
                      <td>{formatMoney(line.net)}</td>
                      <td>{formatMoney(line.gross)}</td>
                      <td>{formatMoney(line.eurEquivalentGross)}</td>
                    </>
                  ) : (
                    <td colSpan={3} className={styles.muted}>
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
                <th>{t("costScenario.totals.currency")}</th>
                <th>{t("costScenario.col.net")}</th>
                <th>{t("costScenario.col.gross")}</th>
              </tr>
            </thead>
            <tbody>
              {scenario.totalsByCurrency.map((totals) => (
                <tr key={totals.currency}>
                  <td>{totals.currency}</td>
                  <td>{formatMoney(totals.net)}</td>
                  <td>
                    <strong>{formatMoney(totals.gross)}</strong>
                  </td>
                </tr>
              ))}
              {showEur && eur && (
                <tr>
                  <td>
                    <strong>{t("costScenario.eurEquivalent")}</strong>
                  </td>
                  <td>{formatMoney(eur.net)}</td>
                  <td>
                    <strong>{formatMoney(eur.gross)}</strong>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          {showEur && eur && (
            <p className={styles.muted}>
              {t("costScenario.eurRate", { rate: formatNumber(eur.ronPerEur) })}
            </p>
          )}
        </section>
      )}
    </main>
  );
}
