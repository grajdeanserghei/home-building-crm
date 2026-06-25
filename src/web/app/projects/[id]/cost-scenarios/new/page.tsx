import Link from "next/link";
import { notFound } from "next/navigation";
import { getProject } from "@/app/lib/api";
import { createCostScenario } from "@/app/cost-scenarios/actions";
import { CostScenarioForm } from "@/app/components/CostScenarioForm";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function NewCostScenarioPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const project = await getProject(id);

  if (!project) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>{t("costScenario.new")}</h1>
      <p className={styles.subtitle}>{t("costScenario.createSubtitle")}</p>

      <section className={styles.card}>
        <CostScenarioForm
          action={createCostScenario}
          projectId={id}
          submitLabel={t("costScenario.add")}
        />
        <Link href={`/projects/${id}/cost-scenarios`} className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
