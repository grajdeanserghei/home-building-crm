import Link from "next/link";
import { notFound } from "next/navigation";
import { LineItemForm } from "@/app/components/LineItemForm";
import { SectionForm } from "@/app/components/SectionForm";
import {
  addLineItem,
  addSection,
  changeBoqStatus,
  deleteBoq,
  removeLineItem,
  removeSection,
} from "@/app/bills-of-quantities/actions";
import { awardContract } from "@/app/contracts/actions";
import {
  BOQ_STATUSES,
  BOQ_STATUS_LABELS,
  CONTRACT_STATUS_LABELS,
  CURRENCIES,
  getBid,
  getBillOfQuantities,
  getContractByWorkPackage,
  getUnitsOfMeasure,
  type BoqStatus,
  type Contract,
} from "@/app/lib/api";
import { formatDate, formatMoney, formatNumber, formatPercent } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A BoQ accepts structural edits (header, sections, line items) only while Draft or
// Submitted; once Accepted/Rejected/Withdrawn it is locked. Mirrors the aggregate.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

// The statuses a BoQ may move to from its current one. Rejected and Withdrawn are
// terminal (closed) — the backend forbids transitioning out of them.
function allowedTargets(current: BoqStatus): BoqStatus[] {
  if (current === "Rejected" || current === "Withdrawn") return [];
  return BOQ_STATUSES.filter((s) => s !== current);
}

export default async function BillOfQuantitiesDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  // All units (incl. retired) for displaying existing lines; only active ones are offered
  // for new lines further down.
  const allUnits = await getUnitsOfMeasure(true);

  // Only active units may be referenced by a new line; all units (incl. retired) are kept
  // for displaying the code of an existing line whose unit was later deactivated.
  const activeUnits = allUnits.filter((u) => u.isActive);
  const unitCode = new Map(allUnits.map((u) => [u.id, u.code]));

  const editable = isEditable(boq.status);
  const targets = allowedTargets(boq.status);
  const title = t("boq.title", { version: boq.version }) +
    (boq.reference ? ` · ${boq.reference}` : "");

  // A contract is awarded from an accepted BoQ. Once this BoQ is accepted, resolve its
  // owning bid (to reach the work package) and any contract already on that work
  // package, so we can either link to the award or offer to create it.
  let awardBid = null;
  let existingContract: Contract | null = null;
  if (boq.status === "Accepted") {
    awardBid = await getBid(boq.bidId);
    if (awardBid) {
      existingContract = await getContractByWorkPackage(awardBid.workPackageId);
    }
  }

  return (
    <main className={styles.main}>
      <Link href={`/bids/${boq.bidId}`} className={styles.backLink}>
        {t("boq.backToBid")}
      </Link>
      <h1>{title}</h1>
      <p className={styles.subtitle}>
        {t("boq.subtitle", { currency: boq.pricingCurrency })}
        {" · "}
        <span className={`${styles.badge} ${styles[`status${boq.status}`]}`}>
          {BOQ_STATUS_LABELS[boq.status]}
        </span>
        {" · "}
        <strong>{formatMoney(boq.totalWithVat)}</strong> {t("boq.inclVat")}
        <span className={styles.muted}> ({formatMoney(boq.total)} {t("boq.exclVat")})</span>
      </p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>{t("boq.version")}</dt>
          <dd>{boq.version}</dd>
          <dt>{t("boq.reference")}</dt>
          <dd>{boq.reference || "—"}</dd>
          <dt>{t("common.status")}</dt>
          <dd>{BOQ_STATUS_LABELS[boq.status]}</dd>
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
          <dt>{t("common.created")}</dt>
          <dd>{formatDate(boq.createdAt)}</dd>
        </dl>
        <div className={styles.actions}>
          {editable ? (
            <Link
              href={`/bills-of-quantities/${boq.id}/edit`}
              className={styles.edit}
            >
              {t("common.edit")}
            </Link>
          ) : null}
          <form action={deleteBoq}>
            <input type="hidden" name="id" value={boq.id} />
            <input type="hidden" name="bidId" value={boq.bidId} />
            <button type="submit" className={styles.delete}>
              {t("common.delete")}
            </button>
          </form>
        </div>
      </section>

      <section className={styles.card}>
        <h2>{t("boq.changeStatus")}</h2>
        {targets.length === 0 ? (
          <p className={styles.muted}>
            {t("boq.statusFinal", {
              status: BOQ_STATUS_LABELS[boq.status].toLowerCase(),
            })}
          </p>
        ) : (
          <form action={changeBoqStatus} className={styles.form}>
            <input type="hidden" name="id" value={boq.id} />
            <input type="hidden" name="bidId" value={boq.bidId} />
            <select name="status" defaultValue={targets[0]}>
              {targets.map((s) => (
                <option key={s} value={s}>
                  {BOQ_STATUS_LABELS[s]}
                </option>
              ))}
            </select>
            <button type="submit">{t("boq.updateStatus")}</button>
          </form>
        )}
        <p className={styles.muted}>{t("boq.acceptNote")}</p>
      </section>

      {boq.status === "Accepted" ? (
        <section className={styles.card}>
          <h2>{t("boq.contract")}</h2>
          {existingContract ? (
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
          ) : (
            <>
              <p className={styles.muted}>{t("boq.awardNote")}</p>
              <form action={awardContract} className={styles.form}>
                <input type="hidden" name="boqId" value={boq.id} />
                <input type="hidden" name="bidId" value={boq.bidId} />
                {awardBid ? (
                  <input
                    type="hidden"
                    name="workPackageId"
                    value={awardBid.workPackageId}
                  />
                ) : null}
                <input
                  name="contractNumber"
                  placeholder={t("boq.contractNumberPlaceholder")}
                />
                <span />
                <label className={styles.fieldLabel}>
                  {t("boq.agreedValueLabel")}
                  <input
                    name="valueAmount"
                    type="number"
                    min={0}
                    step="0.01"
                    placeholder={String(boq.total.amount)}
                  />
                </label>
                <label className={styles.fieldLabel}>
                  {t("boq.currency")}
                  <select
                    name="valueCurrency"
                    defaultValue={boq.pricingCurrency}
                  >
                    {CURRENCIES.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </label>
                <label className={styles.fieldLabel}>
                  {t("boq.startDate")}
                  <input name="startDate" type="date" />
                </label>
                <label className={styles.fieldLabel}>
                  {t("boq.plannedEndDate")}
                  <input name="plannedEndDate" type="date" />
                </label>
                <input name="notes" placeholder={t("boq.notesPlaceholder")} />
                <button type="submit">{t("boq.awardContract")}</button>
              </form>
            </>
          )}
        </section>
      ) : null}

      {boq.sections.map((section) => (
        <section className={styles.card} key={section.id}>
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

          {section.lineItems.length === 0 ? (
            <p>{t("lineItems.empty")}</p>
          ) : (
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>#</th>
                  <th>{t("common.description")}</th>
                  <th>{t("lineItems.col.unit")}</th>
                  <th>{t("lineItems.col.qty")}</th>
                  <th>{t("lineItems.col.unitPriceExclVat")}</th>
                  <th>{t("lineItems.col.vat")}</th>
                  <th>{t("lineItems.col.lineTotalExclVat")}</th>
                  <th>{t("lineItems.col.lineTotalInclVat")}</th>
                  {editable ? <th aria-label={t("common.actions")} /> : null}
                </tr>
              </thead>
              <tbody>
                {section.lineItems.map((li) => (
                  <tr key={li.id}>
                    <td>{li.sequence}</td>
                    <td>
                      <strong>{li.description}</strong>
                      {li.notes ? (
                        <div className={styles.muted}>{li.notes}</div>
                      ) : null}
                    </td>
                    <td>{unitCode.get(li.unitOfMeasureId) ?? "—"}</td>
                    <td>{formatNumber(li.quantity)}</td>
                    <td>{formatMoney(li.unitPrice)}</td>
                    <td>{formatPercent(li.vatRatePercentage)}</td>
                    <td>{formatMoney(li.lineTotal)}</td>
                    <td>{formatMoney(li.lineTotalWithVat)}</td>
                    {editable ? (
                      <td>
                        <div className={styles.actions}>
                          <Link
                            href={`/bills-of-quantities/${boq.id}/sections/${section.id}/line-items/${li.id}/edit`}
                            className={styles.edit}
                          >
                            {t("common.edit")}
                          </Link>
                          <form action={removeLineItem}>
                            <input type="hidden" name="boqId" value={boq.id} />
                            <input
                              type="hidden"
                              name="sectionId"
                              value={section.id}
                            />
                            <input
                              type="hidden"
                              name="lineItemId"
                              value={li.id}
                            />
                            <button type="submit" className={styles.delete}>
                              {t("common.remove")}
                            </button>
                          </form>
                        </div>
                      </td>
                    ) : null}
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {editable ? (
            <>
              <h2 style={{ marginTop: 20 }}>{t("lineItems.add")}</h2>
              {activeUnits.length === 0 ? (
                <p className={styles.muted}>{t("lineItems.noActiveUnits")}</p>
              ) : (
                <LineItemForm
                  action={addLineItem}
                  boqId={boq.id}
                  sectionId={section.id}
                  currency={boq.pricingCurrency}
                  units={activeUnits}
                  defaultSequence={section.lineItems.length + 1}
                />
              )}
              <div className={styles.actions}>
                <form action={removeSection}>
                  <input type="hidden" name="boqId" value={boq.id} />
                  <input type="hidden" name="sectionId" value={section.id} />
                  <button type="submit" className={styles.delete}>
                    {t("sections.remove")}
                  </button>
                </form>
              </div>
            </>
          ) : null}
        </section>
      ))}

      {editable ? (
        <section className={styles.card}>
          <h2>{t("sections.add")}</h2>
          <SectionForm
            action={addSection}
            boqId={boq.id}
            defaultSequence={boq.sections.length + 1}
          />
        </section>
      ) : boq.sections.length === 0 ? (
        <section className={styles.card}>
          <p className={styles.muted}>{t("boq.noSectionsLocked")}</p>
        </section>
      ) : null}
    </main>
  );
}
