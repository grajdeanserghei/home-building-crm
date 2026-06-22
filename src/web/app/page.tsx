import { createProject, deleteProject } from "./actions";
import { getProjects, type Project } from "./lib/api";
import styles from "./page.module.css";

const STATUS_OPTIONS = ["Planned", "InProgress", "OnHold", "Completed"] as const;

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
        <form action={createProject} className={styles.form}>
          <input name="name" placeholder="Project name" required />
          <input name="description" placeholder="Description (optional)" />
          <select name="status" defaultValue="Planned">
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
          <input name="dueDate" type="date" />
          <button type="submit">Add project</button>
        </form>
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
                    <strong>{p.name}</strong>
                    {p.description ? (
                      <div className={styles.muted}>{p.description}</div>
                    ) : null}
                  </td>
                  <td>{p.status}</td>
                  <td>{formatDate(p.dueDate)}</td>
                  <td>{formatDate(p.createdAt)}</td>
                  <td>
                    <form action={deleteProject}>
                      <input type="hidden" name="id" value={p.id} />
                      <button type="submit" className={styles.delete}>
                        Delete
                      </button>
                    </form>
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
