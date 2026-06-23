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
  getBid,
  getBidBoq,
  getContractor,
  getWorkPackage,
  type BidStatus,
} from "@/app/lib/api";
import { formatDate, formatMoney } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

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

  const [contractor, workPackage, boq] = await Promise.all([
    getContractor(bid.contractorId),
    getWorkPackage(bid.workPackageId),
    getBidBoq(bid.id),
  ]);

  const contractorName = contractor?.name ?? t("bids.unknownContractor");
  const targets = allowedTargets(bid.status);
  // `new Date()` here runs server-side at request time; the picker seed is just a default.
  const today = new Date().toISOString().slice(0, 10);

  return (
    <main className={styles.main}>
      <Link
        href={`/work-packages/${bid.workPackageId}`}
        className={styles.backLink}
      >
        {t("bids.backTo", {
          name: workPackage?.name ?? t("bids.workPackageFallback"),
        })}
      </Link>
      <h1>{contractorName}</h1>
      <p className={styles.subtitle}>
        {t("bids.bidOn", {
          name: workPackage?.name ?? t("bids.thisWorkPackage"),
        })}
        {" · "}
        <span className={`${styles.badge} ${styles[`status${bid.status}`]}`}>
          {BID_STATUS_LABELS[bid.status]}
        </span>
      </p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>{t("common.status")}</dt>
          <dd>{BID_STATUS_LABELS[bid.status]}</dd>
          <dt>{t("bids.firstContacted")}</dt>
          <dd>{formatDate(bid.firstContactedOn)}</dd>
          <dt>{t("bids.summary")}</dt>
          <dd>{bid.summary || "—"}</dd>
          <dt>{t("bids.opened")}</dt>
          <dd>{formatDate(bid.createdAt)}</dd>
        </dl>
        <div className={styles.actions}>
          <Link href={`/bids/${bid.id}/edit`} className={styles.edit}>
            {t("common.edit")}
          </Link>
          <form action={deleteBid}>
            <input type="hidden" name="id" value={bid.id} />
            <input
              type="hidden"
              name="workPackageId"
              value={bid.workPackageId}
            />
            <button type="submit" className={styles.delete}>
              {t("common.delete")}
            </button>
          </form>
        </div>
      </section>

      <section className={styles.card}>
        <h2>{t("bids.changeStatus")}</h2>
        {targets.length === 0 ? (
          <p className={styles.muted}>{t("bids.withdrawnFinal")}</p>
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
            <button type="submit">{t("bids.updateStatus")}</button>
          </form>
        )}
        <p className={styles.muted}>{t("bids.selectWinnerNote")}</p>
      </section>

      <section className={styles.card}>
        <h2>{t("bids.boqHeading")}</h2>
        {!boq ? (
          <p>{t("bids.boqEmpty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("bids.boqCol.reference")}</th>
                <th>{t("common.status")}</th>
                <th>{t("bids.boqCol.totalWithVat")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              <tr>
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
                      {t("bids.view")}
                    </Link>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        )}
      </section>

      {!boq && (
        <section className={styles.card}>
          <h2>{t("bids.draftBoqHeading")}</h2>
          <BillOfQuantitiesForm
            action={draftBoq}
            bidId={bid.id}
            submitLabel={t("bids.draftBoqSubmit")}
          />
        </section>
      )}

      <section className={styles.card}>
        <h2>{t("notes.logHeading")}</h2>
        <BidNoteForm action={logBidNote} bidId={bid.id} today={today} />
      </section>

      <section className={styles.card}>
        <h2>{t("notes.discussionLog")}</h2>
        {bid.notes.length === 0 ? (
          <p>{t("notes.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("notes.col.when")}</th>
                <th>{t("notes.col.type")}</th>
                <th>{t("notes.col.note")}</th>
                <th aria-label={t("common.actions")} />
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
                          {t("common.remove")}
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
