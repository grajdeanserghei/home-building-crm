import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { changeBoqStatus } from "@/app/bills-of-quantities/actions";
import {
  BOQ_STATUSES,
  BOQ_STATUS_LABELS,
  getBillOfQuantities,
  type BoqStatus,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// The statuses a BoQ may move to from its current one. Rejected and Withdrawn are
// terminal (closed) — the backend forbids transitioning out of them.
function allowedTargets(current: BoqStatus): BoqStatus[] {
  if (current === "Rejected" || current === "Withdrawn") return [];
  return BOQ_STATUSES.filter((s) => s !== current);
}

export default async function ChangeBoqStatusPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  const targets = allowedTargets(boq.status);

  // A terminal BoQ has no onward transitions — nothing to change here.
  if (targets.length === 0) {
    redirect(`/bills-of-quantities/${id}`);
  }

  return (
    <main className={styles.main}>
      <Link href={`/bills-of-quantities/${boq.id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("boq.changeStatus")}</h1>
      <p className={styles.subtitle}>{t("boq.changeStatusSubtitle")}</p>

      <section className={styles.card}>
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
        <p className={styles.muted}>{t("boq.acceptNote")}</p>
      </section>
    </main>
  );
}
