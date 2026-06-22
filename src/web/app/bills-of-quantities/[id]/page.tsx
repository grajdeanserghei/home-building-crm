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
import {
  BOQ_STATUSES,
  BOQ_STATUS_LABELS,
  formatMoney,
  getBillOfQuantities,
  getUnitsOfMeasure,
  type BoqStatus,
} from "@/app/lib/api";
import styles from "@/app/page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

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
  const title = `BoQ v${boq.version}${boq.reference ? ` · ${boq.reference}` : ""}`;

  return (
    <main className={styles.main}>
      <Link href={`/bids/${boq.bidId}`} className={styles.backLink}>
        ← Back to bid
      </Link>
      <h1>{title}</h1>
      <p className={styles.subtitle}>
        Bill of quantities, priced in {boq.pricingCurrency}
        {" · "}
        <span className={`${styles.badge} ${styles[`status${boq.status}`]}`}>
          {BOQ_STATUS_LABELS[boq.status]}
        </span>
        {" · "}
        <strong>{formatMoney(boq.total)}</strong>
      </p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>Version</dt>
          <dd>{boq.version}</dd>
          <dt>Reference</dt>
          <dd>{boq.reference || "—"}</dd>
          <dt>Status</dt>
          <dd>{BOQ_STATUS_LABELS[boq.status]}</dd>
          <dt>Pricing currency</dt>
          <dd>{boq.pricingCurrency}</dd>
          <dt>Pinned rate</dt>
          <dd>
            {boq.exchangeRate
              ? `1 ${boq.exchangeRate.baseCurrency} = ${boq.exchangeRate.rate} ${boq.exchangeRate.quoteCurrency} (as of ${formatDate(
                  boq.exchangeRate.asOf,
                )})`
              : "—"}
          </dd>
          <dt>Submitted on</dt>
          <dd>{formatDate(boq.submittedOn)}</dd>
          <dt>Valid until</dt>
          <dd>{formatDate(boq.validUntil)}</dd>
          <dt>Total</dt>
          <dd>{formatMoney(boq.total)}</dd>
          <dt>Created</dt>
          <dd>{formatDate(boq.createdAt)}</dd>
        </dl>
        <div className={styles.actions}>
          {editable ? (
            <Link
              href={`/bills-of-quantities/${boq.id}/edit`}
              className={styles.edit}
            >
              Edit
            </Link>
          ) : null}
          <form action={deleteBoq}>
            <input type="hidden" name="id" value={boq.id} />
            <input type="hidden" name="bidId" value={boq.bidId} />
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
            This bill of quantities is {BOQ_STATUS_LABELS[boq.status].toLowerCase()} — its
            status is final.
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
            <button type="submit">Update status</button>
          </form>
        )}
        <p className={styles.muted}>
          Accepting a BoQ locks it against further edits — it becomes the basis for a
          contract.
        </p>
      </section>

      {boq.sections.map((section) => (
        <section className={styles.card} key={section.id}>
          <h2>
            {section.sequence}. {section.name}{" "}
            <span className={styles.muted}>· {formatMoney(section.subtotal)}</span>
          </h2>
          {section.description ? (
            <p className={styles.muted}>{section.description}</p>
          ) : null}

          {section.lineItems.length === 0 ? (
            <p>No line items yet.</p>
          ) : (
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Description</th>
                  <th>Qty</th>
                  <th>Unit</th>
                  <th>Unit price</th>
                  <th>Line total</th>
                  {editable ? <th aria-label="actions" /> : null}
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
                    <td>{li.quantity}</td>
                    <td>{unitCode.get(li.unitOfMeasureId) ?? "—"}</td>
                    <td>{formatMoney(li.unitPrice)}</td>
                    <td>{formatMoney(li.lineTotal)}</td>
                    {editable ? (
                      <td>
                        <div className={styles.actions}>
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
                              Remove
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
              <h2 style={{ marginTop: 20 }}>Add line item</h2>
              {activeUnits.length === 0 ? (
                <p className={styles.muted}>
                  No active units of measure — add one under Units of measure first.
                </p>
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
                    Remove section
                  </button>
                </form>
              </div>
            </>
          ) : null}
        </section>
      ))}

      {editable ? (
        <section className={styles.card}>
          <h2>Add section</h2>
          <SectionForm
            action={addSection}
            boqId={boq.id}
            defaultSequence={boq.sections.length + 1}
          />
        </section>
      ) : boq.sections.length === 0 ? (
        <section className={styles.card}>
          <p className={styles.muted}>
            This bill of quantities has no sections and can no longer be edited.
          </p>
        </section>
      ) : null}
    </main>
  );
}
