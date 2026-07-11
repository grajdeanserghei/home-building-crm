import Link from "next/link";
import { notFound } from "next/navigation";
import { deleteBid, duplicateBid, removeBidNote } from "@/app/bids/actions";
import { deleteBoq } from "@/app/bills-of-quantities/actions";
import { BoqDndBoard } from "@/app/components/BoqDndBoard";
import { BoqSections } from "@/app/components/BoqSections";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { SubmitButton } from "@/app/components/SubmitButton";
import {
  BID_STATUS_LABELS,
  BID_STATUSES,
  BOQ_STATUS_LABELS,
  BUDGET_SCOPE_KIND_LABELS,
  budgetMultiplier,
  CONTRACT_STATUS_LABELS,
  effectiveMoney,
  NOTE_TYPE_LABELS,
  getBid,
  getBidBoq,
  getContractByWorkPackage,
  getContractor,
  getProject,
  getUnitsOfMeasure,
  getWorkPackage,
  type BidStatus,
  type BoqStatus,
  type Contract,
  type Money,
} from "@/app/lib/api";
import { getDisplayCurrency } from "@/app/lib/display-currency";
import { displayMoney, formatDate, formatNumber } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Whether the bid has any onward transitions. A Withdrawn bid is terminal, so the
// change-status action is hidden; the targets themselves are computed on the status route.
function canChangeStatus(current: BidStatus): boolean {
  return current !== "Withdrawn" && BID_STATUSES.length > 0;
}

// A BoQ accepts structural edits (header, sections, line items) only while Draft or
// Submitted; once Accepted/Rejected/Withdrawn it is locked. Mirrors the aggregate.
function isBoqEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

// Rejected and Withdrawn are terminal for a BoQ — the backend forbids transitioning out of
// them, so the status-change action is only offered from a non-terminal state.
function canChangeBoqStatus(status: BoqStatus): boolean {
  return status !== "Rejected" && status !== "Withdrawn";
}

// The bid detail page. It leads with the bid's priced BoQ (the thing readers usually come for),
// then the bid's own metadata and discussion log below. The BoQ's structural edits — sections,
// line items, status, award, export — are deliberate steps away on the /bids/[id]/boq/… routes;
// line-item add/edit open as modals over this page via the @modal slot in the layout.
export default async function BidDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ arrange?: string }>;
}) {
  const { id } = await params;
  const { arrange } = await searchParams;
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

  // The BoQ section is assembled here so its awaits (units, any awarded contract) and derived
  // figures stay out of the JSX. Null when the bid has no BoQ yet (see the empty state below).
  let boqSection: React.ReactNode = null;
  if (boq) {
    // The BoQ renders in the global display currency (the header toggle). "Original" shows the BoQ's
    // own pricing currency (with decimals); RON/EUR convert every figure via the BoQ's own rate
    // (`boq.ronPerEur` — the pinned rate when it has one, else the app-wide rate) and drop decimals.
    // `money` converts + formats; a no-op when already in the display currency.
    const displayCurrency = await getDisplayCurrency();
    const converting =
      displayCurrency !== "Original" && boq.pricingCurrency !== displayCurrency;
    const money = (m: Money | null | undefined) =>
      displayMoney(m, displayCurrency, boq.ronPerEur);

    // All units (incl. retired) so a line whose unit was later deactivated still renders its code.
    const allUnits = await getUnitsOfMeasure(true);
    const unitCode = new Map(allUnits.map((u) => [u.id, u.code]));

    const editable = isBoqEditable(boq.status);
    // Arrange mode: a deliberate, editable-only drag-and-drop view for reordering and moving lines.
    // Gated behind ?arrange=1 so the page stays read-first by default.
    const arranging = editable && arrange === "1" && boq.sections.length > 0;
    const title = t("boq.title") + (boq.reference ? ` · ${boq.reference}` : "");

    // A contract is awarded from an accepted BoQ. Resolve any contract already on the owning work
    // package so we can either link to it (read) or offer the award action.
    let existingContract: Contract | null = null;
    if (boq.status === "Accepted") {
      existingContract = await getContractByWorkPackage(bid.workPackageId);
    }
    const canAward = boq.status === "Accepted" && !existingContract;

    const multiplier = budgetMultiplier(boq.budgetScopeKind, apartmentUnits);
    const effectiveTotal = effectiveMoney(
      boq.total,
      boq.budgetScopeKind,
      apartmentUnits,
    );
    const effectiveTotalWithVat = effectiveMoney(
      boq.totalWithVat,
      boq.budgetScopeKind,
      apartmentUnits,
    );

    boqSection = (
      <>
        <div
          className={`${styles.toolbar}${arranging ? ` ${styles.stickyToolbar}` : ""}`}
        >
          <div>
            <h2>{title}</h2>
            <p className={styles.subtitle}>
              {t("boq.subtitle", { currency: boq.pricingCurrency })}
              {" · "}
              <span
                className={`${styles.badge} ${styles[`status${boq.status}`]}`}
              >
                {BOQ_STATUS_LABELS[boq.status]}
              </span>
              {" · "}
              {t("boq.scopePrefix")}{" "}
              <strong>{BUDGET_SCOPE_KIND_LABELS[boq.budgetScopeKind]}</strong>
              {" · "}
              <strong>{money(effectiveTotalWithVat)}</strong> {t("boq.inclVat")}
              <span className={styles.muted}>
                {" "}
                ({money(effectiveTotal)} {t("boq.exclVat")})
              </span>
              {multiplier > 1 ? (
                <span className={styles.muted}>
                  {" "}
                  {t("boq.perApartmentNote", {
                    base: money(boq.totalWithVat),
                    count: String(apartmentUnits),
                  })}
                </span>
              ) : null}
              {converting ? (
                <span className={styles.muted}>
                  {" · "}
                  {t("boq.rateNote", { rate: formatNumber(boq.ronPerEur) })}
                </span>
              ) : null}
            </p>
          </div>
          <div style={{ display: "flex", alignItems: "flex-start", gap: 16 }}>
            {editable ? (
              <div className={styles.actions}>
                {arranging ? (
                  <Link
                    href={`/bids/${bid.id}`}
                    className={styles.primaryButton}
                  >
                    {t("boq.arrangeDone")}
                  </Link>
                ) : (
                  <>
                    {boq.sections.length > 0 ? (
                      <Link
                        href={`/bids/${bid.id}?arrange=1`}
                        className={styles.edit}
                      >
                        {t("boq.arrange")}
                      </Link>
                    ) : null}
                    <Link
                      href={`/bids/${bid.id}/boq/sections/new`}
                      className={styles.primaryButton}
                    >
                      {t("sections.add")}
                    </Link>
                  </>
                )}
              </div>
            ) : null}
          </div>
        </div>

        <section className={styles.card}>
          <dl className={styles.detailList}>
            <dt>{t("boq.reference")}</dt>
            <dd>{boq.reference || "—"}</dd>
            <dt>{t("common.status")}</dt>
            <dd>{BOQ_STATUS_LABELS[boq.status]}</dd>
            <dt>{t("boq.budgetScope")}</dt>
            <dd>{BUDGET_SCOPE_KIND_LABELS[boq.budgetScopeKind]}</dd>
            <dt>{t("boq.pricingCurrency")}</dt>
            <dd>{boq.pricingCurrency}</dd>
            <dt>{t("boq.pinnedRate")}</dt>
            <dd>
              {boq.exchangeRate
                ? t("boq.pinnedRateValue", {
                    base: boq.exchangeRate.baseCurrency,
                    rate: formatNumber(boq.exchangeRate.rate),
                    quote: boq.exchangeRate.quoteCurrency,
                    asOf: formatDate(boq.exchangeRate.asOf),
                  })
                : "—"}
            </dd>
            <dt>{t("boq.submittedOn")}</dt>
            <dd>{formatDate(boq.submittedOn)}</dd>
            <dt>{t("boq.validUntil")}</dt>
            <dd>{formatDate(boq.validUntil)}</dd>
            <dt>{t("boq.totalExclVat")}</dt>
            <dd>{money(boq.total)}</dd>
            <dt>{t("boq.totalInclVat")}</dt>
            <dd>{money(boq.totalWithVat)}</dd>
            {multiplier > 1 ? (
              <>
                <dt>
                  {t("boq.buildTotalExclVat", { count: String(apartmentUnits) })}
                </dt>
                <dd>{money(effectiveTotal)}</dd>
                <dt>
                  {t("boq.buildTotalInclVat", { count: String(apartmentUnits) })}
                </dt>
                <dd>{money(effectiveTotalWithVat)}</dd>
              </>
            ) : null}
            <dt>{t("common.created")}</dt>
            <dd>{formatDate(boq.createdAt)}</dd>
          </dl>
          <div className={styles.actions}>
            <a
              href={`/bids/${bid.id}/boq/export`}
              className={styles.edit}
            >
              {t("boq.exportExcel")}
            </a>
            {canChangeBoqStatus(boq.status) ? (
              <Link
                href={`/bids/${bid.id}/boq/status`}
                className={styles.edit}
              >
                {t("boq.changeStatus")}
              </Link>
            ) : null}
            {canAward ? (
              <Link
                href={`/bids/${bid.id}/boq/award`}
                className={styles.edit}
              >
                {t("boq.awardContract")}
              </Link>
            ) : null}
            {editable ? (
              <Link
                href={`/bids/${bid.id}/boq/edit`}
                className={styles.edit}
              >
                {t("common.edit")}
              </Link>
            ) : null}
            <ConfirmDeleteButton
              action={deleteBoq}
              fields={{ id: boq.id, bidId: boq.bidId }}
              title={t("boq.deleteTitle")}
              bodyTemplate={t("boq.deleteBody")}
              name={boq.reference || t("boq.title")}
            />
          </div>
        </section>

        {boq.status === "Accepted" && existingContract ? (
          <section className={styles.card}>
            <h2>{t("boq.contract")}</h2>
            <p>
              {t("boq.underContractBefore")}
              <span
                className={`${styles.badge} ${styles[`status${existingContract.status}`]}`}
              >
                {CONTRACT_STATUS_LABELS[existingContract.status]}
              </span>
              {t("boq.underContractAfter")}{" "}
              <Link
                href={`/contracts/${existingContract.id}`}
                className={styles.nameLink}
              >
                {t("boq.viewContract")}
              </Link>
            </p>
          </section>
        ) : null}

        {arranging ? (
          <section className={styles.card}>
            <BoqDndBoard
              bidId={bid.id}
              boqId={boq.id}
              sections={boq.sections}
              unitCode={Object.fromEntries(unitCode)}
            />
          </section>
        ) : null}

        {!arranging && boq.sections.length > 0 ? (
          <BoqSections
            bidId={bid.id}
            boqId={boq.id}
            sections={boq.sections}
            unitCode={Object.fromEntries(unitCode)}
            editable={editable}
            displayCurrency={displayCurrency}
            ronPerEur={boq.ronPerEur}
          />
        ) : null}

        {boq.sections.length === 0 ? (
          <section className={styles.card}>
            <p className={styles.muted}>
              {editable ? t("boq.noSectionsYet") : t("boq.noSectionsLocked")}
            </p>
          </section>
        ) : null}
      </>
    );
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
            {bid.label ? (
              <>
                {" · "}
                <strong>{bid.label}</strong>
              </>
            ) : null}
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

      {/* The priced BoQ leads — or an invitation to draft one when the bid has none yet. The BoQ
          cluster is wrapped so it reads as one grouped unit, distinct from the bid cards below. */}
      {boq ? (
        <div className={styles.boqGroup}>{boqSection}</div>
      ) : (
        <section className={styles.card}>
          <h2>{t("bids.boqHeading")}</h2>
          <p className={styles.muted}>{t("bids.boqEmpty")}</p>
          <div className={styles.actions}>
            <Link href={`/bids/${bid.id}/boq/new`} className={styles.edit}>
              {t("bids.draftBoqSubmit")}
            </Link>
          </div>
        </section>
      )}

      <section className={styles.card}>
        <h2>{t("bids.detailsHeading")}</h2>
        <dl className={styles.detailList}>
          <dt>{t("common.status")}</dt>
          <dd>{BID_STATUS_LABELS[bid.status]}</dd>
          <dt>{t("bids.label")}</dt>
          <dd>{bid.label || "—"}</dd>
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
          <form action={duplicateBid}>
            <input type="hidden" name="id" value={bid.id} />
            <SubmitButton
              label={t("bids.duplicate")}
              pendingLabel={t("bids.duplicating")}
              className={styles.edit}
            />
          </form>
          <ConfirmDeleteButton
            action={deleteBid}
            fields={{ id: bid.id, workPackageId: bid.workPackageId }}
            title={t("bids.deleteTitle")}
            bodyTemplate={t("bids.deleteBody")}
            name={contractorName}
          />
        </div>
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
                    <td className={styles.multilineCell}>{n.content}</td>
                    <td>
                      <div className={styles.actions}>
                        <ConfirmDeleteButton
                          action={removeBidNote}
                          fields={{ bidId: bid.id, noteId: n.id }}
                          title={t("notes.removeTitle")}
                          bodyTemplate={t("notes.removeBody")}
                          name={NOTE_TYPE_LABELS[n.type]}
                          triggerLabel={t("common.remove")}
                          confirmLabel={t("common.remove")}
                        />
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
