import Link from "next/link";
import {
  CONTRACT_STATUS_LABELS,
  getContracts,
  getWorkPackage,
  type Contract,
  type WorkPackage,
} from "@/app/lib/api";
import { formatDate, formatMoney } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function ContractsPage() {
  let contracts: Contract[] = [];
  let error: string | null = null;

  try {
    contracts = await getContracts();
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  // Each contract carries only its work-package id; resolve the names so the table can
  // show what was awarded. The set is small (one contract per work package), so a fan-out
  // of lookups is fine here.
  const workPackages = await Promise.all(
    contracts.map((c) =>
      getWorkPackage(c.workPackageId).catch(() => null as WorkPackage | null),
    ),
  );
  const workPackageName = new Map<string, string>();
  workPackages.forEach((wp) => {
    if (wp) workPackageName.set(wp.id, wp.name);
  });

  return (
    <main className={styles.main}>
      <h1>{t("contracts.title")}</h1>
      <p className={styles.subtitle}>{t("contracts.subtitle")}</p>

      <section className={styles.card}>
        <h2>{t("contracts.all")}</h2>
        {error ? (
          <p className={styles.error}>{t("common.apiError", { error })}</p>
        ) : contracts.length === 0 ? (
          <p>{t("contracts.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("contracts.workPackage")}</th>
                <th>{t("contracts.contractNumberShort")}</th>
                <th>{t("common.status")}</th>
                <th>{t("contracts.value")}</th>
                <th>{t("contracts.signedShort")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {contracts.map((c) => (
                <tr key={c.id}>
                  <td>
                    <Link href={`/contracts/${c.id}`} className={styles.nameLink}>
                      <strong>
                        {workPackageName.get(c.workPackageId) ??
                          t("contracts.workPackage")}
                      </strong>
                    </Link>
                  </td>
                  <td>{c.contractNumber || "—"}</td>
                  <td>
                    <span
                      className={`${styles.badge} ${styles[`status${c.status}`]}`}
                    >
                      {CONTRACT_STATUS_LABELS[c.status]}
                    </span>
                  </td>
                  <td>{formatMoney(c.value)}</td>
                  <td>{formatDate(c.signedOn)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link href={`/contracts/${c.id}`} className={styles.edit}>
                        {t("contracts.view")}
                      </Link>
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
