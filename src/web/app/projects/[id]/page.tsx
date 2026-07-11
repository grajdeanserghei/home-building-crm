import Link from "next/link";
import { notFound } from "next/navigation";
import { getProject, getWorkPackages, type WorkPackage } from "@/app/lib/api";
import { WorkPackagesTable } from "@/app/components/WorkPackagesTable";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function ProjectDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const project = await getProject(id);

  if (!project) {
    notFound();
  }

  let workPackages: WorkPackage[] = [];
  let error: string | null = null;

  try {
    workPackages = await getWorkPackages(id);
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  return (
    <main className={styles.main}>
      <Link href="/projects" className={styles.backLink}>
        {t("projects.backToAll")}
      </Link>

      <h1>{project.name}</h1>
      <p className={styles.subtitle}>
        {project.description || t("projects.workPackagesSubtitle")}
      </p>

      <p className={styles.muted}>
        {t("projects.apartmentUnitsSummary", {
          count: String(project.apartmentUnits),
        })}
      </p>

      <div className={styles.linkRow}>
        <Link href={`/projects/${project.id}/budget`} className={styles.edit}>
          {t("budget.link")} →
        </Link>
        <Link href={`/projects/${project.id}/bids`} className={styles.edit}>
          {t("projectBids.link")} →
        </Link>
        <Link
          href={`/projects/${project.id}/cost-scenarios`}
          className={styles.edit}
        >
          {t("costScenario.link")} →
        </Link>
      </div>

      <section className={styles.card}>
        <h2>{t("projects.workPackages")}</h2>
        <WorkPackagesTable
          workPackages={workPackages}
          projectId={project.id}
          error={error}
        />
      </section>

      <p>
        <Link
          href={`/projects/${project.id}/work-packages/new`}
          className={styles.primaryButton}
        >
          {t("workPackages.add")}
        </Link>
      </p>
    </main>
  );
}
