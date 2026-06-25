import Link from "next/link";
import { notFound } from "next/navigation";
import { WorkPackageForm } from "@/app/components/WorkPackageForm";
import { defineWorkPackage } from "@/app/work-packages/actions";
import { getProject, getWorkPackages, type WorkPackage } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Defining a work package is a deliberate act on its own route, nested under the project
// it belongs to. The form's success action revalidates and returns to the project's list.
export default async function NewWorkPackagePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const project = await getProject(id);

  if (!project) {
    notFound();
  }

  // Suggest the next order (one past the existing packages). An API hiccup just falls
  // back to 1 — the field stays editable either way.
  let workPackages: WorkPackage[] = [];
  try {
    workPackages = await getWorkPackages(id);
  } catch {
    workPackages = [];
  }

  return (
    <main className={styles.main}>
      <Link href={`/projects/${project.id}`} className={styles.backLink}>
        {t("workPackages.backToProject")}
      </Link>
      <h1>{t("workPackages.new")}</h1>
      <p className={styles.subtitle}>{t("workPackages.createSubtitle")}</p>

      <section className={styles.card}>
        <WorkPackageForm
          action={defineWorkPackage}
          projectId={project.id}
          defaultSequence={workPackages.length + 1}
          submitLabel={t("workPackages.add")}
        />
        <Link href={`/projects/${project.id}`} className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
