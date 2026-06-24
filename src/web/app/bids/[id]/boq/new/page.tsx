import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { BillOfQuantitiesForm } from "@/app/components/BillOfQuantitiesForm";
import { draftBoq } from "@/app/bills-of-quantities/actions";
import { getBid, getBidBoq } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function DraftBidBoqPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const bid = await getBid(id);

  if (!bid) {
    notFound();
  }

  // A bid carries at most one BoQ. If one already exists, drafting is unavailable —
  // send the reader back to the bid, where the existing bill is shown.
  const boq = await getBidBoq(bid.id);
  if (boq) {
    redirect(`/bids/${id}`);
  }

  return (
    <main className={styles.main}>
      <Link href={`/bids/${bid.id}`} className={styles.backLink}>
        {t("bids.backToBid")}
      </Link>
      <h1>{t("bids.draftBoqHeading")}</h1>
      <p className={styles.subtitle}>{t("bids.draftBoqSubtitle")}</p>

      <section className={styles.card}>
        <BillOfQuantitiesForm
          action={draftBoq}
          bidId={bid.id}
          submitLabel={t("bids.draftBoqSubmit")}
        />
      </section>
    </main>
  );
}
