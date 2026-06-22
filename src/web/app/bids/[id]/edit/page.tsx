import Link from "next/link";
import { notFound } from "next/navigation";
import { BidForm } from "@/app/components/BidForm";
import { updateBid } from "@/app/bids/actions";
import { getBid, getContractor } from "@/app/lib/api";
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
  const contractorName = contractor?.name ?? "this contractor";

  return (
    <main className={styles.main}>
      <h1>Edit bid</h1>
      <p className={styles.subtitle}>
        Update the standing for &ldquo;{contractorName}&rdquo;. The contractor and
        status are not editable here (status has its own controls on the bid).
      </p>

      <section className={styles.card}>
        <BidForm
          action={updateBid}
          workPackageId={bid.workPackageId}
          bid={bid}
          submitLabel="Save changes"
        />
        <Link href={`/bids/${bid.id}`} className={styles.backLink}>
          Cancel
        </Link>
      </section>
    </main>
  );
}
