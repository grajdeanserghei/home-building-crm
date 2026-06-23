import Link from "next/link";
import { notFound } from "next/navigation";
import { updateProject } from "@/app/actions";
import { ProjectForm } from "@/app/components/ProjectForm";
import { getProject } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
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
      <h1>{t("projects.edit")}</h1>
      <p className={styles.subtitle}>
        {t("projects.editSubtitle", { name: project.name })}
      </p>

      <section className={styles.card}>
        <ProjectForm
          action={updateProject}
          project={project}
          submitLabel={t("common.saveChanges")}
        />
        <Link href="/projects" className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
