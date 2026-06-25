import Link from "next/link";
import { notFound } from "next/navigation";
import { getCostScenario } from "@/app/lib/api";
import { updateCostScenario } from "@/app/cost-scenarios/actions";
import { CostScenarioForm } from "@/app/components/CostScenarioForm";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function EditCostScenarioPage({
  params,
}: {
  params: Promise<{ id: string; scenarioId: string }>;
}) {
  const { id, scenarioId } = await params;
  const scenario = await getCostScenario(scenarioId);

  if (!scenario) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>{t("costScenario.editTitle", { name: scenario.name })}</h1>

      <section className={styles.card}>
        <CostScenarioForm
          action={updateCostScenario}
          projectId={id}
          scenario={{
            id: scenario.id,
            name: scenario.name,
            description: scenario.description,
          }}
          submitLabel={t("common.save")}
        />
        <Link
          href={`/projects/${id}/cost-scenarios/${scenarioId}`}
          className={styles.backLink}
        >
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
