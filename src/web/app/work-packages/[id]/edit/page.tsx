import Link from "next/link";
import { notFound } from "next/navigation";
import { WorkPackageForm } from "@/app/components/WorkPackageForm";
import { getWorkPackage } from "@/app/lib/api";
import { updateWorkPackage } from "@/app/work-packages/actions";
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
      <h1>Edit work package</h1>
      <p className={styles.subtitle}>
        Update the details for &ldquo;{workPackage.name}&rdquo;.
      </p>

      <section className={styles.card}>
        <WorkPackageForm
          action={updateWorkPackage}
          projectId={workPackage.projectId}
          workPackage={workPackage}
          submitLabel="Save changes"
        />
        <Link
          href={`/projects/${workPackage.projectId}`}
          className={styles.backLink}
        >
          Cancel
        </Link>
      </section>
    </main>
  );
}
