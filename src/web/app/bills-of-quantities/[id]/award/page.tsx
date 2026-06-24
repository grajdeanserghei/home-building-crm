import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { awardContract } from "@/app/contracts/actions";
import {
  CURRENCIES,
  getBid,
  getBillOfQuantities,
  getContractByWorkPackage,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A contract is awarded from an accepted BoQ, and only once. This route renders the award
// form; the detail page links here only while those conditions hold, but we re-check them
// (and bounce back) so a stale link or direct navigation can't reach an invalid award.
export default async function AwardContractPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  if (boq.status !== "Accepted") {
    redirect(`/bills-of-quantities/${id}`);
  }

  // Resolve the owning bid (to reach the work package) and any contract already on it.
  const awardBid = await getBid(boq.bidId);
  const existingContract = awardBid
    ? await getContractByWorkPackage(awardBid.workPackageId)
    : null;

  if (existingContract) {
    redirect(`/bills-of-quantities/${id}`);
  }

  return (
    <main className={styles.main}>
      <Link href={`/bills-of-quantities/${boq.id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("boq.awardTitle")}</h1>
      <p className={styles.subtitle}>{t("boq.awardNote")}</p>

      <section className={styles.card}>
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
            <select name="valueCurrency" defaultValue={boq.pricingCurrency}>
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
      </section>
    </main>
  );
}
