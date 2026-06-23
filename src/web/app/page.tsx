import Link from "next/link";
import { createProject, deleteProject } from "./actions";
import { DeleteProjectButton } from "./components/DeleteProjectButton";
import { ProjectForm } from "./components/ProjectForm";
import { getProjects, PROJECT_STATUS_LABELS, type Project } from "./lib/api";
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

  return (
    <main className={styles.main}>
      <h1 style={{ marginBottom: 32 }}>{t("meta.title")}</h1>

      <section className={styles.card}>
        <h2>{t("projects.new")}</h2>
        <ProjectForm action={createProject} submitLabel={t("projects.add")} />
      </section>

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
