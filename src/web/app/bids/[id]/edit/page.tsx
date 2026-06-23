import Link from "next/link";
import { notFound } from "next/navigation";
import { BidForm } from "@/app/components/BidForm";
import { updateBid } from "@/app/bids/actions";
import { getBid, getContractor } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function EditBidPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const bid = await getBid(id);

  if (!bid) {
    notFound();
  }

  const contractor = await getContractor(bid.contractorId);
  const contractorName = contractor?.name ?? t("bids.thisContractor");

  return (
    <main className={styles.main}>
      <h1>{t("bids.edit")}</h1>
      <p className={styles.subtitle}>
        {t("bids.editSubtitle", { name: contractorName })}
      </p>

      <section className={styles.card}>
        <BidForm
          action={updateBid}
          workPackageId={bid.workPackageId}
          bid={bid}
          submitLabel={t("common.saveChanges")}
        />
        <Link href={`/bids/${bid.id}`} className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
