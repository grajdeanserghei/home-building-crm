import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { changeBidStatus } from "@/app/bids/actions";
import {
  BID_STATUS_LABELS,
  BID_STATUSES,
  getBid,
  type BidStatus,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// The statuses a bid may move to from its current one. A Withdrawn bid is terminal, so
// it has no targets; Selected is unavailable from Rejected (the backend forbids it).
function allowedTargets(current: BidStatus): BidStatus[] {
  if (current === "Withdrawn") return [];
  return BID_STATUSES.filter(
    (s) => s !== current && !(current === "Rejected" && s === "Selected"),
  );
}

export default async function ChangeBidStatusPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const bid = await getBid(id);

  if (!bid) {
    notFound();
  }

  const targets = allowedTargets(bid.status);

  // A terminal bid has no onward transitions — nothing to change here.
  if (targets.length === 0) {
    redirect(`/bids/${id}`);
  }

  return (
    <main className={styles.main}>
      <Link href={`/bids/${bid.id}`} className={styles.backLink}>
        {t("bids.backToBid")}
      </Link>
      <h1>{t("bids.changeStatus")}</h1>
      <p className={styles.subtitle}>{t("bids.changeStatusSubtitle")}</p>

      <section className={styles.card}>
        <form action={changeBidStatus} className={styles.form}>
          <input type="hidden" name="id" value={bid.id} />
          <input type="hidden" name="workPackageId" value={bid.workPackageId} />
          <select name="status" defaultValue={targets[0]}>
            {targets.map((s) => (
              <option key={s} value={s}>
                {BID_STATUS_LABELS[s]}
              </option>
            ))}
          </select>
          <button type="submit">{t("bids.updateStatus")}</button>
        </form>
        <p className={styles.muted}>{t("bids.selectWinnerNote")}</p>
      </section>
    </main>
  );
}
