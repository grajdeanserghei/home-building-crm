import Link from "next/link";
import { deleteProject } from "./actions";
import { DeleteProjectButton } from "./components/DeleteProjectButton";
import {
  getProjects,
  PROJECT_STATUS_LABELS,
  PROJECT_STATUSES,
  type Project,
} from "./lib/api";
import { formatDate } from "./lib/format";
import { t } from "./lib/i18n";
import styles from "./page.module.css";

export default async function Home() {
  let projects: Project[] = [];
  let error: string | null = null;

  try {
    projects = await getProjects();
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  // Count projects per status for the dashboard summary strip (computed from the
  // already-loaded list — no extra request).
  const countByStatus = projects.reduce<Record<string, number>>((acc, p) => {
    acc[p.status] = (acc[p.status] ?? 0) + 1;
    return acc;
  }, {});

  return (
    <main className={styles.main}>
      <div className={styles.toolbar}>
        <div>
          <h1>{t("meta.title")}</h1>
          <p className={styles.subtitle}>{t("projects.subtitle")}</p>
        </div>
        <Link href="/projects/new" className={styles.primaryButton}>
          {t("projects.add")}
        </Link>
      </div>

      {!error && projects.length > 0 ? (
        <section className={styles.stats}>
          <div className={styles.stat}>
            <span className={styles.statValue}>{projects.length}</span>
            <span className={styles.statLabel}>{t("projects.summaryTotal")}</span>
          </div>
          {PROJECT_STATUSES.map((s) => (
            <div key={s} className={styles.stat}>
              <span className={styles.statValue}>{countByStatus[s] ?? 0}</span>
              <span className={styles.statLabel}>
                {PROJECT_STATUS_LABELS[s]}
              </span>
            </div>
          ))}
        </section>
      ) : null}

      <section className={styles.card}>
        <h2>{t("projects.title")}</h2>
        {error ? (
          <p className={styles.error}>{t("common.apiError", { error })}</p>
        ) : projects.length === 0 ? (
          <p>{t("projects.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("common.name")}</th>
                <th>{t("common.status")}</th>
                <th>{t("projects.col.due")}</th>
                <th>{t("common.created")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {projects.map((p) => (
                <tr key={p.id}>
                  <td>
                    <Link href={`/projects/${p.id}`} className={styles.nameLink}>
                      <strong>{p.name}</strong>
                    </Link>
                    {p.description ? (
                      <div className={styles.muted}>{p.description}</div>
                    ) : null}
                  </td>
                  <td>
                    <span className={`${styles.badge} ${styles[`status${p.status}`]}`}>
                      {PROJECT_STATUS_LABELS[p.status]}
                    </span>
                  </td>
                  <td>{formatDate(p.dueDate)}</td>
                  <td>{formatDate(p.createdAt)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link
                        href={`/projects/${p.id}/edit`}
                        className={styles.edit}
                      >
                        {t("common.edit")}
                      </Link>
                      <DeleteProjectButton
                        action={deleteProject}
                        projectId={p.id}
                        projectName={p.name}
                      />
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
