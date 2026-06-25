import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { changeWorkPackageStatus } from "@/app/work-packages/actions";
import {
  getWorkPackage,
  WORK_PACKAGE_STATUS_LABELS,
  type WorkPackageStatus,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// The statuses a package may move to from its current one, given the lifecycle. Awarded is
// excluded — it is reached only through the award flow (selecting a bid, creating a
// contract), not this control. Completed and Cancelled are terminal.
function allowedTargets(current: WorkPackageStatus): WorkPackageStatus[] {
  switch (current) {
    case "Defined":
      return ["OpenForBids", "Cancelled"];
    case "OpenForBids":
      return ["Defined", "Cancelled"];
    case "Awarded":
      return ["InProgress", "Cancelled"];
    case "InProgress":
      return ["Completed", "Cancelled"];
    default:
      return [];
  }
}

export default async function ChangeWorkPackageStatusPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const workPackage = await getWorkPackage(id);

  if (!workPackage) {
    notFound();
  }

  const targets = allowedTargets(workPackage.status);

  // A terminal package has no onward transitions — nothing to change here.
  if (targets.length === 0) {
    redirect(`/work-packages/${id}`);
  }

  return (
    <main className={styles.main}>
      <Link href={`/work-packages/${workPackage.id}`} className={styles.backLink}>
        {t("workPackages.backToWorkPackage")}
      </Link>
      <h1>{t("workPackages.changeStatusTitle")}</h1>
      <p className={styles.subtitle}>{t("workPackages.changeStatusSubtitle")}</p>

      <section className={styles.card}>
        <form action={changeWorkPackageStatus} className={styles.form}>
          <input type="hidden" name="id" value={workPackage.id} />
          <input type="hidden" name="projectId" value={workPackage.projectId} />
          <select name="status" defaultValue={targets[0]}>
            {targets.map((s) => (
              <option key={s} value={s}>
                {WORK_PACKAGE_STATUS_LABELS[s]}
              </option>
            ))}
          </select>
          <button type="submit">{t("workPackages.updateStatus")}</button>
        </form>
        <p className={styles.muted}>{t("workPackages.awardingHint")}</p>
      </section>
    </main>
  );
}
