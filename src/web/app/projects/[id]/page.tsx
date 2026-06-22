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
import styles from "@/app/page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

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
    error = e instanceof Error ? e.message : "Unknown error";
  }

  return (
    <main className={styles.main}>
      <Link href="/" className={styles.backLink}>
        ← All projects
      </Link>
      <h1>{project.name}</h1>
      <p className={styles.subtitle}>
        {project.description || "Work packages for this project."}
      </p>

      <section className={styles.card}>
        <h2>New work package</h2>
        <WorkPackageForm
          action={defineWorkPackage}
          projectId={project.id}
          defaultSequence={workPackages.length}
          submitLabel="Add work package"
        />
      </section>

      <section className={styles.card}>
        <h2>Work packages</h2>
        {error ? (
          <p className={styles.error}>Could not reach the API: {error}</p>
        ) : workPackages.length === 0 ? (
          <p>No work packages yet. Define your first one above.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>#</th>
                <th>Name</th>
                <th>Status</th>
                <th>Planned start</th>
                <th>Planned end</th>
                <th aria-label="actions" />
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
                        Edit
                      </Link>
                      <form action={deleteWorkPackage}>
                        <input type="hidden" name="id" value={wp.id} />
                        <input
                          type="hidden"
                          name="projectId"
                          value={project.id}
                        />
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
