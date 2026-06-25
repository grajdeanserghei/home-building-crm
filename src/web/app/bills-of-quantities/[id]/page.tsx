import Link from "next/link";
import { notFound } from "next/navigation";
import { BoqDndBoard } from "@/app/components/BoqDndBoard";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { LineItemsTable } from "@/app/components/LineItemsTable";
import {
  deleteBoq,
  duplicateLineItem,
  removeLineItem,
  removeSection,
  removeSubsection,
  removeSubsectionLineItem,
} from "@/app/bills-of-quantities/actions";
import {
  BOQ_STATUS_LABELS,
  BUDGET_SCOPE_KIND_LABELS,
  budgetMultiplier,
  CONTRACT_STATUS_LABELS,
  effectiveMoney,
  getBid,
  getBillOfQuantities,
  getContractByWorkPackage,
  getProject,
  getUnitsOfMeasure,
  getWorkPackage,
  type BoqStatus,
  type Contract,
} from "@/app/lib/api";
import { formatDate, formatMoney, formatNumber } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A BoQ accepts structural edits (header, sections, line items) only while Draft or
// Submitted; once Accepted/Rejected/Withdrawn it is locked. Mirrors the aggregate.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

// Rejected and Withdrawn are terminal — the backend forbids transitioning out of them, so
// the status-change action is only offered from a non-terminal state.
function canChangeStatus(status: BoqStatus): boolean {
  return status !== "Rejected" && status !== "Withdrawn";
}

// Read-first detail page: it shows the bill and its priced structure, with no inline
// create/edit forms. Every mutation (status change, award, adding/editing/removing
// sections, subsections and lines) is a deliberate step away on its own route.
export default async function BillOfQuantitiesDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ arrange?: string }>;
}) {
  const { id } = await params;
  const { arrange } = await searchParams;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  // All units (incl. retired) so a line whose unit was later deactivated still renders its
  // code. Active-only filtering for new lines lives on the add-line route.
  const allUnits = await getUnitsOfMeasure(true);
  const unitCode = new Map(allUnits.map((u) => [u.id, u.code]));

  const editable = isEditable(boq.status);
  // Arrange mode: a deliberate, editable-only drag-and-drop view for reordering and moving lines.
  // Gated behind ?arrange=1 so the detail page stays read-first by default.
  const arranging = editable && arrange === "1" && boq.sections.length > 0;
  const title = t("boq.title") + (boq.reference ? ` · ${boq.reference}` : "");

  // A contract is awarded from an accepted BoQ. Resolve any contract already on the owning
  // work package so we can either link to it (read) or offer the award action.
  let existingContract: Contract | null = null;
  if (boq.status === "Accepted") {
    const awardBid = await getBid(boq.bidId);
    if (awardBid) {
      existingContract = await getContractByWorkPackage(awardBid.workPackageId);
    }
  }
  const canAward = boq.status === "Accepted" && !existingContract;

  // What the supplier priced against, and the cost scaled to the whole build. For a per-apartment
  // quote we need the project's apartment count (reached via the bid's work package) as the multiplier.
  let apartmentUnits = 1;
  if (boq.budgetScopeKind === "PerApartment") {
    const scopeBid = await getBid(boq.bidId);
    const scopeWp = scopeBid ? await getWorkPackage(scopeBid.workPackageId) : null;
    const scopeProject = scopeWp ? await getProject(scopeWp.projectId) : null;
    apartmentUnits = scopeProject?.apartmentUnits ?? 1;
  }
  const multiplier = budgetMultiplier(boq.budgetScopeKind, apartmentUnits);
  const effectiveTotal = effectiveMoney(boq.total, boq.budgetScopeKind, apartmentUnits);
  const effectiveTotalWithVat = effectiveMoney(
    boq.totalWithVat,
    boq.budgetScopeKind,
    apartmentUnits,
  );

  return (
    <main className={styles.main}>
      <Link href={`/bids/${boq.bidId}`} className={styles.backLink}>
        {t("boq.backToBid")}
      </Link>

      <div className={styles.toolbar}>
        <div>
          <h1>{title}</h1>
          <p className={styles.subtitle}>
            {t("boq.subtitle", { currency: boq.pricingCurrency })}
            {" · "}
            <span className={`${styles.badge} ${styles[`status${boq.status}`]}`}>
              {BOQ_STATUS_LABELS[boq.status]}
            </span>
            {" · "}
            {t("boq.scopePrefix")}{" "}
            <strong>{BUDGET_SCOPE_KIND_LABELS[boq.budgetScopeKind]}</strong>
            {" · "}
            <strong>{formatMoney(effectiveTotalWithVat)}</strong> {t("boq.inclVat")}
            <span className={styles.muted}>
              {" "}
              ({formatMoney(effectiveTotal)} {t("boq.exclVat")})
            </span>
            {multiplier > 1 ? (
              <span className={styles.muted}>
                {" "}
                {t("boq.perApartmentNote", {
                  base: formatMoney(boq.totalWithVat),
                  count: String(apartmentUnits),
                })}
              </span>
            ) : null}
          </p>
        </div>
        {editable ? (
          <div className={styles.actions}>
            {arranging ? (
              <Link
                href={`/bills-of-quantities/${boq.id}`}
                className={styles.primaryButton}
              >
                {t("boq.arrangeDone")}
              </Link>
            ) : (
              <>
                {boq.sections.length > 0 ? (
                  <Link
                    href={`/bills-of-quantities/${boq.id}?arrange=1`}
                    className={styles.edit}
                  >
                    {t("boq.arrange")}
                  </Link>
                ) : null}
                <Link
                  href={`/bills-of-quantities/${boq.id}/sections/new`}
                  className={styles.primaryButton}
                >
                  {t("sections.add")}
                </Link>
              </>
            )}
          </div>
        ) : null}
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
          <dd>{formatMoney(boq.total)}</dd>
          <dt>{t("boq.totalInclVat")}</dt>
          <dd>{formatMoney(boq.totalWithVat)}</dd>
          {multiplier > 1 ? (
            <>
              <dt>{t("boq.buildTotalExclVat", { count: String(apartmentUnits) })}</dt>
              <dd>{formatMoney(effectiveTotal)}</dd>
              <dt>{t("boq.buildTotalInclVat", { count: String(apartmentUnits) })}</dt>
              <dd>{formatMoney(effectiveTotalWithVat)}</dd>
            </>
          ) : null}
          <dt>{t("common.created")}</dt>
          <dd>{formatDate(boq.createdAt)}</dd>
        </dl>
        <div className={styles.actions}>
          <a
            href={`/bills-of-quantities/${boq.id}/export`}
            className={styles.edit}
          >
            {t("boq.exportExcel")}
          </a>
          {canChangeStatus(boq.status) ? (
            <Link
              href={`/bills-of-quantities/${boq.id}/status`}
              className={styles.edit}
            >
              {t("boq.changeStatus")}
            </Link>
          ) : null}
          {canAward ? (
            <Link
              href={`/bills-of-quantities/${boq.id}/award`}
              className={styles.edit}
            >
              {t("boq.awardContract")}
            </Link>
          ) : null}
          {editable ? (
            <Link
              href={`/bills-of-quantities/${boq.id}/edit`}
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
            boqId={boq.id}
            sections={boq.sections}
            unitCode={Object.fromEntries(unitCode)}
          />
        </section>
      ) : null}

      {!arranging &&
        boq.sections.map((section) => (
        <section className={`${styles.card} ${styles.boqSection}`} key={section.id}>
          <h2>
            {section.sequence}. {section.name}{" "}
            <span className={styles.muted}>
              · {formatMoney(section.subtotalWithVat)} {t("boq.inclVat")} (
              {formatMoney(section.subtotal)} {t("boq.exclShort")})
            </span>
          </h2>
          {section.description ? (
            <p className={styles.muted}>{section.description}</p>
          ) : null}

          <LineItemsTable
            lineItems={section.lineItems}
            unitCode={unitCode}
            editable={editable}
            boqId={boq.id}
            sectionId={section.id}
            editHrefBase={`/bills-of-quantities/${boq.id}/sections/${section.id}/line-items`}
            removeAction={removeLineItem}
            duplicateAction={duplicateLineItem}
          />

          {/* Subsections: an optional second level of grouping within the section. */}
          {section.subsections.map((subsection) => (
            <div className={styles.subsection} key={subsection.id}>
              <h3>
                {section.sequence}.{subsection.sequence} {subsection.name}{" "}
                <span className={styles.muted}>
                  · {formatMoney(subsection.subtotalWithVat)} {t("boq.inclVat")} (
                  {formatMoney(subsection.subtotal)} {t("boq.exclShort")})
                </span>
              </h3>
              {subsection.description ? (
                <p className={styles.muted}>{subsection.description}</p>
              ) : null}

              <LineItemsTable
                lineItems={subsection.lineItems}
                unitCode={unitCode}
                editable={editable}
                boqId={boq.id}
                sectionId={section.id}
                subsectionId={subsection.id}
                editHrefBase={`/bills-of-quantities/${boq.id}/sections/${section.id}/subsections/${subsection.id}/line-items`}
                removeAction={removeSubsectionLineItem}
                duplicateAction={duplicateLineItem}
              />

              {editable ? (
                <div className={styles.actions}>
                  <Link
                    href={`/bills-of-quantities/${boq.id}/sections/${section.id}/subsections/${subsection.id}/line-items/new`}
                    className={styles.edit}
                  >
                    {t("subsections.addLine")}
                  </Link>
                  <Link
                    href={`/bills-of-quantities/${boq.id}/sections/${section.id}/subsections/${subsection.id}/edit`}
                    className={styles.edit}
                  >
                    {t("subsections.edit")}
                  </Link>
                  <ConfirmDeleteButton
                    action={removeSubsection}
                    fields={{
                      boqId: boq.id,
                      sectionId: section.id,
                      subsectionId: subsection.id,
                    }}
                    title={t("subsections.removeTitle")}
                    bodyTemplate={t("subsections.removeBody")}
                    name={subsection.name}
                    triggerLabel={t("subsections.remove")}
                    confirmLabel={t("subsections.remove")}
                  />
                </div>
              ) : null}
            </div>
          ))}

          {editable ? (
            <div className={styles.actions} style={{ marginTop: 16 }}>
              <Link
                href={`/bills-of-quantities/${boq.id}/sections/${section.id}/line-items/new`}
                className={styles.edit}
              >
                {t("lineItems.add")}
              </Link>
              <Link
                href={`/bills-of-quantities/${boq.id}/sections/${section.id}/subsections/new`}
                className={styles.edit}
              >
                {t("subsections.add")}
              </Link>
              <Link
                href={`/bills-of-quantities/${boq.id}/sections/${section.id}/edit`}
                className={styles.edit}
              >
                {t("sections.edit")}
              </Link>
              <ConfirmDeleteButton
                action={removeSection}
                fields={{ boqId: boq.id, sectionId: section.id }}
                title={t("sections.removeTitle")}
                bodyTemplate={t("sections.removeBody")}
                name={section.name}
                triggerLabel={t("sections.remove")}
                confirmLabel={t("sections.remove")}
              />
            </div>
          ) : null}
        </section>
      ))}

      {boq.sections.length === 0 ? (
        <section className={styles.card}>
          <p className={styles.muted}>
            {editable ? t("boq.noSectionsYet") : t("boq.noSectionsLocked")}
          </p>
        </section>
      ) : null}
    </main>
  );
}
