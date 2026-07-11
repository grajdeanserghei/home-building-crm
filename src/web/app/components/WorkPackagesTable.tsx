import Link from "next/link";
import { WORK_PACKAGE_STATUS_LABELS, type WorkPackage } from "@/app/lib/api";
import { deleteWorkPackage } from "@/app/work-packages/actions";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { formatDate } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface WorkPackagesTableProps {
  // The project's work packages, in display order.
  workPackages: WorkPackage[];
  // Owning project id — needed by the delete action to revalidate the project's list.
  projectId: string;
  // Error string from loading the work packages, if any.
  error?: string | null;
}

/**
 * The project's work-packages table with per-row Edit/Delete actions. Shared by the home
 * dashboard and the project-overview page so both stay identical. Stays a server component:
 * it embeds the `deleteWorkPackage` server action and the `ConfirmDeleteButton` client
 * component, exactly as the overview page did when it owned this markup inline.
 */
export function WorkPackagesTable({
  workPackages,
  projectId,
  error,
}: WorkPackagesTableProps) {
  if (error) {
    return <p className={styles.error}>{t("common.apiError", { error })}</p>;
  }

  if (workPackages.length === 0) {
    return <p>{t("workPackages.empty")}</p>;
  }

  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th>#</th>
          <th>{t("common.name")}</th>
          <th>{t("common.status")}</th>
          <th>{t("workPackages.col.plannedStart")}</th>
          <th>{t("workPackages.col.plannedEnd")}</th>
          <th aria-label={t("common.actions")} />
        </tr>
      </thead>
      <tbody>
        {workPackages.map((wp) => (
          <tr key={wp.id}>
            <td>{wp.sequence}</td>
            <td>
              <Link href={`/work-packages/${wp.id}`} className={styles.nameLink}>
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
                  {t("common.edit")}
                </Link>
                <ConfirmDeleteButton
                  action={deleteWorkPackage}
                  fields={{ id: wp.id, projectId }}
                  title={t("workPackages.deleteTitle")}
                  bodyTemplate={t("workPackages.deleteBody")}
                  name={wp.name}
                />
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
