import Link from "next/link";
import { createProject, deleteProject } from "./actions";
import { ProjectForm } from "./components/ProjectForm";
import { getProjects, PROJECT_STATUS_LABELS, type Project } from "./lib/api";
import styles from "./page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

export default async function Home() {
  let projects: Project[] = [];
  let error: string | null = null;

  try {
    projects = await getProjects();
  } catch (e) {
    error = e instanceof Error ? e.message : "Unknown error";
  }

  return (
    <main className={styles.main}>
      <h1>Home Project Management</h1>
      <p className={styles.subtitle}>
        Next.js frontend · .NET API · PostgreSQL — orchestrated by .NET Aspire.
      </p>

      <section className={styles.card}>
        <h2>New project</h2>
        <ProjectForm action={createProject} submitLabel="Add project" />
      </section>

      <section className={styles.card}>
        <h2>Projects</h2>
        {error ? (
          <p className={styles.error}>Could not reach the API: {error}</p>
        ) : projects.length === 0 ? (
          <p>No projects yet. Add your first one above.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Due</th>
                <th>Created</th>
                <th aria-label="actions" />
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
                        Edit
                      </Link>
                      <form action={deleteProject}>
                        <input type="hidden" name="id" value={p.id} />
                        <button type="submit" className={styles.delete}>
                          Delete
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
