import Link from "next/link";
import { notFound } from "next/navigation";
import { BidForm } from "@/app/components/BidForm";
import { ScopeItemForm } from "@/app/components/ScopeItemForm";
import { openBid } from "@/app/bids/actions";
import {
  addScopeItem,
  changeWorkPackageStatus,
  removeScopeItem,
} from "@/app/work-packages/actions";
import {
  BID_STATUS_LABELS,
  getBids,
  getContractors,
  getWorkPackage,
  SCOPE_ITEM_REQUIREMENT_LABELS,
  WORK_PACKAGE_STATUS_LABELS,
  type Bid,
  type Contractor,
  type WorkPackageStatus,
} from "@/app/lib/api";
import styles from "@/app/page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

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

  const targets = allowedTargets(workPackage.status);
  const scopeItems = workPackage.scopeItems;

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

      {workPackage.awardedContractId ? (
        <section className={styles.card}>
          <h2>Contract</h2>
          <p>
            This work package has been awarded.{" "}
            <Link
              href={`/contracts/${workPackage.awardedContractId}`}
              className={styles.nameLink}
            >
              View contract →
            </Link>
          </p>
        </section>
      ) : null}

      <section className={styles.card}>
        <h2>Change status</h2>
        {targets.length === 0 ? (
          <p className={styles.muted}>
            This work package is{" "}
            {WORK_PACKAGE_STATUS_LABELS[workPackage.status].toLowerCase()} — its status is
            final.
          </p>
        ) : (
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
            <button type="submit">Update status</button>
          </form>
        )}
        <p className={styles.muted}>
          Awarding happens automatically when a bid is selected and its contract created —
          it is not set here.
        </p>
      </section>

      <section className={styles.card}>
        <h2>Scope items</h2>
        <p className={styles.muted}>
          The owner-defined sub-scopes of this work package — what must be done, and what
          could be dropped or deferred if the budget is tight.
        </p>
        {scopeItems.length === 0 ? (
          <p>No scope items yet. Add the first one below.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>#</th>
                <th>Name</th>
                <th>Requirement</th>
                <th aria-label="actions" />
              </tr>
            </thead>
            <tbody>
              {scopeItems.map((si) => (
                <tr key={si.id}>
                  <td>{si.sequence}</td>
                  <td>
                    <strong>{si.name}</strong>
                    {si.description ? (
                      <div className={styles.muted}>{si.description}</div>
                    ) : null}
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${
                        styles[`requirement${si.requirement}`]
                      }`}
                    >
                      {SCOPE_ITEM_REQUIREMENT_LABELS[si.requirement]}
                    </span>
                  </td>
                  <td>
                    <div className={styles.actions}>
                      <form action={removeScopeItem}>
                        <input
                          type="hidden"
                          name="workPackageId"
                          value={workPackage.id}
                        />
                        <input type="hidden" name="scopeItemId" value={si.id} />
                        <button type="submit" className={styles.delete}>
                          Remove
                        </button>
                      </form>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <h2 style={{ marginTop: 20 }}>Add scope item</h2>
        <ScopeItemForm
          action={addScopeItem}
          workPackageId={workPackage.id}
          defaultSequence={scopeItems.length + 1}
        />
      </section>

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
