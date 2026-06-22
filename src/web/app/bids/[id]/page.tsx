import Link from "next/link";
import { notFound } from "next/navigation";
import { BidNoteForm } from "@/app/components/BidNoteForm";
import { BillOfQuantitiesForm } from "@/app/components/BillOfQuantitiesForm";
import {
  changeBidStatus,
  deleteBid,
  logBidNote,
  removeBidNote,
} from "@/app/bids/actions";
import { draftBoq } from "@/app/bills-of-quantities/actions";
import {
  BID_STATUS_LABELS,
  BID_STATUSES,
  BOQ_STATUS_LABELS,
  NOTE_TYPE_LABELS,
  formatMoney,
  getBid,
  getBillsOfQuantities,
  getContractor,
  getWorkPackage,
  type BidStatus,
} from "@/app/lib/api";
import styles from "@/app/page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

// The statuses a bid may move to from its current one. A Withdrawn bid is terminal, so
// it has no targets; selecting is unavailable from Rejected (the backend forbids it).
function allowedTargets(current: BidStatus): BidStatus[] {
  if (current === "Withdrawn") return [];
  return BID_STATUSES.filter(
    (s) => s !== current && !(current === "Rejected" && s === "Selected"),
  );
}

export default async function BidDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const bid = await getBid(id);

  if (!bid) {
    notFound();
  }

  const [contractor, workPackage, boqs] = await Promise.all([
    getContractor(bid.contractorId),
    getWorkPackage(bid.workPackageId),
    getBillsOfQuantities(bid.id),
  ]);

  const contractorName = contractor?.name ?? "Unknown contractor";
  const targets = allowedTargets(bid.status);
  // `new Date()` here runs server-side at request time; the picker seed is just a default.
  const today = new Date().toISOString().slice(0, 10);

  return (
    <main className={styles.main}>
      <Link
        href={`/work-packages/${bid.workPackageId}`}
        className={styles.backLink}
      >
        ← Back to {workPackage?.name ?? "work package"}
      </Link>
      <h1>{contractorName}</h1>
      <p className={styles.subtitle}>
        Bid on {workPackage?.name ?? "this work package"}
        {" · "}
        <span className={`${styles.badge} ${styles[`status${bid.status}`]}`}>
          {BID_STATUS_LABELS[bid.status]}
        </span>
      </p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>Status</dt>
          <dd>{BID_STATUS_LABELS[bid.status]}</dd>
          <dt>First contacted</dt>
          <dd>{formatDate(bid.firstContactedOn)}</dd>
          <dt>Summary</dt>
          <dd>{bid.summary || "—"}</dd>
          <dt>Opened</dt>
          <dd>{formatDate(bid.createdAt)}</dd>
        </dl>
        <div className={styles.actions}>
          <Link href={`/bids/${bid.id}/edit`} className={styles.edit}>
            Edit
          </Link>
          <form action={deleteBid}>
            <input type="hidden" name="id" value={bid.id} />
            <input
              type="hidden"
              name="workPackageId"
              value={bid.workPackageId}
            />
            <button type="submit" className={styles.delete}>
              Delete
            </button>
          </form>
        </div>
      </section>

      <section className={styles.card}>
        <h2>Change status</h2>
        {targets.length === 0 ? (
          <p className={styles.muted}>
            This bid is withdrawn — its status is final.
          </p>
        ) : (
          <form action={changeBidStatus} className={styles.form}>
            <input type="hidden" name="id" value={bid.id} />
            <input
              type="hidden"
              name="workPackageId"
              value={bid.workPackageId}
            />
            <select name="status" defaultValue={targets[0]}>
              {targets.map((s) => (
                <option key={s} value={s}>
                  {BID_STATUS_LABELS[s]}
                </option>
              ))}
            </select>
            <button type="submit">Update status</button>
          </form>
        )}
        <p className={styles.muted}>
          Selecting this bid as the winner rejects the other live bids on this
          work package.
        </p>
      </section>

      <section className={styles.card}>
        <h2>Bills of quantities</h2>
        {boqs.length === 0 ? (
          <p>
            No bills of quantities yet. Draft the contractor&apos;s first version
            below.
          </p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Version</th>
                <th>Reference</th>
                <th>Status</th>
                <th>Total (incl. VAT)</th>
                <th aria-label="actions" />
              </tr>
            </thead>
            <tbody>
              {boqs.map((boq) => (
                <tr key={boq.id}>
                  <td>
                    <Link
                      href={`/bills-of-quantities/${boq.id}`}
                      className={styles.nameLink}
                    >
                      <strong>v{boq.version}</strong>
                    </Link>
                  </td>
                  <td>{boq.reference || "—"}</td>
                  <td>
                    <span
                      className={`${styles.badge} ${styles[`status${boq.status}`]}`}
                    >
                      {BOQ_STATUS_LABELS[boq.status]}
                    </span>
                  </td>
                  <td>{formatMoney(boq.totalWithVat)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link
                        href={`/bills-of-quantities/${boq.id}`}
                        className={styles.edit}
                      >
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

      <section className={styles.card}>
        <h2>Draft a bill of quantities</h2>
        <BillOfQuantitiesForm
          action={draftBoq}
          bidId={bid.id}
          submitLabel="Draft BoQ"
        />
      </section>

      <section className={styles.card}>
        <h2>Log a note</h2>
        <BidNoteForm action={logBidNote} bidId={bid.id} today={today} />
      </section>

      <section className={styles.card}>
        <h2>Discussion log</h2>
        {bid.notes.length === 0 ? (
          <p>No notes yet. Log meetings, calls and emails above.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>When</th>
                <th>Type</th>
                <th>Note</th>
                <th aria-label="actions" />
              </tr>
            </thead>
            <tbody>
              {bid.notes.map((n) => (
                <tr key={n.id}>
                  <td>{formatDate(n.occurredOn)}</td>
                  <td>{NOTE_TYPE_LABELS[n.type]}</td>
                  <td>{n.content}</td>
                  <td>
                    <div className={styles.actions}>
                      <form action={removeBidNote}>
                        <input type="hidden" name="bidId" value={bid.id} />
                        <input type="hidden" name="noteId" value={n.id} />
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
      </section>
    </main>
  );
}
