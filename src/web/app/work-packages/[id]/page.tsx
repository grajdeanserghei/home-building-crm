import Link from "next/link";
import { notFound } from "next/navigation";
import { BidForm } from "@/app/components/BidForm";
import { openBid } from "@/app/bids/actions";
import {
  BID_STATUS_LABELS,
  getBids,
  getContractors,
  getWorkPackage,
  WORK_PACKAGE_STATUS_LABELS,
  type Bid,
  type Contractor,
} from "@/app/lib/api";
import styles from "@/app/page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

export default async function WorkPackageDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const workPackage = await getWorkPackage(id);

  if (!workPackage) {
    notFound();
  }

  let bids: Bid[] = [];
  let contractors: Contractor[] = [];
  let error: string | null = null;

  try {
    [bids, contractors] = await Promise.all([
      getBids(id),
      getContractors(),
    ]);
  } catch (e) {
    error = e instanceof Error ? e.message : "Unknown error";
  }

  // Map contractor id → name for the bids table, and offer only contractors that don't
  // already have a bid here (the backend rejects a duplicate pair with a 409).
  const contractorName = new Map(contractors.map((c) => [c.id, c.name]));
  const taken = new Set(bids.map((b) => b.contractorId));
  const available = contractors.filter((c) => !taken.has(c.id));

  return (
    <main className={styles.main}>
      <Link href={`/projects/${workPackage.projectId}`} className={styles.backLink}>
        ← Back to project
      </Link>
      <h1>{workPackage.name}</h1>
      <p className={styles.subtitle}>
        {workPackage.description || "Bids for this work package."}
        {" · "}
        <span
          className={`${styles.badge} ${styles[`status${workPackage.status}`]}`}
        >
          {WORK_PACKAGE_STATUS_LABELS[workPackage.status]}
        </span>
      </p>

      <section className={styles.card}>
        <h2>New bid</h2>
        {available.length === 0 ? (
          <p className={styles.muted}>
            {contractors.length === 0
              ? "No contractors registered yet — add one under Contractors first."
              : "Every registered contractor already has a bid on this work package."}
          </p>
        ) : (
          <BidForm
            action={openBid}
            workPackageId={workPackage.id}
            contractors={available}
            submitLabel="Open bid"
          />
        )}
      </section>

      <section className={styles.card}>
        <h2>Bids</h2>
        {error ? (
          <p className={styles.error}>Could not reach the API: {error}</p>
        ) : bids.length === 0 ? (
          <p>No bids yet. Open one with a contractor above.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Contractor</th>
                <th>Status</th>
                <th>First contact</th>
                <th>Notes</th>
                <th aria-label="actions" />
              </tr>
            </thead>
            <tbody>
              {bids.map((b) => (
                <tr key={b.id}>
                  <td>
                    <Link href={`/bids/${b.id}`} className={styles.nameLink}>
                      <strong>
                        {contractorName.get(b.contractorId) ?? "Unknown contractor"}
                      </strong>
                    </Link>
                    {b.summary ? (
                      <div className={styles.muted}>{b.summary}</div>
                    ) : null}
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${styles[`status${b.status}`]}`}
                    >
                      {BID_STATUS_LABELS[b.status]}
                    </span>
                  </td>
                  <td>{formatDate(b.firstContactedOn)}</td>
                  <td>{b.notes.length}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link href={`/bids/${b.id}`} className={styles.edit}>
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
