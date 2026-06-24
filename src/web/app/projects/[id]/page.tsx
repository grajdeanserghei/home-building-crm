import Link from "next/link";
import { notFound } from "next/navigation";
import { WorkPackageForm } from "@/app/components/WorkPackageForm";
import {
  getProject,
  getWorkPackages,
  WORK_PACKAGE_STATUS_LABELS,
  type WorkPackage,
} from "@/app/lib/api";
import {
  defineWorkPackage,
  deleteWorkPackage,
} from "@/app/work-packages/actions";
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

      <p>
        <Link href={`/projects/${project.id}/budget`} className={styles.edit}>
          {t("budget.link")} →
        </Link>
        {" · "}
        <Link href={`/projects/${project.id}/bids`} className={styles.edit}>
          {t("projectBids.link")} →
        </Link>
      </p>

      <section className={styles.card}>
        <h2>{t("workPackages.new")}</h2>
        <WorkPackageForm
          action={defineWorkPackage}
          projectId={project.id}
          defaultSequence={workPackages.length + 1}
          submitLabel={t("workPackages.add")}
        />
      </section>

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
                      <form action={deleteWorkPackage}>
                        <input type="hidden" name="id" value={wp.id} />
                        <input
                          type="hidden"
                          name="projectId"
                          value={project.id}
                        />
                        <button type="submit" className={styles.delete}>
                          {t("common.delete")}
                        </button>
                      </form>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  );
}
