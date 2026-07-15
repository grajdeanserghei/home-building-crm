import Link from "next/link";
import { notFound } from "next/navigation";
import {
  getCostScenario,
  getScenarioCandidates,
  getScenarioValuationComparison,
  type ScenarioCandidateWorkPackage,
} from "@/app/lib/api";
import { deleteCostScenario } from "@/app/cost-scenarios/actions";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { CostScenarioView } from "@/app/components/CostScenarioView";
import { ValuationComparisonTable } from "@/app/components/ValuationComparisonTable";
import { getDisplayCurrency } from "@/app/lib/display-currency";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function CostScenarioPage({
  params,
}: {
  params: Promise<{ id: string; scenarioId: string }>;
}) {
  const { id, scenarioId } = await params;
  const [scenario, candidatesData, valuation, displayCurrency] =
    await Promise.all([
      getCostScenario(scenarioId),
      getScenarioCandidates(id),
      getScenarioValuationComparison(scenarioId),
      getDisplayCurrency(),
    ]);

  if (!scenario) {
    notFound();
  }

  const candidates: ScenarioCandidateWorkPackage[] = candidatesData ?? [];

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

      <CostScenarioView
        scenario={scenario}
        candidates={candidates}
        projectId={id}
        displayCurrency={displayCurrency}
      />

      <h2>{t("valuation.vsBoq.scenarioCardTitle")}</h2>
      <p className={styles.subtitle}>
        {t("valuation.vsBoq.scenarioCardSubtitle")}
      </p>
      {valuation ? (
        <ValuationComparisonTable
          comparison={valuation}
          displayCurrency={displayCurrency}
        />
      ) : (
        <section className={styles.card}>
          <p className={styles.muted}>
            {t("valuation.vsBoq.scenarioCardEmpty")}
          </p>
        </section>
      )}
    </main>
  );
}
