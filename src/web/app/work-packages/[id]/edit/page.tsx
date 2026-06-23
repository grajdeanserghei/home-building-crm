import Link from "next/link";
import { notFound } from "next/navigation";
import { WorkPackageForm } from "@/app/components/WorkPackageForm";
import { getWorkPackage } from "@/app/lib/api";
import { updateWorkPackage } from "@/app/work-packages/actions";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function EditWorkPackagePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const workPackage = await getWorkPackage(id);

  if (!workPackage) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>{t("workPackages.edit")}</h1>
      <p className={styles.subtitle}>
        {t("workPackages.editSubtitle", { name: workPackage.name })}
      </p>

      <section className={styles.card}>
        <WorkPackageForm
          action={updateWorkPackage}
          projectId={workPackage.projectId}
          workPackage={workPackage}
          submitLabel={t("common.saveChanges")}
        />
        <Link
          href={`/projects/${workPackage.projectId}`}
          className={styles.backLink}
        >
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
