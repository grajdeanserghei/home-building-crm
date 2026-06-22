import Link from "next/link";
import { notFound } from "next/navigation";
import {
  changeContractStatus,
  deleteContract,
} from "@/app/contracts/actions";
import {
  CONTRACT_STATUS_LABELS,
  CONTRACT_STATUSES,
  formatMoney,
  getBid,
  getBillOfQuantities,
  getContract,
  getContractor,
  getWorkPackage,
  type ContractStatus,
} from "@/app/lib/api";
import styles from "@/app/page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

// The statuses a contract may move to from its current one. Completed and Terminated
// are terminal (closed) — the backend forbids transitioning out of them.
function allowedTargets(current: ContractStatus): ContractStatus[] {
  if (current === "Completed" || current === "Terminated") return [];
  return CONTRACT_STATUSES.filter((s) => s !== current);
}

export default async function ContractDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const contract = await getContract(id);

  if (!contract) {
    notFound();
  }

  // Resolve the related entities for display: the awarded work package, the accepted
  // BoQ this contract is based on, and (through the BoQ's bid) the contractor.
  const [workPackage, boq] = await Promise.all([
    getWorkPackage(contract.workPackageId),
    getBillOfQuantities(contract.acceptedBoqId),
  ]);
  const bid = boq ? await getBid(boq.bidId) : null;
  const contractor = bid ? await getContractor(bid.contractorId) : null;

  const targets = allowedTargets(contract.status);
  const title = contract.contractNumber
    ? `Contract ${contract.contractNumber}`
    : `Contract for ${workPackage?.name ?? "work package"}`;

  return (
    <main className={styles.main}>
      <Link
        href={`/work-packages/${contract.workPackageId}`}
        className={styles.backLink}
      >
        ← Back to {workPackage?.name ?? "work package"}
      </Link>
      <h1>{title}</h1>
      <p className={styles.subtitle}>
        {workPackage
          ? `Awarded for ${workPackage.name}`
          : "Awarded contract"}
        {contractor ? ` · ${contractor.name}` : ""}
        {" · "}
        <span className={`${styles.badge} ${styles[`status${contract.status}`]}`}>
          {CONTRACT_STATUS_LABELS[contract.status]}
        </span>
        {" · "}
        <strong>{formatMoney(contract.value)}</strong>
      </p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>Work package</dt>
          <dd>
            <Link
              href={`/work-packages/${contract.workPackageId}`}
              className={styles.nameLink}
            >
              {workPackage?.name ?? contract.workPackageId}
            </Link>
          </dd>
          <dt>Contractor</dt>
          <dd>
            {contractor ? (
              <Link
                href={`/contractors/${contractor.id}`}
                className={styles.nameLink}
              >
                {contractor.name}
              </Link>
            ) : (
              "—"
            )}
          </dd>
          <dt>Accepted BoQ</dt>
          <dd>
            {boq ? (
              <Link
                href={`/bills-of-quantities/${boq.id}`}
                className={styles.nameLink}
              >
                v{boq.version}
                {boq.reference ? ` · ${boq.reference}` : ""} (
                {formatMoney(boq.totalWithVat)} incl. VAT)
              </Link>
            ) : (
              "—"
            )}
          </dd>
          <dt>Contract number</dt>
          <dd>{contract.contractNumber || "—"}</dd>
          <dt>Status</dt>
          <dd>{CONTRACT_STATUS_LABELS[contract.status]}</dd>
          <dt>Agreed value</dt>
          <dd>{formatMoney(contract.value)}</dd>
          <dt>Signed on</dt>
          <dd>{formatDate(contract.signedOn)}</dd>
          <dt>Start date</dt>
          <dd>{formatDate(contract.startDate)}</dd>
          <dt>Planned end date</dt>
          <dd>{formatDate(contract.plannedEndDate)}</dd>
          <dt>Actual end date</dt>
          <dd>{formatDate(contract.actualEndDate)}</dd>
          <dt>Notes</dt>
          <dd>{contract.notes || "—"}</dd>
          <dt>Awarded</dt>
          <dd>{formatDate(contract.createdAt)}</dd>
        </dl>
        <div className={styles.actions}>
          <Link href={`/contracts/${contract.id}/edit`} className={styles.edit}>
            Edit
          </Link>
          <form action={deleteContract}>
            <input type="hidden" name="id" value={contract.id} />
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
            This contract is {CONTRACT_STATUS_LABELS[contract.status].toLowerCase()} —
            its status is final.
          </p>
        ) : (
          <form action={changeContractStatus} className={styles.form}>
            <input type="hidden" name="id" value={contract.id} />
            <label className={styles.fieldLabel}>
              New status
              <select name="status" defaultValue={targets[0]}>
                {targets.map((s) => (
                  <option key={s} value={s}>
                    {CONTRACT_STATUS_LABELS[s]}
                  </option>
                ))}
              </select>
            </label>
            <span />
            <label className={styles.fieldLabel}>
              Signed on (when moving to Signed)
              <input name="signedOn" type="date" />
            </label>
            <label className={styles.fieldLabel}>
              Actual end date (when moving to Completed)
              <input name="actualEndDate" type="date" />
            </label>
            <button type="submit">Update status</button>
          </form>
        )}
        <p className={styles.muted}>
          Signing records the signed date; completing records the actual end date. A
          completed or terminated contract is closed and can no longer change.
        </p>
      </section>
    </main>
  );
}
