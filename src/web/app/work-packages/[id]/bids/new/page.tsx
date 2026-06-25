import Link from "next/link";
import { notFound } from "next/navigation";
import { BidForm } from "@/app/components/BidForm";
import { openBid } from "@/app/bids/actions";
import {
  getContractors,
  getWorkPackage,
  type Contractor,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Opening a bid is a deliberate act on its own route. Every registered contractor is offered —
// a contractor may hold several bids on one package (variants), told apart by their label.
export default async function NewBidPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const workPackage = await getWorkPackage(id);

  if (!workPackage) {
    notFound();
  }

  let contractors: Contractor[] = [];
  try {
    contractors = await getContractors();
  } catch {
    contractors = [];
  }

  return (
    <main className={styles.main}>
      <Link href={`/work-packages/${workPackage.id}`} className={styles.backLink}>
        {t("workPackages.backToWorkPackage")}
      </Link>
      <h1>{t("workPackages.newBidTitle")}</h1>
      <p className={styles.subtitle}>{t("workPackages.openBidSubtitle")}</p>

      <section className={styles.card}>
        {contractors.length === 0 ? (
          <p className={styles.muted}>{t("workPackages.noContractors")}</p>
        ) : (
          <BidForm
            action={openBid}
            workPackageId={workPackage.id}
            contractors={contractors}
            submitLabel={t("workPackages.openBid")}
          />
        )}
        <Link
          href={`/work-packages/${workPackage.id}`}
          className={styles.backLink}
        >
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
