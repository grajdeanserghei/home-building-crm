import Link from "next/link";
import { notFound } from "next/navigation";
import { updateProject } from "@/app/actions";
import { ProjectForm } from "@/app/components/ProjectForm";
import { getProject } from "@/app/lib/api";
import styles from "@/app/page.module.css";

export default async function EditProjectPage({
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
      <h1>Edit project</h1>
      <p className={styles.subtitle}>
        Update the details for &ldquo;{project.name}&rdquo;.
      </p>

      <section className={styles.card}>
        <ProjectForm
          action={updateProject}
          project={project}
          submitLabel="Save changes"
        />
        <Link href="/" className={styles.backLink}>
          Cancel
        </Link>
      </section>
    </main>
  );
}
