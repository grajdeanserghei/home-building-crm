import Link from "next/link";
import { notFound } from "next/navigation";
import { getProject, getCostScenarios, type CostScenarioSummary } from "@/app/lib/api";
import { deleteCostScenario } from "@/app/cost-scenarios/actions";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { formatDateTime } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function CostScenariosPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const project = await getProject(id);

  if (!project) {
    notFound();
  }

  let scenarios: CostScenarioSummary[] = [];
  let error: string | null = null;

  try {
    scenarios = await getCostScenarios(id);
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  return (
    <main className={styles.main}>
      <Link href={`/projects/${id}`} className={styles.backLink}>
        {t("costScenario.backToProject")}
      </Link>
      <h1>{t("costScenario.listTitle", { name: project.name })}</h1>
      <p className={styles.subtitle}>{t("costScenario.subtitle")}</p>

      <section className={styles.card}>
        {error ? (
          <p className={styles.error}>{t("common.apiError", { error })}</p>
        ) : scenarios.length === 0 ? (
          <p>{t("costScenario.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("common.name")}</th>
                <th>{t("costScenario.col.workPackages")}</th>
                <th>{t("costScenario.col.created")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {scenarios.map((scenario) => (
                <tr key={scenario.id}>
                  <td>
                    <Link
                      href={`/projects/${id}/cost-scenarios/${scenario.id}`}
                      className={styles.nameLink}
                    >
                      <strong>{scenario.name}</strong>
                    </Link>
                  </td>
                  <td>{scenario.workPackageCount}</td>
                  <td>{formatDateTime(scenario.createdAt)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link
                        href={`/projects/${id}/cost-scenarios/${scenario.id}/edit`}
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
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <p>
        <Link
          href={`/projects/${id}/cost-scenarios/new`}
          className={styles.primaryButton}
        >
          {t("costScenario.add")}
        </Link>
      </p>
    </main>
  );
}
