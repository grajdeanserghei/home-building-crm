import Link from "next/link";
import { notFound } from "next/navigation";
import { deleteBid, removeBidNote } from "@/app/bids/actions";
import {
  BID_STATUS_LABELS,
  BID_STATUSES,
  BOQ_STATUS_LABELS,
  BUDGET_SCOPE_KIND_LABELS,
  budgetMultiplier,
  effectiveMoney,
  NOTE_TYPE_LABELS,
  getBid,
  getBidBoq,
  getContractor,
  getProject,
  getWorkPackage,
  type BidStatus,
} from "@/app/lib/api";
import { formatDate, formatMoney } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Whether the bid has any onward transitions. A Withdrawn bid is terminal, so the
// change-status action is hidden; the targets themselves are computed on the status route.
function canChangeStatus(current: BidStatus): boolean {
  return current !== "Withdrawn" && BID_STATUSES.length > 0;
}

// Read-first detail page: it shows the bid, its priced BoQ and its discussion log, with no
// inline create/edit forms. Every mutation (status change, edit, drafting a BoQ, logging a
// note) is a deliberate step away on its own route.
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

  // A per-apartment BoQ's cost for the whole build needs the project's apartment count as the
  // multiplier; the work package (already loaded) points at the owning project.
  let apartmentUnits = 1;
  if (boq?.budgetScopeKind === "PerApartment" && workPackage) {
    const project = await getProject(workPackage.projectId);
    apartmentUnits = project?.apartmentUnits ?? 1;
  }

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

      <div className={styles.toolbar}>
        <div>
          <h1>
            {contractor ? (
              <Link
                href={`/contractors/${contractor.id}`}
                className={styles.nameLink}
              >
                {contractorName}
              </Link>
            ) : (
              contractorName
            )}
          </h1>
          {contractor?.reference ? (
            <p className={styles.muted}>{contractor.reference}</p>
          ) : null}
          <p className={styles.subtitle}>
            {t("bids.bidOn", {
              name: workPackage?.name ?? t("bids.thisWorkPackage"),
            })}
            {" · "}
            <span className={`${styles.badge} ${styles[`status${bid.status}`]}`}>
              {BID_STATUS_LABELS[bid.status]}
            </span>
          </p>
        </div>
        <Link href={`/bids/${bid.id}/notes/new`} className={styles.primaryButton}>
          {t("notes.logHeading")}
        </Link>
      </div>

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
          {canChangeStatus(bid.status) ? (
            <Link href={`/bids/${bid.id}/status`} className={styles.edit}>
              {t("bids.changeStatus")}
            </Link>
          ) : null}
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
        <h2>{t("bids.boqHeading")}</h2>
        {!boq ? (
          <>
            <p className={styles.muted}>{t("bids.boqEmpty")}</p>
            <div className={styles.actions}>
              <Link href={`/bids/${bid.id}/boq/new`} className={styles.edit}>
                {t("bids.draftBoqSubmit")}
              </Link>
            </div>
          </>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>{t("bids.boqCol.reference")}</th>
                  <th>{t("common.status")}</th>
                  <th>{t("boq.budgetScope")}</th>
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
                  <td>{BUDGET_SCOPE_KIND_LABELS[boq.budgetScopeKind]}</td>
                  <td>
                    {formatMoney(
                      effectiveMoney(
                        boq.totalWithVat,
                        boq.budgetScopeKind,
                        apartmentUnits,
                      ),
                    )}
                    {budgetMultiplier(boq.budgetScopeKind, apartmentUnits) > 1 ? (
                      <span className={styles.muted}>
                        {" "}
                        {t("boq.perApartmentNote", {
                          base: formatMoney(boq.totalWithVat),
                          count: String(apartmentUnits),
                        })}
                      </span>
                    ) : null}
                  </td>
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
          </div>
        )}
      </section>

      <section className={styles.card}>
        <h2>{t("notes.discussionLog")}</h2>
        {bid.notes.length === 0 ? (
          <>
            <p className={styles.muted}>{t("notes.empty")}</p>
            <div className={styles.actions}>
              <Link
                href={`/bids/${bid.id}/notes/new`}
                className={styles.edit}
              >
                {t("notes.logHeading")}
              </Link>
            </div>
          </>
        ) : (
          <div className={styles.tableWrap}>
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
          </div>
        )}
      </section>
    </main>
  );
}
