import Link from "next/link";
import {
  CONTRACT_STATUS_LABELS,
  formatMoney,
  getContracts,
  getWorkPackage,
  type Contract,
  type WorkPackage,
} from "@/app/lib/api";
import styles from "@/app/page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

export default async function ContractsPage() {
  let contracts: Contract[] = [];
  let error: string | null = null;

  try {
    contracts = await getContracts();
  } catch (e) {
    error = e instanceof Error ? e.message : "Unknown error";
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
      <h1>Contracts</h1>
      <p className={styles.subtitle}>
        Awarded contracts across all work packages. A contract is created by awarding a
        work package from its accepted bill of quantities.
      </p>

      <section className={styles.card}>
        <h2>All contracts</h2>
        {error ? (
          <p className={styles.error}>Could not reach the API: {error}</p>
        ) : contracts.length === 0 ? (
          <p>
            No contracts yet. Award one from an accepted bill of quantities on a bid.
          </p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Work package</th>
                <th>Contract no.</th>
                <th>Status</th>
                <th>Value</th>
                <th>Signed</th>
                <th aria-label="actions" />
              </tr>
            </thead>
            <tbody>
              {contracts.map((c) => (
                <tr key={c.id}>
                  <td>
                    <Link href={`/contracts/${c.id}`} className={styles.nameLink}>
                      <strong>
                        {workPackageName.get(c.workPackageId) ?? "Work package"}
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
                        View
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
