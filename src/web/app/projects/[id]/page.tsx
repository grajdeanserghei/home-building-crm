import Link from "next/link";
import { notFound } from "next/navigation";
import {
  getProject,
  getWorkPackages,
  WORK_PACKAGE_STATUS_LABELS,
  type WorkPackage,
} from "@/app/lib/api";
import { deleteWorkPackage } from "@/app/work-packages/actions";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { formatDate } from "@/app/lib/format";
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
        {error ? (
          <p className={styles.error}>{t("common.apiError", { error })}</p>
        ) : workPackages.length === 0 ? (
          <p>{t("workPackages.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>#</th>
                <th>{t("common.name")}</th>
                <th>{t("common.status")}</th>
                <th>{t("workPackages.col.plannedStart")}</th>
                <th>{t("workPackages.col.plannedEnd")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {workPackages.map((wp) => (
                <tr key={wp.id}>
                  <td>{wp.sequence}</td>
                  <td>
                    <Link
                      href={`/work-packages/${wp.id}`}
                      className={styles.nameLink}
                    >
                      <strong>{wp.name}</strong>
                    </Link>
                    {wp.description ? (
                      <div className={styles.muted}>{wp.description}</div>
                    ) : null}
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${styles[`status${wp.status}`]}`}
                    >
                      {WORK_PACKAGE_STATUS_LABELS[wp.status]}
                    </span>
                  </td>
                  <td>{formatDate(wp.plannedStartDate)}</td>
                  <td>{formatDate(wp.plannedEndDate)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link
                        href={`/work-packages/${wp.id}/edit`}
                        className={styles.edit}
                      >
                        {t("common.edit")}
                      </Link>
                      <ConfirmDeleteButton
                        action={deleteWorkPackage}
                        fields={{ id: wp.id, projectId: project.id }}
                        title={t("workPackages.deleteTitle")}
                        bodyTemplate={t("workPackages.deleteBody")}
                        name={wp.name}
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
          href={`/projects/${project.id}/work-packages/new`}
          className={styles.primaryButton}
        >
          {t("workPackages.add")}
        </Link>
      </p>
    </main>
  );
}
